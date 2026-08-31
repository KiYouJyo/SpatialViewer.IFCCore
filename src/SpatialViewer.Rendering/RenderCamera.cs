using System.Numerics;

namespace SpatialViewer.Rendering;

public enum CameraProjectionKind
{
    Perspective,
    Orthographic,
}

public sealed record RenderCamera(
    Vector3 Position,
    Vector3 Target,
    Vector3 Up,
    CameraProjectionKind Projection = CameraProjectionKind.Perspective,
    float VerticalFieldOfViewRadians = 1.0471976f,
    float OrthographicHeight = 10f,
    float NearPlane = 0.01f,
    float FarPlane = 100_000f)
{
    public Matrix4x4 CreateViewMatrix()
    {
        EnsureViewIsValid();
        return Matrix4x4.CreateLookAt(Position, Target, Up);
    }

    public Matrix4x4 CreateProjectionMatrix(float aspectRatio)
    {
        if (!float.IsFinite(aspectRatio) || aspectRatio <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(aspectRatio));
        }

        if (NearPlane <= 0f || FarPlane <= NearPlane)
        {
            throw new InvalidOperationException("Camera clipping planes are invalid.");
        }

        return Projection == CameraProjectionKind.Perspective
            ? Matrix4x4.CreatePerspectiveFieldOfView(
                VerticalFieldOfViewRadians,
                aspectRatio,
                NearPlane,
                FarPlane)
            : Matrix4x4.CreateOrthographic(
                OrthographicHeight * aspectRatio,
                OrthographicHeight,
                NearPlane,
                FarPlane);
    }

    public RenderCamera Pan(Vector3 offset) =>
        this with { Position = Position + offset, Target = Target + offset };

    public RenderCamera Zoom(float factor)
    {
        if (!float.IsFinite(factor) || factor <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(factor));
        }

        if (Projection == CameraProjectionKind.Orthographic)
        {
            return this with { OrthographicHeight = Math.Max(OrthographicHeight / factor, 0.0001f) };
        }

        var offset = Position - Target;
        return this with { Position = Target + (offset / factor) };
    }

    public RenderCamera Orbit(float yawRadians, float pitchRadians)
    {
        EnsureViewIsValid();
        var up = Vector3.Normalize(Up);
        var offset = Position - Target;
        var yaw = Quaternion.CreateFromAxisAngle(up, yawRadians);
        offset = Vector3.Transform(offset, yaw);

        var forward = Vector3.Normalize(-offset);
        var right = Vector3.Cross(forward, up);
        if (right.LengthSquared() < 1e-8f)
        {
            right = Vector3.UnitX;
        }
        else
        {
            right = Vector3.Normalize(right);
        }

        var pitch = Quaternion.CreateFromAxisAngle(right, pitchRadians);
        offset = Vector3.Transform(offset, pitch);
        var rotatedUp = Vector3.Normalize(Vector3.Transform(up, pitch));
        return this with { Position = Target + offset, Up = rotatedUp };
    }

    private void EnsureViewIsValid()
    {
        if ((Position - Target).LengthSquared() < 1e-8f || Up.LengthSquared() < 1e-8f)
        {
            throw new InvalidOperationException("Camera position, target and up vector do not define a valid view.");
        }
    }
}
