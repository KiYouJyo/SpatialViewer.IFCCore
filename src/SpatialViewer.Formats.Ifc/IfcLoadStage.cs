namespace SpatialViewer.Formats.Ifc;

public enum IfcLoadStage
{
    CheckingCache = 0,
    ReadingCache,
    Opening,
    Parsing,
    BuildingHierarchy,
    ExtractingMetadata,
    GeneratingGeometry,
    ExtractingGeometry,
    WritingCache,
    Completed,
}
