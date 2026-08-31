using System.Numerics;
using SpatialViewer.Core.Geometry;

namespace SpatialViewer.Core;

public sealed class SceneDocument
{
    public SceneDocument(SceneNode root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public SceneNode Root { get; }

    public string? SourcePath { get; init; }

    public BoundingBox3? Bounds { get; set; }

    public BoundingBox3? WorldBounds { get; set; }

    public Vector3 WorldOrigin { get; set; }

    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
