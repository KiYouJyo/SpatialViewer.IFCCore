using System.Numerics;
using SpatialViewer.Core.Geometry;

namespace SpatialViewer.Rendering;

public sealed record RenderMesh(
    string NodeId,
    string ObjectId,
    uint PickId,
    MeshData Mesh,
    Matrix4x4 Transform,
    bool FlipWinding,
    BoundingBox3? Bounds,
    string MaterialId,
    bool IsMaterialFallback,
    float Opacity);
