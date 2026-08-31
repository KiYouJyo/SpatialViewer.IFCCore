using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace SpatialViewer.Formats.Ifc;

public sealed class CachedIfcModelReader : IIfcModelReader
{
    private readonly IIfcModelReader _inner;
    private readonly IfcModelCacheOptions _cacheOptions;
    private readonly object _memoryLock = new();
    private readonly Dictionary<string, LinkedListNode<MemoryCacheItem>> _memory = new(StringComparer.Ordinal);
    private readonly LinkedList<MemoryCacheItem> _lru = new();
    private long _memoryHits;
    private long _diskHits;
    private long _misses;
    private long _diskWrites;

    public CachedIfcModelReader(IIfcModelReader inner, IfcModelCacheOptions? cacheOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cacheOptions = cacheOptions ?? new IfcModelCacheOptions();
        if (_cacheOptions.MemoryEntryLimit < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(cacheOptions),
                _cacheOptions.MemoryEntryLimit,
                "MemoryEntryLimit cannot be negative.");
        }
    }

    public IfcCacheStatistics Statistics => new(
        Interlocked.Read(ref _memoryHits),
        Interlocked.Read(ref _diskHits),
        Interlocked.Read(ref _misses),
        Interlocked.Read(ref _diskWrites));

    public async ValueTask<IfcLoadResult> OpenAsync(
        string path,
        IfcOpenOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        options ??= new IfcOpenOptions();

        if (!_cacheOptions.EnableMemoryCache && !_cacheOptions.EnableDiskCache)
        {
            return await _inner.OpenAsync(path, options, cancellationToken).ConfigureAwait(false);
        }

        var stopwatch = Stopwatch.StartNew();
        Report(options, IfcLoadStage.CheckingCache, 0, "Fingerprinting IFC source for cache lookup");
        var fingerprint = await IfcSourceFingerprint.CreateAsync(path, cancellationToken).ConfigureAwait(false);
        var optionSignature = BuildOptionSignature(options);
        var cacheKey = BuildCacheKey(fingerprint, optionSignature);

        if (_cacheOptions.EnableMemoryCache && TryGetMemory(cacheKey, out var memoryEntry))
        {
            Interlocked.Increment(ref _memoryHits);
            Report(options, IfcLoadStage.Completed, 100, "IFC model restored from memory cache");
            stopwatch.Stop();
            return BuildHitResult(memoryEntry, stopwatch.Elapsed, "IFC_CACHE_MEMORY_HIT", "IFC model restored from the in-memory BIM cache.");
        }

        var cacheDiagnostics = new List<IfcLoadDiagnostic>();
        if (_cacheOptions.EnableDiskCache)
        {
            var cachePath = GetDiskCachePath(cacheKey);
            Report(options, IfcLoadStage.ReadingCache, 0, "Checking SpatialViewer BIM disk cache");
            try
            {
                var diskEntry = await IfcSceneCacheFile.TryReadAsync(
                    cachePath,
                    cacheKey,
                    path,
                    cancellationToken).ConfigureAwait(false);
                if (diskEntry is not null)
                {
                    AddMemory(cacheKey, diskEntry);
                    Interlocked.Increment(ref _diskHits);
                    Report(options, IfcLoadStage.Completed, 100, "IFC model restored from disk cache");
                    stopwatch.Stop();
                    return BuildHitResult(diskEntry, stopwatch.Elapsed, "IFC_CACHE_DISK_HIT", "IFC model restored from the SpatialViewer BIM disk cache.");
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
            {
                cacheDiagnostics.Add(new IfcLoadDiagnostic(
                    IfcDiagnosticSeverity.Warning,
                    "IFC_CACHE_READ_FAILED",
                    $"The BIM disk cache could not be read and was ignored: {exception.Message}"));
            }
        }

        Interlocked.Increment(ref _misses);
        var coldResult = await _inner.OpenAsync(path, options, cancellationToken).ConfigureAwait(false);
        var storedEntry = new IfcCachedEntry(coldResult.Document, coldResult.Schema, coldResult.Diagnostics);
        AddMemory(cacheKey, storedEntry);

        if (_cacheOptions.EnableDiskCache)
        {
            Report(options, IfcLoadStage.WritingCache, 0, "Writing SpatialViewer BIM disk cache");
            try
            {
                await IfcSceneCacheFile.WriteAsync(
                    GetDiskCachePath(cacheKey),
                    cacheKey,
                    storedEntry,
                    cancellationToken).ConfigureAwait(false);
                Interlocked.Increment(ref _diskWrites);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                cacheDiagnostics.Add(new IfcLoadDiagnostic(
                    IfcDiagnosticSeverity.Warning,
                    "IFC_CACHE_WRITE_FAILED",
                    $"The BIM disk cache could not be written: {exception.Message}"));
            }
        }

        stopwatch.Stop();
        var diagnostics = new List<IfcLoadDiagnostic>(coldResult.Diagnostics.Count + cacheDiagnostics.Count + 1);
        diagnostics.AddRange(coldResult.Diagnostics);
        diagnostics.AddRange(cacheDiagnostics);
        diagnostics.Add(new IfcLoadDiagnostic(
            IfcDiagnosticSeverity.Info,
            "IFC_CACHE_MISS",
            "No reusable BIM cache entry matched this source fingerprint and open-option signature."));
        Report(options, IfcLoadStage.Completed, 100, "IFC model loaded and cache state updated");
        return new IfcLoadResult(coldResult.Document, coldResult.Schema, diagnostics, stopwatch.Elapsed);
    }

    public void ClearMemoryCache()
    {
        lock (_memoryLock)
        {
            _memory.Clear();
            _lru.Clear();
        }
    }

    private static IfcLoadResult BuildHitResult(
        IfcCachedEntry entry,
        TimeSpan elapsed,
        string code,
        string message)
    {
        var diagnostics = new List<IfcLoadDiagnostic>(entry.Diagnostics.Count + 1);
        diagnostics.AddRange(entry.Diagnostics);
        diagnostics.Add(new IfcLoadDiagnostic(IfcDiagnosticSeverity.Info, code, message));
        return new IfcLoadResult(entry.Document, entry.Schema, diagnostics, elapsed);
    }

    private bool TryGetMemory(string key, out IfcCachedEntry entry)
    {
        lock (_memoryLock)
        {
            if (!_memory.TryGetValue(key, out var node))
            {
                entry = null!;
                return false;
            }

            _lru.Remove(node);
            _lru.AddFirst(node);
            entry = node.Value.Entry;
            return true;
        }
    }

    private void AddMemory(string key, IfcCachedEntry entry)
    {
        if (!_cacheOptions.EnableMemoryCache || _cacheOptions.MemoryEntryLimit == 0)
        {
            return;
        }

        lock (_memoryLock)
        {
            if (_memory.TryGetValue(key, out var existing))
            {
                existing.Value = new MemoryCacheItem(key, entry);
                _lru.Remove(existing);
                _lru.AddFirst(existing);
                return;
            }

            var node = new LinkedListNode<MemoryCacheItem>(new MemoryCacheItem(key, entry));
            _lru.AddFirst(node);
            _memory.Add(key, node);
            while (_memory.Count > _cacheOptions.MemoryEntryLimit)
            {
                var last = _lru.Last;
                if (last is null)
                {
                    break;
                }

                _lru.RemoveLast();
                _memory.Remove(last.Value.Key);
            }
        }
    }

    private string GetDiskCachePath(string cacheKey)
    {
        var directory = _cacheOptions.DiskCacheDirectory!;
        return Path.Combine(Path.GetFullPath(directory), $"{cacheKey}.svbim");
    }

    private static string BuildOptionSignature(IfcOpenOptions options) => string.Join(
        '|',
        options.IncludeGeometry ? "g1" : "g0",
        options.IncludeProperties ? "p1" : "p0",
        options.PreserveOpeningElements ? "o1" : "o0",
        options.RebaseLargeCoordinates ? "r1" : "r0",
        options.LargeCoordinateThresholdMetres.ToString("R", CultureInfo.InvariantCulture));

    private static string BuildCacheKey(IfcSourceFingerprint fingerprint, string optionSignature)
    {
        var payload = Encoding.UTF8.GetBytes($"svbim-v1|{fingerprint.Sha256}|{fingerprint.Length}|{optionSignature}");
        return Convert.ToHexString(SHA256.HashData(payload));
    }

    private static void Report(IfcOpenOptions options, IfcLoadStage stage, int percent, string message) =>
        options.Progress?.Report(new IfcLoadProgress(stage, percent, message));

    private sealed record MemoryCacheItem(string Key, IfcCachedEntry Entry);
}
