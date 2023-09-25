using System.Numerics;
using System.Runtime.InteropServices;

namespace RaytracingVulkan.Primitives;

[StructLayout(LayoutKind.Explicit, Size = 112)]
public struct Triangle
{
    [FieldOffset(0)] public Vector3 V0;
    [FieldOffset(16)] public Vector3 V1;
    [FieldOffset(32)] public Vector3 V2;

    [FieldOffset(48)] public Vector3 N0;
    [FieldOffset(64)] public Vector3 N1;
    [FieldOffset(80)] public Vector3 N2;

    [FieldOffset(96)] public bool HasNormals;
    
    public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 n0, Vector3 n1, Vector3 n2)
    {
        V0 = v0;
        V1 = v1;
        V2 = v2;
        N0 = n0;
        N1 = n1;
        N2 = n2;
        HasNormals = true;
    }

    public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
    {
        V0 = v0;
        V1 = v1;
        V2 = v2;
        N0 = N1 = N2 = Vector3.Zero;
        HasNormals = false;
    }
}
