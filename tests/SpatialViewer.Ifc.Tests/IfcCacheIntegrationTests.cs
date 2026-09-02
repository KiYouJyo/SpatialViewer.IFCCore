using SpatialViewer.Formats.Ifc.Xbim;
using Xunit;

namespace SpatialViewer.Formats.Ifc.Tests;

public sealed class IfcCacheIntegrationTests
{
    [Fact]
    public async Task DiskCacheBypassesXbimGeometryOnWarmOpen()
    {
        var path = IfcTestFile.WriteGeometryIfc4();
        var cacheDirectory = Path.Combine(Path.GetTempPath(), $"SpatialViewer.IFCCore.CacheIntegration.{Guid.NewGuid():N}");
        try
        {
            var options = new IfcOpenOptions { IncludeGeometry = true };
            var coldInner = new CountingReader(new XbimIfcModelReader());
            var coldReader = new CachedIfcModelReader(coldInner, new IfcModelCacheOptions
            {
                EnableMemoryCache = false,
                DiskCacheDirectory = cacheDirectory,
            });
            var cold = await coldReader.OpenAsync(path, options);

            Assert.Equal(1, coldInner.CallCount);
            Assert.Contains(cold.Diagnostics, item => item.Code == "IFC_CACHE_MISS");
            Assert.Equal("2", cold.Document.Metadata["Geometry.InstanceCount"]);
            Assert.Equal("1", cold.Document.Metadata["Geometry.UniqueMeshCount"]);

            var warmInner = new CountingReader(new XbimIfcModelReader());
            var warmReader = new CachedIfcModelReader(warmInner, new IfcModelCacheOptions
            {
                EnableMemoryCache = false,
                DiskCacheDirectory = cacheDirectory,
            });
            var warm = await warmReader.OpenAsync(path, options);

            Assert.Equal(0, warmInner.CallCount);
            Assert.Contains(warm.Diagnostics, item => item.Code == "IFC_CACHE_DISK_HIT");
            Assert.Equal(cold.Schema, warm.Schema);
            Assert.Equal(cold.Document.Metadata["Geometry.InstanceCount"], warm.Document.Metadata["Geometry.InstanceCount"]);
            Assert.Equal(cold.Document.Metadata["Geometry.UniqueMeshCount"], warm.Document.Metadata["Geometry.UniqueMeshCount"]);
            Assert.Equal(cold.Document.Bounds, warm.Document.Bounds);
            Assert.Equal(cold.Document.WorldBounds, warm.Document.WorldBounds);
        }
        finally
        {
            File.Delete(path);
            if (Directory.Exists(cacheDirectory))
            {
                Directory.Delete(cacheDirectory, recursive: true);
            }
        }
    }

    private sealed class CountingReader(IIfcModelReader inner) : IIfcModelReader
    {
        public int CallCount { get; private set; }

        public ValueTask<IfcLoadResult> OpenAsync(
            string path,
            IfcOpenOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return inner.OpenAsync(path, options, cancellationToken);
        }
    }
}
