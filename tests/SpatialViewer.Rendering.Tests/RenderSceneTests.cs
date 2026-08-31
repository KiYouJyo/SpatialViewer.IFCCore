using System.Numerics;
using SpatialViewer.Core;
using SpatialViewer.Core.Geometry;
using SpatialViewer.Rendering;
using Xunit;

namespace SpatialViewer.Rendering.Tests;

public sealed class RenderSceneTests
{
    private static readonly Vector3[] TrianglePositions = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY];
    private static readonly int[] TriangleIndices = [0, 1, 2];

    [Fact]
    public void SceneFlattensMeshesWithoutLosingNodeIdentity()
    {
        var root = new SceneNode("root");
        var wall = new SceneNode("wall-1");
        wall.Meshes.Add(new MeshData(TrianglePositions, TriangleIndices));
        root.Children.Add(wall);

        var renderScene = RenderScene.FromDocument(new SceneDocument(root));

        var renderMesh = Assert.Single(renderScene.Meshes);
        Assert.Equal("wall-1", renderMesh.NodeId);
    }
}
