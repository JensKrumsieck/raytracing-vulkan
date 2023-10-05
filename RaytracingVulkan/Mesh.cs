using System.Runtime.CompilerServices;
using RaytracingVulkan.Memory;
using RaytracingVulkan.Primitives;
using Silk.NET.Vulkan;
using Buffer = System.Buffer;

namespace RaytracingVulkan;

public class Mesh
{
    public Vertex[] Vertices;
    public uint[] Indices;

    public VkBuffer? VertexBuffer;
    public VkBuffer? IndexBuffer;

    public void CreateBuffers(VkContext ctx)
    {
        CreateVertexBuffer(ctx);
        CreateIndexBuffer(ctx);
    }
    private unsafe void CreateVertexBuffer(VkContext ctx)
    {
        using var stagingBuffer = VkBuffer.CreateAndCopyToStagingBuffer(ctx, Vertices);
        VertexBuffer = new VkBuffer(ctx, (uint) (Vertices.Length * sizeof(Vertex)), BufferUsageFlags.IndexBufferBit | BufferUsageFlags.TransferDstBit,
                                    MemoryPropertyFlags.DeviceLocalBit);
        stagingBuffer.CopyToBuffer(VertexBuffer); 
    }
    private void CreateIndexBuffer(VkContext ctx)
    {
        using var stagingBuffer = VkBuffer.CreateAndCopyToStagingBuffer(ctx, Indices);
        IndexBuffer = new VkBuffer(ctx, (uint) (Indices.Length * sizeof(uint)), BufferUsageFlags.VertexBufferBit | BufferUsageFlags.TransferDstBit,
                                   MemoryPropertyFlags.DeviceLocalBit);
        stagingBuffer.CopyToBuffer(IndexBuffer); 
    }
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