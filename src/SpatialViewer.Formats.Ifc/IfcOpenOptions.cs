namespace SpatialViewer.Formats.Ifc;

public sealed record IfcOpenOptions
{
    public bool IncludeGeometry { get; init; } = true;

    public bool IncludeProperties { get; init; } = true;

    public bool IncludeQuantities { get; init; } = true;

    public bool RebaseLargeCoordinates { get; init; } = true;
}
