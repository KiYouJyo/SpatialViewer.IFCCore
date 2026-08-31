using System.Numerics;
using SpatialViewer.Core;
using SpatialViewer.Core.Geometry;
using SpatialViewer.Rendering;
using Xunit;

namespace SpatialViewer.Rendering.Tests;

public sealed class RenderSceneIndexTests
{
    [Fact]
    public void IndexRebuildsViewStateWithoutRetraversingDocument()
    {
        var document = CreateDocument();
        var index = RenderSceneIndex.Create(document);
        var baseline = index.Build();
        var firstPickId = baseline.Objects.Single(item => item.ObjectId == "wall-1").PickId;

        document.Root.Children.Clear();
        var options = new RenderSceneOptions();
        options.Visibility.HiddenObjectIds.Add("wall-2");
        options.Appearance.CategoryOpacity["IfcWall"] = 0.4f;
        var rebuilt = index.Build(options);

        Assert.Equal(2, index.ObjectCount);
        Assert.Equal(2, index.MeshTemplateCount);
        var visible = Assert.Single(rebuilt.Objects);
        Assert.Equal("wall-1", visible.ObjectId);
        Assert.Equal(firstPickId, visible.PickId);
        var mesh = Assert.Single(rebuilt.Meshes);
        Assert.Equal(0.4f, mesh.Opacity);
    }

    [Fact]
    public void UploadEstimateCountsSharedGeometryOnce()
    {
        var scene = RenderSceneIndex.Create(CreateDocument()).Build();

        var estimate = RenderPerformanceMetrics.EstimateGpuUpload(scene);

        Assert.Equal(1, estimate.UniqueMeshCount);
        Assert.Equal(2, estimate.InstanceCount);
        Assert.Equal(1, estimate.TriangleCount);
        Assert.Equal(1, estimate.MaterialCount);
        Assert.Equal(36, estimate.VertexBytes);
        Assert.Equal(12, estimate.IndexBytes);
        Assert.Equal(48, estimate.TotalGeometryBytes);
    }

    [Fact]
    public void RebuildMeasurementReportsIterationsAndAllocations()
    {
        var index = RenderSceneIndex.Create(CreateDocument());
        var options = new RenderSceneOptions();
        options.Visibility.IsolatedObjectIds.Add("wall-1");

        var measurement = RenderPerformanceMetrics.MeasureRebuild(index, options, iterations: 3);

        Assert.Equal(3, measurement.Iterations);
        Assert.True(measurement.TotalElapsed >= TimeSpan.Zero);
        Assert.True(measurement.AverageElapsed >= TimeSpan.Zero);
        Assert.True(measurement.AllocatedBytes >= 0);
    }

    private static SceneDocument CreateDocument()
    {
        var sharedMesh = new MeshData(
            new[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY },
            new[] { 0, 1, 2 });
        var root = new SceneNode("root") { Category = "IFC" };
        var storey = new SceneNode("storey")
        {
            SourceId = "storey-1",
            Name = "Level 1",
            Category = "IfcBuildingStorey",
        };
        storey.Children.Add(CreateWall("wall-1", "Wall 1", sharedMesh, Vector3.Zero));
        storey.Children.Add(CreateWall("wall-2", "Wall 2", sharedMesh, new Vector3(3f, 0f, 0f)));
        root.Children.Add(storey);
        return new SceneDocument(root);
    }

    private static SceneNode CreateWall(string sourceId, string name, MeshData mesh, Vector3 translation)
    {
        var wall = new SceneNode($"semantic:{sourceId}")
        {
            SourceId = sourceId,
            Name = name,
            Category = "IfcWall",
        };
        var bounds = new BoundingBox3(translation, translation + Vector3.One);
        var geometry = new SceneNode($"geometry:{sourceId}")
        {
            SourceId = sourceId,
            Name = name,
            Category = "IFC.Geometry",
            Transform = Matrix4x4.CreateTranslation(translation),
            Bounds = bounds,
        };
        geometry.Meshes.Add(mesh);
        wall.Children.Add(geometry);
        wall.Bounds = bounds;
        return wall;
    }
}
