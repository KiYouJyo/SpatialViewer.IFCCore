namespace SpatialViewer.Rendering;

public sealed class RenderSceneOptions
{
    public RenderVisibilityState Visibility { get; init; } = new();

    public RenderAppearanceOverrides Appearance { get; init; } = new();

    public SectionBox? SectionBox { get; init; }

    public bool IncludeOutlineTargets { get; init; } = true;
}
