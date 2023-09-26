using RaytracingVulkan.Primitives;

namespace RaytracingVulkan;

public class Mesh
{
    public Vertex[] Vertices;
    public uint[] Indices;

    public Triangle[] ToTriangles()
    {
        var triangles = new List<Triangle>();
        for (var i = 0; i < Indices.Length; i += 3)
        {
            var v0 = Vertices[Indices[i]];
            var v1 = Vertices[Indices[i + 1]];
            var v2 = Vertices[Indices[i + 2]];
            triangles.Add(new Triangle(v0.Position, v1.Position, v2.Position, v0.Normal, v1.Normal, v2.Normal));
        }
        
        return triangles.ToArray();
    }
}