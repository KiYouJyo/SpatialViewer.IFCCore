namespace SpatialViewer.Rendering.Windows;

public sealed class WindowsRenderBackend
{
    public string Name => "SpatialViewer Windows BIM Renderer";

    public bool IsInitialized { get; private set; }

    public void Initialize()
    {
        IsInitialized = true;
    }
}
