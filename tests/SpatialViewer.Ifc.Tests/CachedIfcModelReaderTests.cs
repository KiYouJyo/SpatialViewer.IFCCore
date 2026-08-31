using System.Numerics;
using SpatialViewer.Core;
using SpatialViewer.Core.Geometry;
using SpatialViewer.Formats.Ifc;
using Xunit;

namespace SpatialViewer.Ifc.Tests;

public sealed class CachedIfcModelReaderTests
{
    private static readonly Vector3[] TrianglePositions = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY];
    private static readonly int[] TriangleIndices = [0, 1, 2];
    private static readonly Vector3[] TriangleNormals = [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ];

    [Fact]
    public async Task MemoryCacheReusesLoadedSceneUntilSourceChanges()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "model.ifc");
            await File.WriteAllTextAsync(path, "IFC-A");
            var inner = new CountingReader();
            var reader = new CachedIfcModelReader(inner, new IfcModelCacheOptions
            {
                EnableMemoryCache = true,
                MemoryEntryLimit = 2,
            });

            var first = await reader.OpenAsync(path);
            var second = await reader.OpenAsync(path);

            Assert.Equal(1, inner.CallCount);
            Assert.Same(first.Document, second.Document);
            Assert.Contains(second.Diagnostics, item => item.Code == "IFC_CACHE_MEMORY_HIT");
            Assert.Equal(1, reader.Statistics.MemoryHits);

            await File.WriteAllTextAsync(path, "IFC-B");
            _ = await reader.OpenAsync(path);
            Assert.Equal(2, inner.CallCount);
            Assert.Equal(2, reader.Statistics.Misses);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task DiskCacheSurvivesReaderInstancesAndPreservesSharedMeshes()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "model.ifc");
            var cacheDirectory = Path.Combine(directory, "cache");
            await File.WriteAllTextAsync(path, "IFC-DISK");

            var coldInner = new CountingReader();
            var coldReader = new CachedIfcModelReader(coldInner, new IfcModelCacheOptions
            {
                EnableMemoryCache = false,
                DiskCacheDirectory = cacheDirectory,
            });
            var cold = await coldReader.OpenAsync(path);
            Assert.Equal(1, coldInner.CallCount);
            Assert.Equal(1, coldReader.Statistics.DiskWrites);

            var warmInner = new CountingReader();
            var warmReader = new CachedIfcModelReader(warmInner, new IfcModelCacheOptions
            {
                EnableMemoryCache = false,
                DiskCacheDirectory = cacheDirectory,
            });
            var warm = await warmReader.OpenAsync(path);

            Assert.Equal(0, warmInner.CallCount);
            Assert.Equal(IfcSchemaVersion.Ifc4, warm.Schema);
            Assert.Contains(warm.Diagnostics, item => item.Code == "IFC_CACHE_DISK_HIT");
            Assert.Equal(Path.GetFullPath(path), warm.Document.SourcePath);
            Assert.Equal(cold.Document.Metadata["Fixture"], warm.Document.Metadata["Fixture"]);

            var firstMesh = warm.Document.Root.Children[0].Meshes[0];
            var secondMesh = warm.Document.Root.Children[1].Meshes[0];
            Assert.Same(firstMesh, secondMesh);
            Assert.Equal("fallback:test", firstMesh.MaterialId);
            Assert.Equal("42", warm.Document.Root.Children[0].Properties["Pset.Value"].Value);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task OpenOptionSignaturePreventsIncompatibleCacheReuse()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "model.ifc");
            await File.WriteAllTextAsync(path, "IFC-OPTIONS");
            var inner = new CountingReader();
            var reader = new CachedIfcModelReader(inner);

            _ = await reader.OpenAsync(path, new IfcOpenOptions { IncludeGeometry = false });
            _ = await reader.OpenAsync(path, new IfcOpenOptions { IncludeGeometry = true });

            Assert.Equal(2, inner.CallCount);
            Assert.Equal(2, reader.Statistics.Misses);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task CorruptDiskCacheFallsBackToColdLoad()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "model.ifc");
            var cacheDirectory = Path.Combine(directory, "cache");
            await File.WriteAllTextAsync(path, "IFC-CORRUPT");

            var firstReader = new CachedIfcModelReader(new CountingReader(), new IfcModelCacheOptions
            {
                EnableMemoryCache = false,
                DiskCacheDirectory = cacheDirectory,
            });
            _ = await firstReader.OpenAsync(path);
            var files = Directory.GetFiles(cacheDirectory, "*.svbim");
            var cacheFile = Assert.Single(files);
            await File.WriteAllTextAsync(cacheFile, "not-a-valid-cache");

            var fallbackInner = new CountingReader();
            var fallbackReader = new CachedIfcModelReader(fallbackInner, new IfcModelCacheOptions
            {
                EnableMemoryCache = false,
                DiskCacheDirectory = cacheDirectory,
            });
            var result = await fallbackReader.OpenAsync(path);

            Assert.Equal(1, fallbackInner.CallCount);
            Assert.Contains(result.Diagnostics, item => item.Code == "IFC_CACHE_READ_FAILED");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task CacheFingerprintHonorsCancellation()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "model.ifc");
            await File.WriteAllTextAsync(path, "IFC-CANCEL");
            using var source = new CancellationTokenSource();
            await source.CancelAsync();
            var reader = new CachedIfcModelReader(new CountingReader());

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => reader.OpenAsync(path, cancellationToken: source.Token).AsTask());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"SpatialViewer.IFCCore.Tests.{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class CountingReader : IIfcModelReader
    {
        public int CallCount { get; private set; }

        public ValueTask<IfcLoadResult> OpenAsync(
            string path,
            IfcOpenOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CallCount++;
            return ValueTask.FromResult(CreateResult(path));
        }

        private static IfcLoadResult CreateResult(string path)
        {
            var mesh = new MeshData(TrianglePositions, TriangleIndices)
            {
                Normals = TriangleNormals,
                MaterialId = "fallback:test",
            };
            var bounds = new BoundingBox3(Vector3.Zero, Vector3.One);
            var root = new SceneNode("root") { Name = "Root", Category = "IFC", Bounds = bounds };
            var first = new SceneNode("first")
            {
                SourceId = "source-first",
                Name = "First",
                Category = "IfcWall",
                Bounds = bounds,
                Transform = Matrix4x4.CreateTranslation(1f, 2f, 3f),
            };
            first.Meshes.Add(mesh);
            first.Properties["Pset.Value"] = new SceneProperty("Value", "42", null, "Pset");
            var second = new SceneNode("second")
            {
                SourceId = "source-second",
                Name = "Second",
                Category = "IfcWall",
                Bounds = bounds,
                FlipWinding = true,
            };
            second.Meshes.Add(mesh);
            root.Children.Add(first);
            root.Children.Add(second);

            var document = new SceneDocument(root)
            {
                SourcePath = Path.GetFullPath(path),
                Bounds = bounds,
                WorldBounds = bounds.Translate(new Vector3(100f, 200f, 300f)),
                WorldOrigin = new Vector3(100f, 200f, 300f),
            };
            document.Metadata["Fixture"] = "cache-test";
            document.Metadata["SourcePath"] = document.SourcePath;

            return new IfcLoadResult(
                document,
                IfcSchemaVersion.Ifc4,
                new[] { new IfcLoadDiagnostic(IfcDiagnosticSeverity.Info, "FIXTURE", "fixture diagnostic") },
                TimeSpan.FromMilliseconds(25));
        }
    }
}
