using System.Numerics;
using SpatialViewer.Rendering;
using Xunit;

namespace SpatialViewer.Rendering.Tests;

public sealed class RenderCameraTests
{
    [Fact]
    public void CameraSupportsPanOrbitAndPerspectiveZoomWithoutChangingTargetIdentity()
    {
        var camera = new RenderCamera(
            new Vector3(0f, -10f, 5f),
            Vector3.Zero,
            Vector3.UnitZ);

        var panned = camera.Pan(new Vector3(2f, 3f, 0f));
        Assert.Equal(new Vector3(2f, 3f, 0f), panned.Target);
        Assert.Equal(camera.Position + new Vector3(2f, 3f, 0f), panned.Position);

        var distance = Vector3.Distance(camera.Position, camera.Target);
        var zoomed = camera.Zoom(2f);
        Assert.InRange(Vector3.Distance(zoomed.Position, zoomed.Target), (distance / 2f) - 0.001f, (distance / 2f) + 0.001f);

        var orbited = camera.Orbit(0.5f, 0.25f);
        Assert.InRange(Vector3.Distance(orbited.Position, orbited.Target), distance - 0.001f, distance + 0.001f);
        Assert.NotEqual(camera.Position, orbited.Position);
        _ = orbited.CreateViewMatrix();
        _ = orbited.CreateProjectionMatrix(16f / 9f);
    }

    [Fact]
    public void OrthographicZoomChangesHeightInsteadOfCameraDistance()
    {
        var camera = new RenderCamera(
            new Vector3(0f, -10f, 5f),
            Vector3.Zero,
            Vector3.UnitZ,
            CameraProjectionKind.Orthographic,
            OrthographicHeight: 20f);

        var zoomed = camera.Zoom(2f);

        Assert.Equal(camera.Position, zoomed.Position);
        Assert.Equal(10f, zoomed.OrthographicHeight);
        _ = zoomed.CreateProjectionMatrix(1.5f);
    }
}
