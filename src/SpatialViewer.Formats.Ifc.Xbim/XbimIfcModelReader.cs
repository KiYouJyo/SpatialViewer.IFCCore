namespace SpatialViewer.Formats.Ifc.Xbim;

public sealed class XbimIfcModelReader : IIfcModelReader
{
    public ValueTask<IfcLoadResult> OpenAsync(
        string path,
        IfcOpenOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromException<IfcLoadResult>(
            new NotSupportedException(
                "The xBIM adapter boundary is established, but package integration is scheduled for Phase 1."));
    }
}
