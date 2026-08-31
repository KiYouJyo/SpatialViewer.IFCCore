namespace SpatialViewer.Formats.Ifc;

public sealed record IfcOpenOptions
{
    public bool IncludeGeometry { get; init; }
    public bool IncludeProperties { get; init; } = true;
    public bool PreserveOpeningElements { get; init; }
    public IProgress<IfcLoadProgress>? Progress { get; init; }
}
