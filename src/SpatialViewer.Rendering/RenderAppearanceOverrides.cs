namespace SpatialViewer.Rendering;

public sealed class RenderAppearanceOverrides
{
    public float DefaultOpacity { get; init; } = 1f;

    public IDictionary<string, float> ObjectOpacity { get; } =
        new Dictionary<string, float>(StringComparer.Ordinal);

    public IDictionary<string, float> CategoryOpacity { get; } =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

    public string FallbackMaterialPrefix { get; init; } = "fallback";

    internal float ResolveOpacity(RenderObjectInfo item)
    {
        if (ObjectOpacity.TryGetValue(item.ObjectId, out var objectOpacity))
        {
            return NormalizeOpacity(objectOpacity);
        }

        if (item.Category is not null && CategoryOpacity.TryGetValue(item.Category, out var categoryOpacity))
        {
            return NormalizeOpacity(categoryOpacity);
        }

        return NormalizeOpacity(DefaultOpacity);
    }

    internal string ResolveFallbackMaterial(string? category)
    {
        var suffix = string.IsNullOrWhiteSpace(category) ? "default" : category.Trim();
        return $"{FallbackMaterialPrefix}:{suffix}";
    }

    private static float NormalizeOpacity(float value) =>
        float.IsFinite(value) ? Math.Clamp(value, 0f, 1f) : 1f;
}
