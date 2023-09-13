using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace RaytracingVulkan;

public struct AllocatedBuffer
{
    public Buffer Buffer;
    public DeviceMemory Memory;
}