using SpatialViewer.Core;
using SpatialViewer.Core.Geometry;

namespace SpatialViewer.Rendering;

public sealed class RenderSceneIndex
{
    private readonly List<RenderMesh> _meshTemplates;
    private readonly Dictionary<string, RenderObjectInfo> _objects;

    private RenderSceneIndex(List<RenderMesh> meshTemplates, Dictionary<string, RenderObjectInfo> objects)
    {
        _meshTemplates = meshTemplates;
        _objects = objects;
    }

    public int MeshTemplateCount => _meshTemplates.Count;

    public int ObjectCount => _objects.Count;

    public static RenderSceneIndex Create(SceneDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var baseline = RenderScene.FromDocument(document);
        return new RenderSceneIndex(
            baseline.Meshes.ToList(),
            baseline.Objects.ToDictionary(item => item.ObjectId, StringComparer.Ordinal));
    }

    public RenderScene Build(RenderSceneOptions? options = null)
    {
        options ??= new RenderSceneOptions();
        var meshes = new List<RenderMesh>(_meshTemplates.Count);
        var visibleObjects = new Dictionary<string, RenderObjectInfo>(StringComparer.Ordinal);
        var outlineBounds = new Dictionary<string, BoundingBox3?>(StringComparer.Ordinal);

        foreach (var template in _meshTemplates)
        {
            var item = _objects[template.ObjectId];
            if (!options.Visibility.IsVisible(item) || !IntersectsSection(template.Bounds, options.SectionBox))
            {
                continue;
            }

            var opacity = options.Appearance.ResolveOpacity(item);
            var sourceMaterial = template.Mesh.MaterialId;
            var isFallback = string.IsNullOrWhiteSpace(sourceMaterial);
            var materialId = isFallback
                ? options.Appearance.ResolveFallbackMaterial(item.Category)
                : sourceMaterial!;

            meshes.Add(template with
            {
                MaterialId = materialId,
                IsMaterialFallback = isFallback,
                Opacity = opacity,
            });
            visibleObjects.TryAdd(item.ObjectId, item);
            AccumulateBounds(outlineBounds, item.ObjectId, template.Bounds);
        }

        var objects = visibleObjects.Values
            .OrderBy(item => item.ObjectId, StringComparer.Ordinal)
            .ToList();
        var pickMap = objects.ToDictionary(item => item.PickId);
        var outlines = options.IncludeOutlineTargets
            ? objects.Select(item => new RenderOutlineTarget(
                    item.ObjectId,
                    item.PickId,
                    outlineBounds.GetValueOrDefault(item.ObjectId)))
                .ToList()
            : [];

        return new RenderScene
        {
            Meshes = meshes,
            Batches = BuildBatches(meshes),
            Objects = objects,
            PickMap = pickMap,
            OutlineTargets = outlines,
            SectionBox = options.SectionBox is { Enabled: true } ? options.SectionBox : null,
        };
    }

    private static List<RenderBatch> BuildBatches(List<RenderMesh> meshes) =>
        meshes.GroupBy(mesh => new RenderBatchKey(
                mesh.Mesh,
                mesh.MaterialId,
                mesh.IsMaterialFallback,
                mesh.Opacity,
                mesh.FlipWinding))
            .Select(group => new RenderBatch(
                group.Key.Mesh,
                group.Key.MaterialId,
                group.Key.IsMaterialFallback,
                group.Key.Opacity,
                group.Key.FlipWinding,
                group.Select(mesh => new RenderInstance(
                        mesh.NodeId,
                        mesh.ObjectId,
                        mesh.PickId,
                        mesh.Transform,
                        mesh.Bounds))
                    .ToList()))
            .ToList();

    private static bool IntersectsSection(BoundingBox3? bounds, SectionBox? sectionBox) =>
        sectionBox is not { Enabled: true } || bounds is null || bounds.Value.Intersects(sectionBox.Bounds);

    private static void AccumulateBounds(
        Dictionary<string, BoundingBox3?> destination,
        string objectId,
        BoundingBox3? bounds)
    {
        if (!destination.TryGetValue(objectId, out var existing))
        {
            destination[objectId] = bounds;
            return;
        }

        if (existing is null)
        {
            destination[objectId] = bounds;
        }
        else if (bounds is not null)
        {
            destination[objectId] = existing.Value.Union(bounds.Value);
        }
    }

    private sealed record RenderBatchKey(
        MeshData Mesh,
        string MaterialId,
        bool IsMaterialFallback,
        float Opacity,
        bool FlipWinding);
}
