namespace SpatialViewer.Formats.Ifc;

public enum IfcDiagnosticSeverity
{
    Info,
    Warning,
    Error,
}

public sealed record IfcLoadDiagnostic(IfcDiagnosticSeverity Severity, string Code, string Message);
