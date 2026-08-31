using SpatialViewer.Core;

namespace SpatialViewer.Rendering;

public sealed class RenderScene
{
    public IReadOnlyList<RenderMesh> Meshes { get; init; } = Array.Empty<RenderMesh>();

    public static RenderScene FromDocument(SceneDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var meshes = new List<RenderMesh>();
        AppendNode(document.Root, meshes);
        return new RenderScene { Meshes = meshes };
    }

    private static void AppendNode(SceneNode node, ICollection<RenderMesh> output)
    {
        foreach (var mesh in node.Meshes)
        {
            output.Add(new RenderMesh(node.Id, mesh, node.Transform, node.FlipWinding, node.Bounds));
        }

        foreach (var child in node.Children)
        {
            AppendNode(child, output);
        }
    }
}
