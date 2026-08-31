using SpatialViewer.Core;

namespace SpatialViewer.Formats.Ifc;

public sealed record IfcLoadResult(
    SceneDocument Document,
    IfcSchemaVersion Schema,
    IReadOnlyList<IfcLoadDiagnostic> Diagnostics,
    TimeSpan Elapsed);
