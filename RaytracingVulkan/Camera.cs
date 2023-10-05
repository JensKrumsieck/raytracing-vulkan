using System.Numerics;

namespace RaytracingVulkan;

public class Camera
{
    public Vector3 Position;
    public Vector3 Rotation;
    
    public Matrix4x4 Projection;
    public Matrix4x4 View;
    public Matrix4x4 InverseProjection;
    public Matrix4x4 InverseView;

    private float _viewportWidth;
    private float _viewportHeight;
    private float AspectRatio => _viewportWidth / _viewportHeight;
    private readonly float _verticalFov;
    private readonly float _nearClip;
    private readonly float _farClip;

    public Camera(float verticalFovDegrees, float nearClip, float farClip)
    {
        _verticalFov = verticalFovDegrees * MathF.PI / 180;
        _nearClip = nearClip;
        _farClip = farClip;
        RecalculateProjection();
        RecalculateView();
    }

    private void RecalculateProjection()
    {
        Projection = Matrix4x4.CreatePerspectiveFieldOfView(_verticalFov, AspectRatio, _nearClip, _farClip);
        Matrix4x4.Invert(Projection, out InverseProjection);
    }

    public void RecalculateView()
    {
        View = Matrix4x4.CreateLookAt(Position, Position + CalculateForward(), CalculateUp());
        Matrix4x4.Invert(View, out InverseView);
    }

    public Vector3 CalculateForward()
    {
        var degRot = Rotation * (MathF.PI / 180.0f);
        var quat = Quaternion.CreateFromYawPitchRoll(degRot.Y, degRot.X, degRot.Z);
        return Vector3.Transform(Vector3.UnitZ, quat);
    }

    public Vector3 CalculateUp()
    {
        var degRot = Rotation * (MathF.PI / 180.0f);
        var quat = Quaternion.CreateFromYawPitchRoll(degRot.Y, degRot.X, degRot.Z);
        return Vector3.Transform(Vector3.UnitY, quat);
    }
    
    public void Resize(uint newWidth, uint newHeight)
    {
        _viewportWidth = newWidth;
        _viewportHeight = newHeight;
        RecalculateProjection();
    }
}
