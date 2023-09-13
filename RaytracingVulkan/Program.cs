using RaytracingVulkan;
using Silk.NET.Vulkan;
using SkiaSharp;
using Buffer = System.Buffer;

unsafe
{
    using var ctx = new VkContext();
    var image = ctx.CreateImage(500, 500, Format.R8G8B8A8Unorm, MemoryPropertyFlags.DeviceLocalBit, ImageUsageFlags.StorageBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit);
    var buffer = ctx.CreateBuffer(500*500*4, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit);

    var imageData = new uint[500 * 500];
    for (var i = 0; i < 500; i++)
    {
        for (var j = 0; j < 500; j++)
        {
            var x = i / 500f;
            var y = j / 500f;
            imageData[i + j * 500] = 0xff000000 | (uint) (x * 255f) << 8 | (uint) (y * 255f);
        }
    }
    
    void* mappedData = default;
    ctx.MapMemory(buffer.Memory, ref mappedData);
    fixed (void* pImageData = imageData)
        Buffer.MemoryCopy(pImageData, mappedData,
                          imageData.Length * sizeof(uint),
                          imageData.Length * sizeof(uint));
    ctx.UnmapMemory(buffer.Memory);
    ctx.TransitionImageLayout(image.Image, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);
    ctx.CopyBufferToImage(buffer.Buffer, image.Image, new Extent3D(500, 500, 1));
    ctx.TransitionImageLayout(image.Image, ImageLayout.TransferDstOptimal, ImageLayout.General);
    ctx.DestroyBuffer(buffer);
    
    var newBuffer = ctx.CreateBuffer(500*500*4, BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.HostVisibleBit);
    ctx.TransitionImageLayout(image.Image, ImageLayout.General, ImageLayout.TransferSrcOptimal);
    ctx.CopyImageToBuffer(image.Image, newBuffer.Buffer, new Extent3D(500,500,1), ImageLayout.TransferSrcOptimal);
    ctx.TransitionImageLayout(image.Image, ImageLayout.TransferSrcOptimal, ImageLayout.General);
    
    var newImageData = new uint[500 * 500];
    ctx.MapMemory(newBuffer.Memory, ref mappedData);
    fixed (void* pNewImageData = newImageData)
        Buffer.MemoryCopy(mappedData, pNewImageData,
                          newImageData.Length * sizeof(uint),
                          newImageData.Length * sizeof(uint));
    ctx.UnmapMemory(newBuffer.Memory);
    ctx.DestroyBuffer(newBuffer);

    var info = new SKImageInfo(500, 500, SKColorType.Rgba8888, SKAlphaType.Premul);
    var bmp = new SKBitmap();
    fixed (uint* pImageData = newImageData)
        bmp.InstallPixels(info, (nint) pImageData, info.RowBytes);
    using var fs = File.Create("./render.png");
    bmp.Encode(fs, SKEncodedImageFormat.Png, 100);
    
    ctx.DestroyImage(image);
}