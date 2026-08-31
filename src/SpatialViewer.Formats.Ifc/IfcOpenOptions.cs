namespace SpatialViewer.Formats.Ifc;

public sealed record IfcOpenOptions
{
    public bool IncludeGeometry { get; init; }
    public bool IncludeProperties { get; init; } = true;
    public bool PreserveOpeningElements { get; init; }
    public bool RebaseLargeCoordinates { get; init; } = true;
    public double LargeCoordinateThresholdMetres { get; init; } = 10_000d;
    public IProgress<IfcLoadProgress>? Progress { get; init; }
}
