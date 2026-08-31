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
    public void ScenePreservesGeometryFlagsAndBuildsSelectionMetadata()
    {
        var root = new SceneNode("root");
        var storey = new SceneNode("storey-1")
        {
            SourceId = "storey-global",
            Name = "Level 1",
            Category = "IfcBuildingStorey",
        };
        var wall = new SceneNode("wall-1")
        {
            SourceId = "wall-global",
            Name = "Wall 01",
            Category = "IfcWall",
        };
        var bounds = new BoundingBox3(Vector3.Zero, Vector3.One);
        var geometry = new SceneNode("wall-geometry")
        {
            Category = "IFC.Geometry",
            Transform = Matrix4x4.CreateTranslation(10f, 20f, 30f),
            FlipWinding = true,
            Bounds = bounds,
        };
        geometry.Meshes.Add(CreateMesh());
        wall.Children.Add(geometry);
        storey.Children.Add(wall);
        root.Children.Add(storey);

        var renderScene = RenderScene.FromDocument(new SceneDocument(root));

        var renderMesh = Assert.Single(renderScene.Meshes);
        var renderObject = Assert.Single(renderScene.Objects);
        Assert.Equal("wall-geometry", renderMesh.NodeId);
        Assert.Equal("wall-global", renderMesh.ObjectId);
        Assert.Equal(renderObject.PickId, renderMesh.PickId);
        Assert.Equal(geometry.Transform, renderMesh.Transform);
        Assert.True(renderMesh.FlipWinding);
        Assert.Equal(bounds, renderMesh.Bounds);
        Assert.Equal("IfcWall", renderObject.Category);
        Assert.Equal("storey-global", renderObject.StoreyId);
        Assert.Equal("Level 1", renderObject.StoreyName);
        Assert.Same(renderObject, renderScene.PickMap[renderObject.PickId]);
    }

    [Fact]
    public void PickIdsRemainStableAcrossVisibilityChanges()
    {
        var document = CreateTwoObjectDocument();
        var baseline = RenderScene.FromDocument(document);
        var wallPickId = Assert.Single(baseline.Objects, item => item.ObjectId == "wall-global").PickId;

        var options = new RenderSceneOptions();
        options.Visibility.HiddenObjectIds.Add("door-global");
        var filtered = RenderScene.FromDocument(document, options);

        var wall = Assert.Single(filtered.Objects);
        Assert.Equal("wall-global", wall.ObjectId);
        Assert.Equal(wallPickId, wall.PickId);
    }

    [Fact]
    public void SceneSupportsCategoryStoreyHideAndObjectIsolation()
    {
        var document = CreateTwoObjectDocument();

        var categoryOptions = new RenderSceneOptions();
        categoryOptions.Visibility.HiddenCategories.Add("IfcWall");
        var withoutWalls = RenderScene.FromDocument(document, categoryOptions);
        Assert.DoesNotContain(withoutWalls.Objects, item => item.Category == "IfcWall");
        Assert.Contains(withoutWalls.Objects, item => item.Category == "IfcDoor");

        var storeyOptions = new RenderSceneOptions();
        storeyOptions.Visibility.HiddenStoreyIds.Add("storey-global");
        Assert.Empty(RenderScene.FromDocument(document, storeyOptions).Meshes);

        var isolateOptions = new RenderSceneOptions();
        isolateOptions.Visibility.IsolatedObjectIds.Add("door-global");
        var isolated = RenderScene.FromDocument(document, isolateOptions);
        Assert.Equal("door-global", Assert.Single(isolated.Objects).ObjectId);
    }

    [Fact]
    public void AppearanceOverridesUseObjectPrecedenceAndMaterialFallbacks()
    {
        var document = CreateTwoObjectDocument();
        var options = new RenderSceneOptions();
        options.Appearance.CategoryOpacity["IfcWall"] = 0.4f;
        options.Appearance.ObjectOpacity["wall-global"] = 0.2f;

        var scene = RenderScene.FromDocument(document, options);
        var wall = Assert.Single(scene.Meshes, item => item.ObjectId == "wall-global");
        var door = Assert.Single(scene.Meshes, item => item.ObjectId == "door-global");

        Assert.Equal(0.2f, wall.Opacity);
        Assert.True(wall.IsMaterialFallback);
        Assert.Equal("fallback:IfcWall", wall.MaterialId);
        Assert.Equal(1f, door.Opacity);
        Assert.False(door.IsMaterialFallback);
        Assert.Equal("xbim-style:42", door.MaterialId);
    }

    [Fact]
    public void SectionBoxCullsOutsideObjectsAndRemainsAvailableForGpuClipping()
    {
        var document = CreateTwoObjectDocument();
        var section = new SectionBox(new BoundingBox3(new Vector3(-1f), new Vector3(2f)));
        var options = new RenderSceneOptions { SectionBox = section };

        var scene = RenderScene.FromDocument(document, options);

        Assert.Equal("wall-global", Assert.Single(scene.Objects).ObjectId);
        Assert.Same(section, scene.SectionBox);
    }

    [Fact]
    public void OutlineTargetsAreOnePerVisibleObjectAndCanBeDisabled()
    {
        var document = CreateTwoObjectDocument();
        var scene = RenderScene.FromDocument(document);
        Assert.Equal(scene.Objects.Count, scene.OutlineTargets.Count);
        Assert.All(scene.OutlineTargets, outline => Assert.True(scene.PickMap.ContainsKey(outline.PickId)));

        var disabled = RenderScene.FromDocument(
            document,
            new RenderSceneOptions { IncludeOutlineTargets = false });
        Assert.Empty(disabled.OutlineTargets);
    }

    private static SceneDocument CreateTwoObjectDocument()
    {
        var root = new SceneNode("root");
        var storey = new SceneNode("storey-1")
        {
            SourceId = "storey-global",
            Name = "Level 1",
            Category = "IfcBuildingStorey",
        };
        storey.Children.Add(CreateObject(
            "wall-1",
            "wall-global",
            "IfcWall",
            new BoundingBox3(Vector3.Zero, Vector3.One),
            null));
        storey.Children.Add(CreateObject(
            "door-1",
            "door-global",
            "IfcDoor",
            new BoundingBox3(new Vector3(10f), new Vector3(11f)),
            "xbim-style:42"));
        root.Children.Add(storey);
        return new SceneDocument(root);
    }

    private static SceneNode CreateObject(
        string id,
        string sourceId,
        string category,
        BoundingBox3 bounds,
        string? materialId)
    {
        var node = new SceneNode(id)
        {
            SourceId = sourceId,
            Name = id,
            Category = category,
            Bounds = bounds,
        };
        node.Meshes.Add(CreateMesh(materialId));
        return node;
    }

    private static MeshData CreateMesh(string? materialId = null) =>
        new(TrianglePositions, TriangleIndices) { MaterialId = materialId };
}
