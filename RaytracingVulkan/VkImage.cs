using Silk.NET.Vulkan;
using SkiaSharp;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace RaytracingVulkan;

public sealed unsafe class VkImage : Allocation
{
    public readonly uint Width;
    public readonly uint Height;
    public readonly Format Format;
    public Extent3D ImageExtent => new(Width, Height, 1);

    public readonly Image Image;
    private readonly ImageView _imageView;
    private ImageLayout _currentLayout;

    public VkImage(VkContext context, uint width, uint height, Format format, ImageUsageFlags imageUsageFlags) : base(context)
    {
        Width = width;
        Height = height;
        Format = format;

        //create image
        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D(Width, Height, 1),
            Format = Format,
            Samples = SampleCountFlags.Count1Bit,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined,
            Tiling = ImageTiling.Linear,
            Usage = imageUsageFlags,
            MipLevels = 1,
            ArrayLayers = 1
        };
        Vk.CreateImage(Device, imageInfo, null, out Image);
        _currentLayout = ImageLayout.Undefined;

        //create and bind memory
        Vk.GetImageMemoryRequirements(Device, Image, out var memReq);
        Memory = VkContext.AllocateMemory(memReq, MemoryPropertyFlags.DeviceLocalBit);
        Vk.BindImageMemory(Device, Image, Memory, 0);

