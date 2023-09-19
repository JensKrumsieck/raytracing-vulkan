using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace RaytracingVulkan;

public sealed unsafe class VkBuffer : Allocation
{
    public readonly uint Size;
    public Buffer Buffer;

    private readonly BufferUsageFlags _bufferUsageFlags;
    private readonly MemoryPropertyFlags _memoryPropertyFlags;

    public VkBuffer(VkContext context, uint size, BufferUsageFlags usageFlags, MemoryPropertyFlags memoryFlags) : base(context)
    {
        Size = size;
        _bufferUsageFlags = usageFlags;
        _memoryPropertyFlags = memoryFlags;

        //create buffer
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Usage = _bufferUsageFlags,
            Size = size,
            SharingMode = SharingMode.Exclusive
        };

        Vk.CreateBuffer(Device, bufferInfo, null, out Buffer);

        //allocate and bind memory
        Vk.GetBufferMemoryRequirements(Device, Buffer, out var memReq);
        Memory = VkContext.AllocateMemory(memReq, _memoryPropertyFlags);
        Vk.BindBufferMemory(Device, Buffer, Memory, 0);
    }

    public void CopyToImage(VkImage vkImage)
    {
        var cmd = VkContext.BeginSingleTimeCommands();
        var layers = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1);
        var copyRegion = new BufferImageCopy(0, 0, 0, layers, default, vkImage.ImageExtent);
        Vk.CmdCopyBufferToImage(cmd, Buffer, vkImage.Image, ImageLayout.TransferDstOptimal, 1, copyRegion);
        VkContext.EndSingleTimeCommands(cmd);
    }

    public DescriptorBufferInfo GetBufferInfo() => new()
    {
        Buffer = Buffer,
        Offset = 0,
        Range = Size
    };
    
    public override void Dispose()
    {
        UnmapMemory();
        Vk.FreeMemory(Device, Memory, null);
        Vk.DestroyBuffer(Device, Buffer, null);
    }
}