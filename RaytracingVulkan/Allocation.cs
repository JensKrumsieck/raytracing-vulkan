using Silk.NET.Vulkan;

namespace RaytracingVulkan;

public abstract unsafe class Allocation : IDisposable
{
    protected readonly VkContext VkContext;
    protected Vk Vk => VkContext.Vk;
    protected Device Device => VkContext.Device;

    protected DeviceMemory Memory;
    protected Allocation(VkContext vkContext) => VkContext = vkContext;

    public Result MapMemory(ref void* pData) => Vk.MapMemory(Device, Memory, 0, Vk.WholeSize, 0, ref pData);
    public void UnmapMemory() => Vk.UnmapMemory(Device, Memory);

    public abstract void Dispose();
}
