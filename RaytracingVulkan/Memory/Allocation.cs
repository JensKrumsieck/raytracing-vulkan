using Silk.NET.Vulkan;

namespace RaytracingVulkan.Memory;

public abstract unsafe class Allocation : IDisposable
{
    protected readonly VkContext VkContext;
    protected Vk Vk => VkContext.Vk;
    protected Device Device => VkContext.Device;

    protected DeviceMemory Memory;
    protected Allocation(VkContext vkContext) => VkContext = vkContext;

    protected bool HostMapped;
    
    public Result MapMemory(ref void* pData)
    {
        HostMapped = true;
        return Vk.MapMemory(Device, Memory, 0, Vk.WholeSize, 0, ref pData);
    }
    public void UnmapMemory()
    {
        HostMapped = false;
        Vk.UnmapMemory(Device, Memory);
    }

    public abstract void Dispose();
}
