using System.Numerics;
using SpatialViewer.Core;
using SpatialViewer.Core.Geometry;
using SpatialViewer.Rendering;
using Xunit;

namespace SpatialViewer.Rendering.Tests;

public sealed class RenderSelectionTests
{
    [Fact]
    public void PickMapExposesBimPropertiesWithoutRewalkingSceneGraph()
    {
        var root = new SceneNode("root");
        var wall = new SceneNode("wall-node")
        {
            SourceId = "wall-global",
            Name = "Wall 01",
            Category = "IfcWall",
            Bounds = new BoundingBox3(Vector3.Zero, Vector3.One),
        };
        wall.Properties["Pset_WallCommon.Reference"] = new SceneProperty(
            "Reference",
            "W-01",
            null,
            "Pset_WallCommon");
        wall.Meshes.Add(new MeshData(
            [Vector3.Zero, Vector3.UnitX, Vector3.UnitY],
            [0, 1, 2]));
        root.Children.Add(wall);

        var scene = RenderScene.FromDocument(new SceneDocument(root));
        var item = Assert.Single(scene.Objects);
        var selected = scene.PickMap[item.PickId];

        Assert.Equal("Wall 01", selected.Name);
        Assert.Equal("IfcWall", selected.Category);
        Assert.Equal("W-01", selected.Properties["Pset_WallCommon.Reference"].Value);
    }
}
