namespace SpatialViewer.Formats.Ifc;

public enum IfcLoadStage
{
    Opening = 0,
    Parsing,
    BuildingHierarchy,
    ExtractingMetadata,
    Completed,
}
