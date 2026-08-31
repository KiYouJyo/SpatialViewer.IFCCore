namespace SpatialViewer.Rendering;

public sealed class RenderVisibilityState
{
    public ISet<string> HiddenObjectIds { get; } = new HashSet<string>(StringComparer.Ordinal);

    public ISet<string> IsolatedObjectIds { get; } = new HashSet<string>(StringComparer.Ordinal);

    public ISet<string> HiddenCategories { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public ISet<string> HiddenStoreyIds { get; } = new HashSet<string>(StringComparer.Ordinal);

    internal bool IsVisible(RenderObjectInfo item)
    {
        if (HiddenObjectIds.Contains(item.ObjectId))
        {
            return false;
        }

        if (IsolatedObjectIds.Count > 0 && !IsolatedObjectIds.Contains(item.ObjectId))
        {
            return false;
        }

        if (item.Category is not null && HiddenCategories.Contains(item.Category))
        {
            return false;
        }

        return item.StoreyId is null || !HiddenStoreyIds.Contains(item.StoreyId);
    }
}
