using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace RaytracingVulkan;

public unsafe partial class VkContext
{
    public AllocatedImage CreateImage(uint width, uint height, Format imageFormat, MemoryPropertyFlags memoryFlags, ImageUsageFlags imageUsageFlags)
    {
        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D(width, height, 1),
            Format = imageFormat,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined,
            Tiling = ImageTiling.Optimal,
            Usage = imageUsageFlags,
            MipLevels = 1,
            ArrayLayers = 1
        };
        _vk.CreateImage(_device, imageInfo, null, out var image);
        var deviceMemory = AllocateImage(image, memoryFlags);
        _vk.BindImageMemory(_device, image, deviceMemory, 0);
        return new AllocatedImage {Image = image, Memory = deviceMemory};
    }

    public void DestroyImage(AllocatedImage allocatedImage)
    {
        _vk.FreeMemory(_device, allocatedImage.Memory, null);
        _vk.DestroyImage(_device, allocatedImage.Image, null);
    }

    public AllocatedBuffer CreateBuffer(uint size, BufferUsageFlags usageFlags, MemoryPropertyFlags memoryFlags)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Usage = usageFlags,
            Size = size,
            SharingMode = SharingMode.Exclusive
        };
        _vk.CreateBuffer(_device, bufferInfo, null, out var buffer);
        var deviceMemory = AllocateBuffer(buffer, memoryFlags);
        _vk.BindBufferMemory(_device, buffer, deviceMemory, 0);
        return new AllocatedBuffer{Buffer = buffer, Memory = deviceMemory};
    }
   
    public void DestroyBuffer(AllocatedBuffer allocatedBuffer)
    {
        _vk.FreeMemory(_device, allocatedBuffer.Memory, null);
        _vk.DestroyBuffer(_device, allocatedBuffer.Buffer, null);
    }
    
    public Result MapMemory(DeviceMemory memory, ref void* pData) => _vk.MapMemory(_device, memory, 0, Vk.WholeSize, 0, ref pData);
    public void UnmapMemory(DeviceMemory memory) => _vk.UnmapMemory(_device, memory);

    public void TransitionImageLayout(Image image, ImageLayout oldLayout, ImageLayout newLayout)
    {
        var cmd = BeginSingleTimeCommands();
        var range = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1);
        var barrierInfo = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            Image = image,
            SubresourceRange = range,
        };
        
        //determining AccessMasks and PipelineStageFlags from layouts
        PipelineStageFlags srcStage;
        PipelineStageFlags dstStage;
        if (oldLayout == ImageLayout.Undefined && newLayout is ImageLayout.TransferDstOptimal or ImageLayout.TransferSrcOptimal)
        {
            barrierInfo.SrcAccessMask = 0;
            barrierInfo.DstAccessMask = AccessFlags.TransferWriteBit;
            srcStage = PipelineStageFlags.TopOfPipeBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrierInfo.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrierInfo.DstAccessMask = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.TransferBit;
            dstStage = PipelineStageFlags.FragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.TransferSrcOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrierInfo.SrcAccessMask = AccessFlags.TransferReadBit;
            barrierInfo.DstAccessMask = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.TransferBit;
            dstStage = PipelineStageFlags.FragmentShaderBit;
        }
        else if (oldLayout == ImageLayout.ShaderReadOnlyOptimal && newLayout == ImageLayout.TransferSrcOptimal)
        {
            barrierInfo.SrcAccessMask = AccessFlags.ShaderReadBit;
            barrierInfo.DstAccessMask = AccessFlags.TransferReadBit;
            srcStage = PipelineStageFlags.FragmentShaderBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.General)
        {
            barrierInfo.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrierInfo.DstAccessMask = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.TransferBit;
            dstStage = PipelineStageFlags.ComputeShaderBit;
        }
        else if (oldLayout == ImageLayout.TransferSrcOptimal && newLayout == ImageLayout.General)
        {
            barrierInfo.SrcAccessMask = AccessFlags.TransferReadBit;
            barrierInfo.DstAccessMask = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.TransferBit;
            dstStage = PipelineStageFlags.ComputeShaderBit;
        }
        else if (oldLayout == ImageLayout.General && newLayout == ImageLayout.TransferSrcOptimal)
        {
            barrierInfo.SrcAccessMask = AccessFlags.ShaderReadBit;
            barrierInfo.DstAccessMask = AccessFlags.TransferReadBit;
            srcStage = PipelineStageFlags.ComputeShaderBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else throw new Exception("Currently unsupported Layout Transition");
        
        _vk.CmdPipelineBarrier(cmd, srcStage, dstStage, 0, 0, null, 0, null, 1, barrierInfo);
        EndSingleTimeCommands(cmd);
    }

    public void CopyBufferToImage(Buffer buffer, Image image, Extent3D imageExtent)
    {
        var cmd = BeginSingleTimeCommands();
        var layers = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1);
        var copyRegion = new BufferImageCopy(0, 0, 0, layers, default, imageExtent);
        _vk.CmdCopyBufferToImage(cmd, buffer, image, ImageLayout.TransferDstOptimal, 1, copyRegion);
        EndSingleTimeCommands(cmd);
    }

    public void CopyImageToBuffer(Image image, Buffer buffer, Extent3D imageExtent, ImageLayout layout)
    {
        var cmd = BeginSingleTimeCommands();
        var layers = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1);
        var copyRegion = new BufferImageCopy(0, 0, 0, layers, default, imageExtent);
        _vk.CmdCopyImageToBuffer(cmd, image, layout, buffer, 1, copyRegion);
        EndSingleTimeCommands(cmd);
    }
    
    public CommandBuffer BeginSingleTimeCommands()
    {
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            CommandBufferCount = 1,
            Level = CommandBufferLevel.Primary
        };
        _vk.AllocateCommandBuffers(_device, allocInfo, out var commandBuffer);
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.None
        };
        _vk.BeginCommandBuffer(commandBuffer, beginInfo);
        return commandBuffer; }

    public void EndSingleTimeCommands(CommandBuffer cmd)
    {
        _vk.EndCommandBuffer(cmd);
        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmd
        };
        SubmitMainQueue(submitInfo, default);
        WaitForQueue();
        _vk.FreeCommandBuffers(_device, _commandPool, 1, cmd);
    }

    private uint FindMemoryTypeIndex(uint filter, MemoryPropertyFlags flags)
    {
        _vk.GetPhysicalDeviceMemoryProperties(_physicalDevice, out var props);
        for (var i = 0; i < props.MemoryTypeCount; i++)
        {
            if ((filter & (uint)(1 << i)) != 0u && (props.MemoryTypes[i].PropertyFlags & flags) == flags)
                return (uint)i;
        }
        throw new Exception("Unable to find suitable memory type");
    }

    private DeviceMemory Allocate(MemoryRequirements memoryRequirements, MemoryPropertyFlags propertyFlags)
    {
        var size = memoryRequirements.Size;
        var typeIndex = FindMemoryTypeIndex(memoryRequirements.MemoryTypeBits, propertyFlags);
        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = size,
            MemoryTypeIndex = typeIndex
        };
        _vk.AllocateMemory(_device, allocInfo, null, out var deviceMemory);
        return deviceMemory;
    }
    
    private DeviceMemory AllocateImage(Image image, MemoryPropertyFlags propertyFlags)
    {
        _vk.GetImageMemoryRequirements(_device, image, out var memReq);
        return Allocate(memReq, propertyFlags);
    }

    private DeviceMemory AllocateBuffer(Buffer buffer, MemoryPropertyFlags propertyFlags)
    {
        _vk.GetBufferMemoryRequirements(_device, buffer, out var memReq);
        return Allocate(memReq, propertyFlags);
    }
}