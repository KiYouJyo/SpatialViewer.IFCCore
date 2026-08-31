namespace SpatialViewer.Formats.Ifc;

public interface IIfcModelReader
{
    ValueTask<IfcLoadResult> OpenAsync(
        string path,
        IfcOpenOptions? options = null,
        CancellationToken cancellationToken = default);
}
