using System.Numerics;
using SpatialViewer.Core;
using SpatialViewer.Core.Geometry;

namespace SpatialViewer.Rendering.Tests;

public sealed class RenderSceneTests
{
    [Fact]
    public void Scene_flattens_meshes_without_losing_node_identity()
    {
        var root = new SceneNode("root");
        var wall = new SceneNode("wall-1");
        wall.Meshes.Add(new MeshData(
            new[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY },
            new[] { 0, 1, 2 }));
        root.Children.Add(wall);

        var renderScene = RenderScene.FromDocument(new SceneDocument(root));

        var renderMesh = Assert.Single(renderScene.Meshes);
        Assert.Equal("wall-1", renderMesh.NodeId);
    }
}
