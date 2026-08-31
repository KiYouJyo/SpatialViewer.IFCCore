using SpatialViewer.Core;
using Xunit;

namespace SpatialViewer.Core.Tests;

public sealed class SceneDocumentTests
{
    [Fact]
    public void Document_preserves_root_and_children()
    {
        var root = new SceneNode("project");
        root.Children.Add(new SceneNode("building"));

        var document = new SceneDocument(root);

        Assert.Same(root, document.Root);
        Assert.Single(document.Root.Children);
    }
}
