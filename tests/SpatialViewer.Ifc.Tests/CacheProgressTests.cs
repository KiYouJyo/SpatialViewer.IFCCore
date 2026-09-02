using SpatialViewer.Core;
using Xunit;

namespace SpatialViewer.Formats.Ifc.Tests;

public sealed class CacheProgressTests
{
    [Fact]
    public async Task ColdLoadReportsCacheWriteBeforeSingleCompletedStage()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"SpatialViewer.IFCCore.Progress.{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "model.ifc");
            await File.WriteAllTextAsync(path, "IFC-PROGRESS");
            var progress = new RecordingProgress();
            var reader = new CachedIfcModelReader(new ProgressReader(), new IfcModelCacheOptions
            {
                DiskCacheDirectory = Path.Combine(directory, "cache"),
            });

            _ = await reader.OpenAsync(path, new IfcOpenOptions { Progress = progress });

            Assert.Equal(IfcLoadStage.CheckingCache, progress.Events[0].Stage);
            Assert.Contains(progress.Events, item => item.Stage == IfcLoadStage.Opening);
            var writingIndex = progress.Events.FindIndex(item => item.Stage == IfcLoadStage.WritingCache);
            var completedIndexes = progress.Events
                .Select((item, index) => (item, index))
                .Where(pair => pair.item.Stage == IfcLoadStage.Completed)
                .Select(pair => pair.index)
                .ToList();
            var completedIndex = Assert.Single(completedIndexes);
            Assert.True(writingIndex >= 0);
            Assert.True(writingIndex < completedIndex);
            Assert.Equal(progress.Events.Count - 1, completedIndex);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class RecordingProgress : IProgress<IfcLoadProgress>
    {
        public List<IfcLoadProgress> Events { get; } = [];

        public void Report(IfcLoadProgress value) => Events.Add(value);
    }

    private sealed class ProgressReader : IIfcModelReader
    {
        public ValueTask<IfcLoadResult> OpenAsync(
            string path,
            IfcOpenOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            options?.Progress?.Report(new IfcLoadProgress(IfcLoadStage.Opening, 0, "opening"));
            options?.Progress?.Report(new IfcLoadProgress(IfcLoadStage.Completed, 100, "inner complete"));
            var document = new SceneDocument(new SceneNode("root"))
            {
                SourcePath = Path.GetFullPath(path),
            };
            return ValueTask.FromResult(new IfcLoadResult(
                document,
                IfcSchemaVersion.Ifc4,
                Array.Empty<IfcLoadDiagnostic>(),
                TimeSpan.Zero));
        }
    }
}
