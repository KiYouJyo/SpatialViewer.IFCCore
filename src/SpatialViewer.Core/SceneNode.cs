using System.Numerics;
using SpatialViewer.Core.Geometry;

namespace SpatialViewer.Core;

public sealed class SceneNode
{
    public SceneNode(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id;
    }

    public string Id { get; }

    public string? SourceId { get; init; }

    public string? Name { get; init; }

    public string? Category { get; init; }

    public Matrix4x4 Transform { get; init; } = Matrix4x4.Identity;

    public IList<SceneNode> Children { get; } = new List<SceneNode>();

    public IList<MeshData> Meshes { get; } = new List<MeshData>();

    public IDictionary<string, SceneProperty> Properties { get; } = new Dictionary<string, SceneProperty>(StringComparer.Ordinal);
}
