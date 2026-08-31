using System.Numerics;

namespace SpatialViewer.Core.Geometry;

public sealed class MeshData
{
    public MeshData(IReadOnlyList<Vector3> positions, IReadOnlyList<int> indices)
    {
        Positions = positions ?? throw new ArgumentNullException(nameof(positions));
        Indices = indices ?? throw new ArgumentNullException(nameof(indices));

        if (Positions.Count == 0)
        {
            throw new ArgumentException("A mesh must contain at least one position.", nameof(positions));
        }

        if (Indices.Count % 3 != 0)
        {
            throw new ArgumentException("Triangle index count must be divisible by three.", nameof(indices));
        }

        Bounds = BoundingBox3.FromPoints(Positions);
    }

    public IReadOnlyList<Vector3> Positions { get; }

    public IReadOnlyList<int> Indices { get; }

    public IReadOnlyList<Vector3>? Normals { get; init; }

    public string? MaterialId { get; init; }

    public BoundingBox3 Bounds { get; }

    public int TriangleCount => Indices.Count / 3;
}
