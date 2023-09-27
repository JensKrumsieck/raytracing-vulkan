using System.Numerics;
using RaytracingVulkan.Memory;
using RaytracingVulkan.Primitives;
using Silk.NET.Vulkan;

namespace RaytracingVulkan;

public sealed unsafe class Renderer : IDisposable
{
    //vulkan
    private readonly VkContext _context;
    private readonly CommandBuffer _computeCmd;
    private readonly Fence _computeFence;

    private readonly CommandBuffer _copyCmd;
    private readonly Fence _copyFence;
    
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
    private readonly VkBuffer _triangleBuffer;
    private readonly VkBuffer _sphereBuffer;
    
    //pointers
    private void* _mappedData;
    private readonly void* _mappedSceneParameterData;

    //scene data
    private uint _viewportWidth;
    private uint _viewportHeight;
    private uint _frameIndex = 1;
    
    //mesh data
    private Triangle[] _triangles;
    private Sphere[] _spheres;

    public bool IsReady;

    public Renderer(VkContext context)
    {
        _context = context;
        
        _triangles = MeshImporter.FromFile("./assets/models/suzanne.fbx")[0].ToTriangles();
        _spheres = new Sphere[]
        {
            new() {Position = new Vector3(-2.0f, -0.5f, -1.0f), Radius = 0.5f},
            new() {Position = new Vector3(2.0f, -0.5f, -1.0f), Radius = 0.5f},
            new() {Position = new Vector3(0.0f, -101f, -1.0f), Radius = 100.0f}
        };
        
        //pipeline creation
        var poolSizes = new DescriptorPoolSize[]
        {
            new() {Type = DescriptorType.StorageImage, DescriptorCount = 1000},
            new() {Type = DescriptorType.UniformBuffer, DescriptorCount = 1000},
            new() {Type = DescriptorType.StorageBuffer, DescriptorCount = 1000}
        };
        
        _descriptorPool = _context.CreateDescriptorPool(poolSizes);
        var binding0 = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageImage,
            StageFlags = ShaderStageFlags.ComputeBit
        };
        var binding1 = binding0 with {Binding = 1, DescriptorType = DescriptorType.UniformBuffer};
        var binding2 = binding0 with {Binding = 2};
        var binding3 = binding0 with {Binding = 3, DescriptorType = DescriptorType.StorageBuffer};
        var binding4 = binding0 with {Binding = 4, DescriptorType = DescriptorType.StorageBuffer};

        _setLayout = _context.CreateDescriptorSetLayout(new[] {binding0, binding1, binding2, binding3, binding4});
        _descriptorSet = _context.AllocateDescriptorSet(_descriptorPool, _setLayout);

