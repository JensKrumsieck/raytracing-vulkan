using System;
using System.Numerics;

namespace RaytracingVulkan.UI;

public class Camera
{
    public Vector3 Position { get; set; }
    public Vector3 Forward;
    
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
        Forward = -Vector3.UnitZ;
        Position = Vector3.Zero;
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
        View = Matrix4x4.CreateLookAt(Position, Position + Forward, Vector3.UnitY);
        Matrix4x4.Invert(View, out InverseView);
    }

    public void Resize(uint newWidth, uint newHeight)
    {
        _viewportWidth = newWidth;
        _viewportHeight = newHeight;
        RecalculateProjection();
    }
}
