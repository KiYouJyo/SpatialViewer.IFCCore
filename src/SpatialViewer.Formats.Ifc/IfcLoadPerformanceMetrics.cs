using System.Diagnostics;

namespace SpatialViewer.Formats.Ifc;

public enum IfcCacheDisposition
{
    None,
    Miss,
    MemoryHit,
    DiskHit,
}

public sealed record IfcLoadPerformanceMetrics(
    IfcLoadResult Result,
    TimeSpan Elapsed,
    long ManagedBytesStart,
    long ManagedBytesPeak,
    long ManagedBytesEnd,
    long WorkingSetBytesStart,
    long WorkingSetBytesPeak,
    long WorkingSetBytesEnd,
    IfcCacheDisposition CacheDisposition);

public static class IfcLoadBenchmark
{
    public static async ValueTask<IfcLoadPerformanceMetrics> MeasureOpenAsync(
        IIfcModelReader reader,
        string path,
        IfcOpenOptions? options = null,
        TimeSpan? sampleInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var interval = sampleInterval ?? TimeSpan.FromMilliseconds(25);
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleInterval));
        }

        using var process = Process.GetCurrentProcess();
        var managedStart = GC.GetTotalMemory(forceFullCollection: false);
        process.Refresh();
        var workingSetStart = process.WorkingSet64;
        var managedPeak = managedStart;
        var workingSetPeak = workingSetStart;
        var stopwatch = Stopwatch.StartNew();
        var loadTask = reader.OpenAsync(path, options, cancellationToken).AsTask();

        while (!loadTask.IsCompleted)
        {
            Sample(process, ref managedPeak, ref workingSetPeak);
            var delayTask = Task.Delay(interval, cancellationToken);
            var completed = await Task.WhenAny(loadTask, delayTask).ConfigureAwait(false);
            if (ReferenceEquals(completed, delayTask))
            {
                await delayTask.ConfigureAwait(false);
            }
        }

        var result = await loadTask.ConfigureAwait(false);
        stopwatch.Stop();
        Sample(process, ref managedPeak, ref workingSetPeak);
        var managedEnd = GC.GetTotalMemory(forceFullCollection: false);
        process.Refresh();
        var workingSetEnd = process.WorkingSet64;
        managedPeak = Math.Max(managedPeak, managedEnd);
        workingSetPeak = Math.Max(workingSetPeak, workingSetEnd);

        return new IfcLoadPerformanceMetrics(
            result,
            stopwatch.Elapsed,
            managedStart,
            managedPeak,
            managedEnd,
            workingSetStart,
            workingSetPeak,
            workingSetEnd,
            GetCacheDisposition(result.Diagnostics));
    }

    private static void Sample(Process process, ref long managedPeak, ref long workingSetPeak)
    {
        managedPeak = Math.Max(managedPeak, GC.GetTotalMemory(forceFullCollection: false));
        process.Refresh();
        workingSetPeak = Math.Max(workingSetPeak, process.WorkingSet64);
    }

    private static IfcCacheDisposition GetCacheDisposition(IReadOnlyList<IfcLoadDiagnostic> diagnostics)
    {
        if (diagnostics.Any(item => item.Code == "IFC_CACHE_MEMORY_HIT"))
        {
            return IfcCacheDisposition.MemoryHit;
        }

        if (diagnostics.Any(item => item.Code == "IFC_CACHE_DISK_HIT"))
        {
            return IfcCacheDisposition.DiskHit;
        }

        return diagnostics.Any(item => item.Code == "IFC_CACHE_MISS")
            ? IfcCacheDisposition.Miss
            : IfcCacheDisposition.None;
    }
}
