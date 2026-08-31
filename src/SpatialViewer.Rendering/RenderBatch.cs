using System.Numerics;
using SpatialViewer.Core.Geometry;

namespace SpatialViewer.Rendering;

public sealed record RenderInstance(
    string NodeId,
    string ObjectId,
    uint PickId,
    Matrix4x4 Transform,
    BoundingBox3? Bounds);

public sealed record RenderBatch(
    MeshData Mesh,
    string MaterialId,
    bool IsMaterialFallback,
    float Opacity,
    bool FlipWinding,
    IReadOnlyList<RenderInstance> Instances);
