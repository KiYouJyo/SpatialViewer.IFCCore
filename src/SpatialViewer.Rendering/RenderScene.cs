using SpatialViewer.Core;
using SpatialViewer.Core.Geometry;

namespace SpatialViewer.Rendering;

public sealed class RenderScene
{
    public IReadOnlyList<RenderMesh> Meshes { get; init; } = Array.Empty<RenderMesh>();

    public IReadOnlyList<RenderBatch> Batches { get; init; } = Array.Empty<RenderBatch>();

    public IReadOnlyList<RenderObjectInfo> Objects { get; init; } = Array.Empty<RenderObjectInfo>();

    public IReadOnlyDictionary<uint, RenderObjectInfo> PickMap { get; init; } =
        new Dictionary<uint, RenderObjectInfo>();

    public IReadOnlyList<RenderOutlineTarget> OutlineTargets { get; init; } = Array.Empty<RenderOutlineTarget>();

    public SectionBox? SectionBox { get; init; }

    public static RenderScene FromDocument(SceneDocument document, RenderSceneOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        options ??= new RenderSceneOptions();

        var candidates = new List<RenderCandidate>();
        CollectCandidates(document.Root, null, null, candidates);

        var objectIds = candidates
            .Select(candidate => candidate.ObjectId)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();
        var pickIds = BuildPickIds(objectIds);
        var allObjects = BuildObjectMap(candidates, pickIds);

        var meshes = new List<RenderMesh>();
        var visibleObjects = new Dictionary<string, RenderObjectInfo>(StringComparer.Ordinal);
        var outlineBounds = new Dictionary<string, BoundingBox3?>(StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            var item = allObjects[candidate.ObjectId];
            if (!options.Visibility.IsVisible(item) || !IntersectsSection(candidate.GeometryNode.Bounds, options.SectionBox))
            {
                continue;
            }

            var opacity = options.Appearance.ResolveOpacity(item);
            var materialId = candidate.Mesh.MaterialId;
            var isFallback = string.IsNullOrWhiteSpace(materialId);
            materialId = isFallback
                ? options.Appearance.ResolveFallbackMaterial(item.Category)
                : materialId;

            meshes.Add(new RenderMesh(
                candidate.GeometryNode.Id,
                item.ObjectId,
                item.PickId,
                candidate.Mesh,
                candidate.GeometryNode.Transform,
                candidate.GeometryNode.FlipWinding,
                candidate.GeometryNode.Bounds,
                materialId!,
                isFallback,
                opacity));
            visibleObjects.TryAdd(item.ObjectId, item);
            AccumulateBounds(outlineBounds, item.ObjectId, candidate.GeometryNode.Bounds);
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

    private static IReadOnlyList<RenderBatch> BuildBatches(IEnumerable<RenderMesh> meshes) =>
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

    private static void CollectCandidates(
        SceneNode node,
        SceneNode? semanticOwner,
        SceneNode? storey,
        ICollection<RenderCandidate> destination)
    {
        var isGeometryNode = string.Equals(node.Category, "IFC.Geometry", StringComparison.Ordinal);
        var currentOwner = isGeometryNode ? semanticOwner : node;
        var currentStorey = IsStorey(node) ? node : storey;
        var meshOwner = isGeometryNode && semanticOwner is not null ? semanticOwner : node;

        foreach (var mesh in node.Meshes)
        {
            destination.Add(new RenderCandidate(
                node,
                meshOwner,
                currentStorey,
                mesh,
                GetStableId(meshOwner)));
        }

        foreach (var child in node.Children)
        {
            CollectCandidates(child, currentOwner, currentStorey, destination);
        }
    }

    private static Dictionary<string, RenderObjectInfo> BuildObjectMap(
        IEnumerable<RenderCandidate> candidates,
        IReadOnlyDictionary<string, uint> pickIds)
    {
        var result = new Dictionary<string, RenderObjectInfo>(StringComparer.Ordinal);
        foreach (var candidate in candidates)
        {
            if (result.ContainsKey(candidate.ObjectId))
            {
                continue;
            }

            var owner = candidate.SemanticOwner;
            var storey = candidate.Storey;
            result.Add(
                candidate.ObjectId,
                new RenderObjectInfo(
                    candidate.ObjectId,
                    pickIds[candidate.ObjectId],
                    owner.Id,
                    owner.SourceId,
                    owner.Name,
                    owner.Category,
                    storey is null ? null : GetStableId(storey),
                    storey?.Name));
        }

        return result;
    }

    private static Dictionary<string, uint> BuildPickIds(IEnumerable<string> objectIds)
    {
        var result = new Dictionary<string, uint>(StringComparer.Ordinal);
        var used = new HashSet<uint>();
        foreach (var objectId in objectIds)
        {
            var salt = 0;
            var pickId = HashPickId(objectId);
            while (pickId == 0 || !used.Add(pickId))
            {
                salt++;
                pickId = HashPickId($"{objectId}#{salt}");
            }

            result.Add(objectId, pickId);
        }

        return result;
    }

    private static uint HashPickId(string value)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        var hash = offset;
        foreach (var character in value)
        {
            hash ^= character;
            hash *= prime;
        }

        return hash;
    }

    private static bool IntersectsSection(BoundingBox3? bounds, SectionBox? sectionBox) =>
        sectionBox is not { Enabled: true } || bounds is null || bounds.Value.Intersects(sectionBox.Bounds);

    private static void AccumulateBounds(
        IDictionary<string, BoundingBox3?> destination,
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

    private static bool IsStorey(SceneNode node) =>
        string.Equals(node.Category, "IfcBuildingStorey", StringComparison.OrdinalIgnoreCase);

    private static string GetStableId(SceneNode node) =>
        string.IsNullOrWhiteSpace(node.SourceId) ? node.Id : node.SourceId;

    private sealed record RenderCandidate(
        SceneNode GeometryNode,
        SceneNode SemanticOwner,
        SceneNode? Storey,
        MeshData Mesh,
        string ObjectId);

    private sealed record RenderBatchKey(
        MeshData Mesh,
        string MaterialId,
        bool IsMaterialFallback,
        float Opacity,
        bool FlipWinding);
}
