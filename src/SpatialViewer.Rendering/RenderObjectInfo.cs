namespace SpatialViewer.Rendering;

public sealed record RenderObjectInfo(
    string ObjectId,
    uint PickId,
    string SceneNodeId,
    string? SourceId,
    string? Name,
    string? Category,
    string? StoreyId,
    string? StoreyName);
