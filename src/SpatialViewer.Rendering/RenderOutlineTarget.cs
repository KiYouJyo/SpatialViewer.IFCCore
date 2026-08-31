using SpatialViewer.Core.Geometry;

namespace SpatialViewer.Rendering;

public sealed record RenderOutlineTarget(
    string ObjectId,
    uint PickId,
    BoundingBox3? Bounds);
