using System.Numerics;

namespace SpatialViewer.Core.Geometry;

public readonly record struct BoundingBox3(Vector3 Min, Vector3 Max)
{
    public Vector3 Size => Max - Min;

    public Vector3 Center => (Min + Max) * 0.5f;

    public static BoundingBox3 FromPoints(IReadOnlyList<Vector3> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        if (points.Count == 0)
        {
            throw new ArgumentException("At least one point is required.", nameof(points));
        }

        var min = points[0];
        var max = points[0];
        for (var index = 1; index < points.Count; index++)
        {
            min = Vector3.Min(min, points[index]);
            max = Vector3.Max(max, points[index]);
        }

        return new BoundingBox3(min, max);
    }

    public BoundingBox3 Union(BoundingBox3 other) =>
        new(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));

    public BoundingBox3 Translate(Vector3 offset) => new(Min + offset, Max + offset);

    public bool Intersects(BoundingBox3 other) =>
        Min.X <= other.Max.X && Max.X >= other.Min.X &&
        Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
        Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
}
