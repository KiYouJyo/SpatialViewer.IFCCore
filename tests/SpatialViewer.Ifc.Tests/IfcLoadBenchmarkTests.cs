using SpatialViewer.Core;
using SpatialViewer.Formats.Ifc;
using Xunit;

namespace SpatialViewer.Ifc.Tests;

public sealed class IfcLoadBenchmarkTests
{
    [Fact]
    public async Task BenchmarkDistinguishesColdAndMemoryHitLoads()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"SpatialViewer.IFCCore.Benchmark.{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "model.ifc");
            await File.WriteAllTextAsync(path, "IFC-BENCHMARK");
            var cached = new CachedIfcModelReader(new DelayedReader());

            var cold = await IfcLoadBenchmark.MeasureOpenAsync(
                cached,
                path,
                sampleInterval: TimeSpan.FromMilliseconds(5));
            var warm = await IfcLoadBenchmark.MeasureOpenAsync(
                cached,
                path,
                sampleInterval: TimeSpan.FromMilliseconds(5));

            Assert.Equal(IfcCacheDisposition.Miss, cold.CacheDisposition);
            Assert.Equal(IfcCacheDisposition.MemoryHit, warm.CacheDisposition);
            Assert.True(cold.ManagedBytesPeak >= cold.ManagedBytesStart);
            Assert.True(cold.ManagedBytesPeak >= cold.ManagedBytesEnd);
            Assert.True(cold.WorkingSetBytesPeak >= cold.WorkingSetBytesStart);
            Assert.True(cold.WorkingSetBytesPeak >= cold.WorkingSetBytesEnd);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class DelayedReader : IIfcModelReader
    {
        public async ValueTask<IfcLoadResult> OpenAsync(
            string path,
            IfcOpenOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(20, cancellationToken);
            var document = new SceneDocument(new SceneNode("root"))
            {
                SourcePath = Path.GetFullPath(path),
            };
            return new IfcLoadResult(
                document,
                IfcSchemaVersion.Ifc4,
                Array.Empty<IfcLoadDiagnostic>(),
                TimeSpan.FromMilliseconds(20));
        }
    }
}
