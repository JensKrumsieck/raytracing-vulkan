using System.Numerics;
using System.Runtime.InteropServices;

namespace RaytracingVulkan.Primitives;

[StructLayout(LayoutKind.Explicit)]
public struct Sphere
{
    [FieldOffset(0)] public Vector3 Position;
    [FieldOffset(12)] public float Radius;
}