        var shaderModule = _context.LoadShaderModule("./assets/shaders/raytracing.comp.spv");
        _pipelineLayout = _context.CreatePipelineLayout(_setLayout);
        _pipeline = _context.CreateComputePipeline(_pipelineLayout, shaderModule);
        _sceneParameterBuffer = new VkBuffer(_context, (uint) sizeof(SceneParameters), BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        _sceneParameterBuffer.MapMemory(ref _mappedSceneParameterData);
        _context.UpdateDescriptorSetBuffer(ref _descriptorSet, _sceneParameterBuffer.GetBufferInfo(), DescriptorType.UniformBuffer, 1);

        _triangleBuffer = new VkBuffer(_context, (uint) (sizeof(Triangle) * _triangles.Length), BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.DeviceLocalBit);
        
        //use staging buffer
        var stagingBuffer = new VkBuffer(_context, (uint) (sizeof(Triangle) * _triangles.Length), BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        var pData = IntPtr.Zero.ToPointer();
        stagingBuffer.MapMemory(ref pData);
        fixed (void* pTriangles = _triangles)
            System.Buffer.MemoryCopy(pTriangles, pData, stagingBuffer.Size, stagingBuffer.Size);
        stagingBuffer.UnmapMemory();
        
        stagingBuffer.CopyToBuffer(_triangleBuffer);
        stagingBuffer.Dispose();
        
        _context.UpdateDescriptorSetBuffer(ref _descriptorSet, _triangleBuffer.GetBufferInfo(), DescriptorType.StorageBuffer, 3);
        
        _sphereBuffer = new VkBuffer(_context, (uint) (sizeof(Sphere) * _spheres.Length), BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.DeviceLocalBit);
        
        //use staging buffer
        stagingBuffer = new VkBuffer(_context, (uint) (sizeof(Sphere) * _spheres.Length), BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        pData = IntPtr.Zero.ToPointer();
        stagingBuffer.MapMemory(ref pData);
        fixed (void* pSpheres = _spheres)
            System.Buffer.MemoryCopy(pSpheres, pData, stagingBuffer.Size, stagingBuffer.Size);
        stagingBuffer.UnmapMemory();
        
        stagingBuffer.CopyToBuffer(_sphereBuffer);
        stagingBuffer.Dispose();
        
        _context.UpdateDescriptorSetBuffer(ref _descriptorSet, _sphereBuffer.GetBufferInfo(), DescriptorType.StorageBuffer, 4);
        
        //we don't need it anymore
        _context.DestroyShaderModule(shaderModule);
        
        _computeCmd = _context.AllocateCommandBuffer();
        _computeFence = _context.CreateFence(FenceCreateFlags.SignaledBit);
        _copyCmd = _context.AllocateCommandBuffer();
        _copyFence = _context.CreateFence();
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
        _context.WaitForFence(_computeFence);
        _context.ResetFence(_computeFence);
        _context.BeginCommandBuffer(_computeCmd);
        
        //execute compute shader
        _context.BindComputePipeline(_computeCmd, _pipeline);
        _context.BindComputeDescriptorSet(_computeCmd, _descriptorSet, _pipelineLayout);
        _context.Dispatch(_computeCmd, _vkImage!.Width / 16 + 1, _vkImage.Height / 16 + 1, 1);
        
        _context.EndCommandBuffer(_computeCmd, _computeFence);
    }

    public void PrepareImage()
    {
        if (_frameIndex == 1) return;
        _context.BeginCommandBuffer(_copyCmd);
        //copy image to buffer
        _vkImage!.TransitionLayout(_copyCmd, ImageLayout.TransferSrcOptimal);
        _vkImage.CopyToBuffer(_copyCmd, _vkBuffer!.Buffer);
        _vkImage!.TransitionLayout(_copyCmd, ImageLayout.General);
        
        _context.EndCommandBuffer(_copyCmd, _copyFence);
        
        _context.WaitForFence(_copyFence);
        _context.ResetFence(_copyFence);
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
            FrameIndex = _frameIndex,
            Time = (uint) DateTime.Now.Ticks
        };
        System.Buffer.MemoryCopy(&parameters, _mappedSceneParameterData, sizeof(SceneParameters), sizeof(SceneParameters));
    }

    public void CopyDataTo(IntPtr address)
    { 
        if (_frameIndex == 1) return;
        var size = _viewportWidth * _viewportHeight * 4;
        System.Buffer.MemoryCopy(_mappedData, address.ToPointer(), size, size);
    }

    public void Reset()
    {
        IsReady = false;
        _context.WaitForQueue();
        
        _vkImage?.Dispose();
        _vkImage = new VkImage(_context, _viewportWidth, _viewportHeight, Format.B8G8R8A8Unorm,ImageUsageFlags.StorageBit | ImageUsageFlags.TransferSrcBit);
        _vkImage.TransitionLayoutImmediate(ImageLayout.General);
        _context.UpdateDescriptorSetImage(ref _descriptorSet, _vkImage.GetImageInfo(), DescriptorType.StorageImage, 0);
        
        _accumulationTexture?.Dispose();
        _accumulationTexture = new VkImage(_context, _viewportWidth, _viewportHeight, Format.R32G32B32A32Sfloat,ImageUsageFlags.StorageBit);
        _accumulationTexture.TransitionLayoutImmediate(ImageLayout.General);
        _context.UpdateDescriptorSetImage(ref _descriptorSet, _accumulationTexture.GetImageInfo(), DescriptorType.StorageImage, 2);
        _frameIndex = 1;

        _vkBuffer?.Dispose();
        _vkBuffer = new VkBuffer(_context, _vkImage.Width * _vkImage.Height * 4, BufferUsageFlags.TransferDstBit,
                MemoryPropertyFlags.HostCachedBit | MemoryPropertyFlags.HostCoherentBit |
                MemoryPropertyFlags.HostVisibleBit);
        _vkBuffer!.MapMemory(ref _mappedData);
        IsReady = true;
    }

    public void Dispose()
    {
        IsReady = false;
        _context.WaitIdle();

        _sceneParameterBuffer.Dispose();
        _triangleBuffer.Dispose();
        _sphereBuffer.Dispose();
        _vkBuffer?.Dispose();
        _vkImage?.Dispose();

        _context.FreeCommandBuffer(_computeCmd);
        _context.DestroyFence(_computeFence);
        
        _context.FreeCommandBuffer(_copyCmd);
        _context.DestroyFence(_copyFence);
        
        _context.DestroyDescriptorPool(_descriptorPool);
        _context.DestroyDescriptorSetLayout(_setLayout);
        _context.DestroyPipelineLayout(_pipelineLayout);
        _context.DestroyPipeline(_pipeline);
    }
}