using System.Numerics;
using SpatialViewer.Core;
using SpatialViewer.Core.Geometry;
using SpatialViewer.Rendering;
using Xunit;

namespace SpatialViewer.Rendering.Tests;

public sealed class RenderBatchTests
{
    private static readonly Vector3[] TrianglePositions = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY];
    private static readonly int[] TriangleIndices = [0, 1, 2];

    [Fact]
    public void SharedMeshesWithMatchingStateBecomeOneInstanceBatch()
    {
        var sharedMesh = new MeshData(TrianglePositions, TriangleIndices) { MaterialId = "material:shared" };
        var document = CreateDocument(sharedMesh);

        var scene = RenderScene.FromDocument(document);

        var batch = Assert.Single(scene.Batches);
        Assert.Same(sharedMesh, batch.Mesh);
        Assert.Equal(2, batch.Instances.Count);
        Assert.Equal("material:shared", batch.MaterialId);
    }

    [Fact]
    public void AppearanceDifferencesSplitOtherwiseSharedBatches()
    {
        var sharedMesh = new MeshData(TrianglePositions, TriangleIndices) { MaterialId = "material:shared" };
        var document = CreateDocument(sharedMesh);
        var options = new RenderSceneOptions();
        options.Appearance.ObjectOpacity["object-a"] = 0.5f;

        var scene = RenderScene.FromDocument(document, options);

        Assert.Equal(2, scene.Batches.Count);
        Assert.Contains(scene.Batches, batch => batch.Opacity == 0.5f);
        Assert.Contains(scene.Batches, batch => batch.Opacity == 1f);
    }

    private static SceneDocument CreateDocument(MeshData sharedMesh)
    {
        var root = new SceneNode("root");
        var first = new SceneNode("node-a")
        {
            SourceId = "object-a",
            Category = "IfcWall",
            Bounds = new BoundingBox3(Vector3.Zero, Vector3.One),
        };
        var second = new SceneNode("node-b")
        {
            SourceId = "object-b",
            Category = "IfcWall",
            Bounds = new BoundingBox3(new Vector3(2f), new Vector3(3f)),
            Transform = Matrix4x4.CreateTranslation(2f, 0f, 0f),
        };
        first.Meshes.Add(sharedMesh);
        second.Meshes.Add(sharedMesh);
        root.Children.Add(first);
        root.Children.Add(second);
        return new SceneDocument(root);
    }
}
