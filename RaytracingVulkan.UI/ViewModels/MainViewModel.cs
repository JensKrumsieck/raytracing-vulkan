using System;
using System.Diagnostics;
using System.Numerics;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Silk.NET.Vulkan;
using Vector = Avalonia.Vector;

namespace RaytracingVulkan.UI.ViewModels;

public unsafe partial class MainViewModel : ObservableObject, IDisposable
{
    //observables
    [ObservableProperty] private WriteableBitmap _image;
    [ObservableProperty] private CameraViewModel _cameraViewModel;
    [ObservableProperty] private float _frameTime;
    [ObservableProperty] private float _ioTime;
    
    //camera and input
    private Camera ActiveCamera => _cameraViewModel.ActiveCamera;
    private readonly InputHandler _input;
    
    //stopwatches
    private readonly Stopwatch _frameTimeStopWatch = new();
    private readonly Stopwatch _ioStopWatch = new();
    
    //vulkan
    private readonly VkContext _context = (Application.Current as App)!.VkContext;
    private readonly CommandBuffer _cmd;

    //pipeline
    private readonly DescriptorPool _descriptorPool;
    private DescriptorSet _descriptorSet;
    private readonly DescriptorSetLayout _setLayout;
    private readonly PipelineLayout _pipelineLayout;
    private readonly Pipeline _pipeline;

    //image and buffers
    private VkImage? _vkImage;
    private VkBuffer? _vkBuffer;
    private readonly VkBuffer _sceneParameterBuffer;

    //pointers
    private void* _mappedData;
    private readonly void* _mappedSceneParameterData;

    
    public MainViewModel(InputHandler input)
    {
        _input = input;
        _cameraViewModel = new CameraViewModel(new Camera(90, 0.1f, 1000f));
        
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

        _setLayout = _context.CreateDescriptorSetLayout(new[] {binding0, binding1});
        _descriptorSet = _context.AllocateDescriptorSet(_descriptorPool, _setLayout);

        var shaderModule = _context.LoadShaderModule("./assets/shaders/raytracing.comp.spv");
        _pipelineLayout = _context.CreatePipelineLayout(_setLayout);
        _pipeline = _context.CreateComputePipeline(_pipelineLayout, shaderModule);
        _sceneParameterBuffer = new VkBuffer(_context, (uint) sizeof(SceneParameters), BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        _sceneParameterBuffer.MapMemory(ref _mappedSceneParameterData);
        
        _context.UpdateDescriptorSetBuffer(ref _descriptorSet, _sceneParameterBuffer.GetBufferInfo(), DescriptorType.UniformBuffer, 1);
        
        //needed for initial binding
        _image = new WriteableBitmap(new PixelSize(1, 1), new Vector(96, 96), PixelFormat.Bgra8888);
        
        //we don't need it anymore
        _context.DestroyShaderModule(shaderModule);
        _cmd = _context.AllocateCommandBuffer();
    }
    
    public void Dispose()
    {
        _context.WaitIdle();
        _sceneParameterBuffer.Dispose();
        _image.Dispose();
        _vkBuffer?.Dispose();
        _vkImage?.Dispose();
        
        _context.DestroyDescriptorPool(_descriptorPool);
        _context.DestroyDescriptorSetLayout(_setLayout);
        _context.DestroyPipelineLayout(_pipelineLayout);
        _context.DestroyPipeline(_pipeline);
        GC.SuppressFinalize(this);
    }
    
    public void Render()
    {
        if(_vkImage is null) return;
        _frameTimeStopWatch.Start();
        
        HandleInput(FrameTime / 1000f);
        UpdateSceneParameters();
        RenderImage();
        
        _ioStopWatch.Start();
        CopyImageToHost();
        _ioStopWatch.Stop();
        IoTime = (float) _ioStopWatch.Elapsed.TotalMilliseconds;
        _ioStopWatch.Reset();
        
        _frameTimeStopWatch.Stop();
        FrameTime = (float) _frameTimeStopWatch.Elapsed.TotalMilliseconds;
        _frameTimeStopWatch.Reset();
    }

    public void Resize(uint x, uint y)
    {
        _vkImage?.Dispose();
        _vkImage = new VkImage(_context, x, y, Format.B8G8R8A8Unorm,ImageUsageFlags.StorageBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit);
        _vkImage.TransitionLayoutImmediate(ImageLayout.General);
        _context.UpdateDescriptorSetImage(ref _descriptorSet, _vkImage.GetImageInfo(), DescriptorType.StorageImage, 0);

        //save old image
        var tmp = _image;
        _image = new WriteableBitmap(new PixelSize((int) x, (int) y), new Vector(96, 96), PixelFormat.Bgra8888);
        OnPropertyChanged(nameof(Image));
        //dispose old
        tmp?.Dispose();
        
        _vkBuffer?.Dispose();
        _vkBuffer = new VkBuffer(_context, _vkImage.Width * _vkImage.Height * 4, BufferUsageFlags.TransferDstBit,
                                 MemoryPropertyFlags.HostCachedBit | MemoryPropertyFlags.HostCoherentBit |
                                 MemoryPropertyFlags.HostVisibleBit);
        _vkBuffer.MapMemory(ref _mappedData);
        
        ActiveCamera.Resize(x, y);
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

    private void CopyImageToHost()
    {
        using var buffer = _image.Lock();
        var size = _vkImage!.Width * _vkImage.Height * 4;
        System.Buffer.MemoryCopy(_mappedData, (void*) buffer.Address, size, size);
    }

    private void UpdateSceneParameters()
    {
        //update ubo
        var parameters = new SceneParameters
        {
            CameraProjection = ActiveCamera.Projection,
            InverseCameraProjection = ActiveCamera.InverseProjection,
            CameraView = ActiveCamera.View,
            InverseCameraView = ActiveCamera.InverseView
        };
        System.Buffer.MemoryCopy(&parameters, _mappedSceneParameterData, sizeof(SceneParameters), sizeof(SceneParameters));
    }

    private void HandleInput(float deltaTime)
    {
        //camera move
        var speed = 5f * deltaTime;
        var right = Vector3.Cross(ActiveCamera.Forward, Vector3.UnitY);
        var moved = false;
        
        var moveVector = Vector3.Zero;
        if (_input.PressedKeys.Contains(Key.W))
        {
            moveVector += ActiveCamera.Forward;
            moved = true;
        }
        if (_input.PressedKeys.Contains(Key.S))
        { 
            moveVector -= ActiveCamera.Forward;
            moved = true;
            
        }
        if (_input.PressedKeys.Contains(Key.D))
        {
            moveVector += right;
            moved = true;
        }
        if (_input.PressedKeys.Contains(Key.A))
        {
            moveVector -= right;
            moved = true;
        }
        if (_input.PressedKeys.Contains(Key.Q))
        {
            moveVector += Vector3.UnitY;
            moved = true;
        }
        if (_input.PressedKeys.Contains(Key.E))
        {
            moveVector -= Vector3.UnitY;
            moved = true;
        }

        if (!moved) return;
        if(moveVector.Length() == 0) return;
        
        moveVector = Vector3.Normalize(moveVector) * speed;
        _cameraViewModel.Position += moveVector;
        ActiveCamera.RecalculateView();
    }
}