        //create view
        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = Image,
            ViewType = ImageViewType.Type2D,
            Format = Format,
            SubresourceRange =
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                BaseArrayLayer = 0,
                LevelCount = 1,
                LayerCount = 1
            }
        };
        Vk.CreateImageView(Device, viewInfo, null, out _imageView);
    }

    public DescriptorImageInfo GetImageInfo() => new()
    {
        ImageLayout = ImageLayout.General,
        ImageView = _imageView
    };

    public void TransitionLayoutImmediate(ImageLayout newLayout)
    {
        var cmd = VkContext.BeginSingleTimeCommands();
        TransitionLayout(cmd, newLayout);
        VkContext.EndSingleTimeCommands(cmd);
    }

    public void TransitionLayout(CommandBuffer cmd, ImageLayout newLayout)
    {
        var range = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1);
        var barrierInfo = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = _currentLayout,
            NewLayout = newLayout,
            Image = Image,
            SubresourceRange = range,
        };

        //determining AccessMasks and PipelineStageFlags from layouts
        PipelineStageFlags srcStage;
        PipelineStageFlags dstStage;

        if (_currentLayout == ImageLayout.Undefined)
        {
            barrierInfo.SrcAccessMask = 0;
            srcStage = PipelineStageFlags.TopOfPipeBit;
        }
        else if (_currentLayout == ImageLayout.General)
        {
            barrierInfo.SrcAccessMask = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.ComputeShaderBit;
        }
        else if (_currentLayout == ImageLayout.TransferSrcOptimal)
        {
            barrierInfo.SrcAccessMask = AccessFlags.TransferReadBit;
            srcStage = PipelineStageFlags.TransferBit;
        }
        else if (_currentLayout == ImageLayout.TransferDstOptimal)
        {
            barrierInfo.SrcAccessMask = AccessFlags.TransferWriteBit;
            srcStage = PipelineStageFlags.TransferBit;
        }
        else if (_currentLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrierInfo.SrcAccessMask = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.FragmentShaderBit;
        }
        else throw new Exception($"Currently unsupported Layout Transition from {_currentLayout} to {newLayout}");

        if (newLayout == ImageLayout.TransferSrcOptimal)
        {
            barrierInfo.DstAccessMask = AccessFlags.TransferReadBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else if (newLayout == ImageLayout.TransferDstOptimal)
        {
            barrierInfo.DstAccessMask = AccessFlags.TransferWriteBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else if (newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrierInfo.DstAccessMask = AccessFlags.ShaderReadBit;
            dstStage = PipelineStageFlags.FragmentShaderBit;
        }
        else if (newLayout == ImageLayout.General)
        {
            barrierInfo.DstAccessMask = AccessFlags.ShaderReadBit;
            dstStage = PipelineStageFlags.ComputeShaderBit;
        }
        else
            throw new Exception($"Currently unsupported Layout Transition from {_currentLayout} to {newLayout}");

        Vk.CmdPipelineBarrier(cmd, srcStage, dstStage, 0, 0, null, 0, null, 1, barrierInfo);
        _currentLayout = newLayout;
    }

    public void CopyToBufferImmediate(Buffer buffer)
    { 
        //check if usable as transfer source -> transition if not
        var tmpLayout = _currentLayout;
        if(_currentLayout != ImageLayout.TransferSrcOptimal)
            TransitionLayoutImmediate(ImageLayout.TransferSrcOptimal);

        var cmd = VkContext.BeginSingleTimeCommands();
        var layers = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1);
        var copyRegion = new BufferImageCopy(0, 0, 0, layers, default, ImageExtent);
        Vk.CmdCopyImageToBuffer(cmd, Image, _currentLayout, buffer, 1, copyRegion);
        VkContext.EndSingleTimeCommands(cmd);

        //transfer back to original layout if changed
        if(_currentLayout != tmpLayout)
            TransitionLayoutImmediate(ImageLayout.General);
    }
    public void TransitionAndCopyToBuffer(CommandBuffer cmd, Buffer buffer)
    { 
        //check if usable as transfer source -> transition if not
        var tmpLayout = _currentLayout;
        if(_currentLayout != ImageLayout.TransferSrcOptimal)
            TransitionLayout(cmd, ImageLayout.TransferSrcOptimal);
        CopyToBuffer(cmd, buffer);
        //transfer back to original layout if changed
        if(_currentLayout != tmpLayout)
            TransitionLayout(cmd, ImageLayout.General);
    }

    public void CopyToBuffer(CommandBuffer cmd, Buffer buffer)
    {
        var layers = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1);
        var copyRegion = new BufferImageCopy(0, 0, 0, layers, default, ImageExtent);
        Vk.CmdCopyImageToBuffer(cmd, Image, _currentLayout, buffer, 1, copyRegion);
    }

    public void SetData(void* source)
    {
        var size = Width * Height * 4; 
        using var buffer = new VkBuffer(VkContext, size, BufferUsageFlags.TransferSrcBit,
                                        MemoryPropertyFlags.HostVisibleBit);

        //copy data using a staging buffer
        void* mappedData = default;
        buffer.MapMemory(ref mappedData);
        System.Buffer.MemoryCopy(source, mappedData, size, size);
        buffer.UnmapMemory();
        var tmpLayout = _currentLayout;
        if(_currentLayout != ImageLayout.TransferDstOptimal)
            TransitionLayoutImmediate(ImageLayout.TransferDstOptimal);
        buffer.CopyToImage(this);

        if(_currentLayout != tmpLayout)
            TransitionLayoutImmediate(tmpLayout);
    }

    public void CopyTo(void* destination)
    {
        //this is valid for R8G8B8A8 formats and permutations only
        var size = Width * Height * 4;
        using var buffer = new VkBuffer(VkContext, size, BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit | MemoryPropertyFlags.HostCachedBit);
        CopyToBufferImmediate(buffer.Buffer);

        //copy data using a staging buffer
        void* mappedData = default;
        buffer.MapMemory(ref mappedData);
        System.Buffer.MemoryCopy(mappedData, destination, size, size);
        buffer.UnmapMemory();
    }

    public void Save(string destination)
    {
        var imageData = new uint[Width * Height];
        fixed (void* pImageData = imageData)
        {
            CopyTo(pImageData);

            //color type is hardcoded! convert if needed
            var info = new SKImageInfo((int) Width, (int) Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            var bmp = new SKBitmap();

            bmp.InstallPixels(info, (nint) pImageData, info.RowBytes);
            using var fs = File.Create(destination);
            bmp.Encode(fs, SKEncodedImageFormat.Png, 100);
        }
    }

    public override void Dispose()
    {
        Vk.DestroyImageView(Device, _imageView, null);
        Vk.FreeMemory(Device, Memory, null);
        Vk.DestroyImage(Device, Image, null);
    }
}