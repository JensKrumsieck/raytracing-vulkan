using RaytracingVulkan;
using Silk.NET.Vulkan;
using SkiaSharp;
using Buffer = System.Buffer;

unsafe
{
    using var ctx = new VkContext();
    
    //pipeline creation
    var poolSizes = new DescriptorPoolSize[] {new() {Type = DescriptorType.StorageImage, DescriptorCount = 1000}};
    var descriptorPool = ctx.CreateDescriptorPool(poolSizes);
    var binding = new DescriptorSetLayoutBinding
    {
        Binding = 0,
        DescriptorCount = 1,
        DescriptorType = DescriptorType.StorageImage,
        StageFlags = ShaderStageFlags.ComputeBit
    };
    var setLayout = ctx.CreateDescriptorSetLayout(new[] {binding});
    var descriptorSet = ctx.AllocateDescriptorSet(descriptorPool, setLayout);
    
    var shaderModule = ctx.LoadShaderModule("./assets/shaders/raytracing.comp.spv");
    var pipelineLayout = ctx.CreatePipelineLayout(setLayout);
    var pipeline = ctx.CreateComputePipeline(pipelineLayout, shaderModule);

    //image creation
    var image = ctx.CreateImage(500, 500, Format.R8G8B8A8Unorm, MemoryPropertyFlags.DeviceLocalBit, ImageUsageFlags.StorageBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit);
    var imageView = ctx.CreateImageView(image.Image, Format.R8G8B8A8Unorm);
    ctx.TransitionImageLayout(image.Image, ImageLayout.Undefined, ImageLayout.General);
    var imageInfo = new DescriptorImageInfo
    {
        ImageLayout = ImageLayout.General,
        ImageView = imageView
    };
    ctx.UpdateDescriptorSetImage(ref descriptorSet, imageInfo, DescriptorType.StorageImage, 0);

    //execute compute shader
    var cmd = ctx.BeginSingleTimeCommands();
    ctx.BindComputePipeline(cmd, pipeline);
    ctx.BindComputeDescriptorSet(cmd, descriptorSet, pipelineLayout);
    ctx.Dispatch(cmd, 500/8, 500/8, 1);
    ctx.EndSingleTimeCommands(cmd);
    
    //destroy pipeline objects
    ctx.DestroyDescriptorPool(descriptorPool);
    ctx.DestroyDescriptorSetLayout(setLayout);
    ctx.DestroyShaderModule(shaderModule);
    ctx.DestroyPipelineLayout(pipelineLayout);
    ctx.DestroyPipeline(pipeline);
    
    void* mappedData = default;
    var buffer = ctx.CreateBuffer(500*500*4, BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.HostVisibleBit);
    ctx.TransitionImageLayout(image.Image, ImageLayout.General, ImageLayout.TransferSrcOptimal);
    ctx.CopyImageToBuffer(image.Image, buffer.Buffer, new Extent3D(500,500,1), ImageLayout.TransferSrcOptimal);
    ctx.TransitionImageLayout(image.Image, ImageLayout.TransferSrcOptimal, ImageLayout.General);
    
    //copy data using a staging buffer
    var newImageData = new uint[500 * 500];
    ctx.MapMemory(buffer.Memory, ref mappedData);
    fixed (void* pNewImageData = newImageData)
        Buffer.MemoryCopy(mappedData, pNewImageData,
            newImageData.Length * sizeof(uint),
            newImageData.Length * sizeof(uint));
    ctx.UnmapMemory(buffer.Memory);
    ctx.DestroyBuffer(buffer);

    //save image
    var info = new SKImageInfo(500, 500, SKColorType.Rgba8888, SKAlphaType.Premul);
    var bmp = new SKBitmap();
    fixed (uint* pImageData = newImageData)
        bmp.InstallPixels(info, (nint) pImageData, info.RowBytes);
    using var fs = File.Create("./render.png");
    bmp.Encode(fs, SKEncodedImageFormat.Png, 100);
    
    //destroy image objects
    ctx.DestroyImageView(imageView);
    ctx.DestroyImage(image);
}