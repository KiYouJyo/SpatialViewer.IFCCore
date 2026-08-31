namespace SpatialViewer.Formats.Ifc;

public sealed record IfcLoadProgress(IfcLoadStage Stage, int Percent, string Message);
