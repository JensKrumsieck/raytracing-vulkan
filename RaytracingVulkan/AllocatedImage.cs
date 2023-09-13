using Silk.NET.Vulkan;

namespace RaytracingVulkan;

public struct AllocatedImage
{
    public Image Image;
    public DeviceMemory Memory;
}