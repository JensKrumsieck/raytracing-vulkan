using Silk.NET.Vulkan;

namespace RaytracingVulkan;

public sealed unsafe class Renderer : IDisposable
{
    //vulkan
    private readonly VkContext _context;
    private readonly CommandBuffer _cmd;

    //pipeline
    private readonly DescriptorPool _descriptorPool;
    private DescriptorSet _descriptorSet;
    private readonly DescriptorSetLayout _setLayout;
    private readonly PipelineLayout _pipelineLayout;
    private readonly Pipeline _pipeline;

    //image and buffers
    private VkImage? _vkImage;
    private VkImage? _accumulationTexture;
    private VkBuffer? _vkBuffer;
    private readonly VkBuffer _sceneParameterBuffer;
    
    //pointers
    private void* _mappedData;
    private readonly void* _mappedSceneParameterData;

    //scene data
    private uint _viewportWidth;
    private uint _viewportHeight;
    private uint _frameIndex = 1;

    public bool IsReady;

    public Renderer(VkContext context)
    {
        _context = context;
        
        //pipeline creation
        var poolSizes = new DescriptorPoolSize[]
        {
            new() {Type = DescriptorType.StorageImage, DescriptorCount = 1000},
            new() {Type = DescriptorType.UniformBuffer, DescriptorCount = 1000}
        };
        
        _descriptorPool = _context.CreateDescriptorPool(poolSizes);
        var binding0 = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageImage,
            StageFlags = ShaderStageFlags.ComputeBit
        };
        var binding1 = new DescriptorSetLayoutBinding
        {
            Binding = 1,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
            StageFlags = ShaderStageFlags.ComputeBit
        };
        var binding2 = binding0 with {Binding = 2};
        
        _setLayout = _context.CreateDescriptorSetLayout(new[] {binding0, binding1, binding2});
        _descriptorSet = _context.AllocateDescriptorSet(_descriptorPool, _setLayout);

        var shaderModule = _context.LoadShaderModule("./assets/shaders/raytracing.comp.spv");
        _pipelineLayout = _context.CreatePipelineLayout(_setLayout);
        _pipeline = _context.CreateComputePipeline(_pipelineLayout, shaderModule);
        _sceneParameterBuffer = new VkBuffer(_context, (uint) sizeof(SceneParameters), BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        _sceneParameterBuffer.MapMemory(ref _mappedSceneParameterData);
        
        _context.UpdateDescriptorSetBuffer(ref _descriptorSet, _sceneParameterBuffer.GetBufferInfo(), DescriptorType.UniformBuffer, 1);
        
        //we don't need it anymore
        _context.DestroyShaderModule(shaderModule);
        _cmd = _context.AllocateCommandBuffer();
    }

    public void Render(Camera camera)
    {
        UpdateSceneParameters(camera);
        RenderImage();
        _frameIndex++;
    }

    public void Resize(uint x, uint y)
    {
        _viewportWidth = x;
        _viewportHeight = y;
    }

    private void RenderImage()
    {
        //execute compute shader
        _context.BeginCommandBuffer(_cmd);
        _vkImage!.TransitionLayout(_cmd, ImageLayout.General);
        _context.BindComputePipeline(_cmd, _pipeline);
        _context.BindComputeDescriptorSet(_cmd, _descriptorSet, _pipelineLayout);
        _context.Dispatch(_cmd, _vkImage.Width/32, _vkImage.Height/32, 1);
        _vkImage.TransitionLayout(_cmd, ImageLayout.TransferSrcOptimal);
        _vkImage.CopyToBuffer(_cmd, _vkBuffer!.Buffer);
        _context.EndCommandBuffer(_cmd);
        _context.WaitForQueue();
    }
    private void UpdateSceneParameters(Camera camera)
    {
        //update ubo
        var parameters = new SceneParameters
        {
            CameraProjection = camera.Projection,
            InverseCameraProjection = camera.InverseProjection,
            CameraView = camera.View,
            InverseCameraView = camera.InverseView,
            FrameIndex = _frameIndex
        };
        System.Buffer.MemoryCopy(&parameters, _mappedSceneParameterData, sizeof(SceneParameters), sizeof(SceneParameters));
    }

    public void CopyDataTo(IntPtr address)
    {
        var size = _viewportWidth * _viewportHeight * 4;
        System.Buffer.MemoryCopy(_mappedData, address.ToPointer(), size, size);
    }

    public void Reset()
    {
        IsReady = false;
        _vkImage?.Dispose();
        _vkImage = new VkImage(_context, _viewportWidth, _viewportHeight, Format.B8G8R8A8Unorm,ImageUsageFlags.StorageBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit);
        _vkImage.TransitionLayoutImmediate(ImageLayout.General);
        _context.UpdateDescriptorSetImage(ref _descriptorSet, _vkImage.GetImageInfo(), DescriptorType.StorageImage, 0);
        
        _accumulationTexture?.Dispose();
        _accumulationTexture = new VkImage(_context, _viewportWidth, _viewportHeight, Format.R32G32B32A32Sfloat,ImageUsageFlags.StorageBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit);
        _accumulationTexture.TransitionLayoutImmediate(ImageLayout.General);
        _context.UpdateDescriptorSetImage(ref _descriptorSet, _accumulationTexture.GetImageInfo(), DescriptorType.StorageImage, 2);
        _frameIndex = 1;
        
        _vkBuffer?.Dispose();
        _vkBuffer = new VkBuffer(_context, _vkImage.Width * _vkImage.Height * 4, BufferUsageFlags.TransferDstBit,
                                 MemoryPropertyFlags.HostCachedBit | MemoryPropertyFlags.HostCoherentBit |
                                 MemoryPropertyFlags.HostVisibleBit);
        _vkBuffer.MapMemory(ref _mappedData);
        IsReady = true;
    }
    
    public void Dispose()
    {
        IsReady = false;
        _context.WaitIdle();
        _sceneParameterBuffer.Dispose();
        _vkBuffer?.Dispose();
        _vkImage?.Dispose();
        
        _context.DestroyDescriptorPool(_descriptorPool);
        _context.DestroyDescriptorSetLayout(_setLayout);
        _context.DestroyPipelineLayout(_pipelineLayout);
        _context.DestroyPipeline(_pipeline);
    }
}