using System.Numerics;
using System.Runtime.InteropServices;

namespace RaytracingVulkan;

[StructLayout(LayoutKind.Sequential)]
public struct SceneParameters
{
    public Matrix4x4 CameraProjection;
    public Matrix4x4 InverseCameraProjection;
    public Matrix4x4 CameraView;
    public Matrix4x4 InverseCameraView;
    public uint FrameIndex;
}