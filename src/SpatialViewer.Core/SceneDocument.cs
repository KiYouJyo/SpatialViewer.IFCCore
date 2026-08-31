namespace SpatialViewer.Core;

public sealed class SceneDocument
{
    public SceneDocument(SceneNode root)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public SceneNode Root { get; }

    public string? SourcePath { get; init; }

    public IDictionary<string, string> Metadata { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
