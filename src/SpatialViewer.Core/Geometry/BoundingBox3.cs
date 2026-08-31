using System.Numerics;

namespace SpatialViewer.Core.Geometry;

public readonly record struct BoundingBox3(Vector3 Min, Vector3 Max)
{
    public Vector3 Size => Max - Min;

    public Vector3 Center => (Min + Max) * 0.5f;
}
