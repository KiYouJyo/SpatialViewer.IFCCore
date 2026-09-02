namespace SpatialViewer.Formats.Ifc;

public sealed record IfcModelCacheOptions
{
    public bool EnableMemoryCache { get; init; } = true;

    public int MemoryEntryLimit { get; init; } = 4;

    public string? DiskCacheDirectory { get; init; }

    public bool EnableDiskCache => !string.IsNullOrWhiteSpace(DiskCacheDirectory);
}
