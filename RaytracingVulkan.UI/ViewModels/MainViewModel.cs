using System;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Silk.NET.Vulkan;

namespace RaytracingVulkan.UI.ViewModels;

public unsafe partial class MainViewModel : ObservableObject, IDisposable
{
    [ObservableProperty] private WriteableBitmap _image;

    private readonly VkContext _context = (Application.Current as App)!.VkContext;

    private readonly DescriptorPool _descriptorPool;
    private readonly DescriptorSet _descriptorSet;
    private readonly DescriptorSetLayout _setLayout;
    private readonly PipelineLayout _pipelineLayout;
    private readonly Pipeline _pipeline;

    private readonly CommandBuffer _cmd;

    private readonly VkImage _vkImage;
    private readonly VkBuffer _vkBuffer;

    private readonly void* _mappedData;

    private InputHandler _input;
    
    public MainViewModel(InputHandler input)
    {
        _input = input;
        
        //pipeline creation
        var poolSizes = new DescriptorPoolSize[] {new() {Type = DescriptorType.StorageImage, DescriptorCount = 1000}};
        _descriptorPool = _context.CreateDescriptorPool(poolSizes);
        var binding = new DescriptorSetLayoutBinding
        {
            Binding = 0,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageImage,
            StageFlags = ShaderStageFlags.ComputeBit
        };
        _setLayout = _context.CreateDescriptorSetLayout(new[] {binding});
        _descriptorSet = _context.AllocateDescriptorSet(_descriptorPool, _setLayout);

        var shaderModule = _context.LoadShaderModule("./assets/shaders/raytracing.comp.spv");
        _pipelineLayout = _context.CreatePipelineLayout(_setLayout);
        _pipeline = _context.CreateComputePipeline(_pipelineLayout, shaderModule);
        var sz = 2000u;
        //image creation
        _vkImage = new VkImage(_context, sz, sz, Format.B8G8R8A8Unorm,ImageUsageFlags.StorageBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit);
        _vkImage.TransitionLayoutImmediate(ImageLayout.General);
        _context.UpdateDescriptorSetImage(ref _descriptorSet, _vkImage.GetImageInfo(), DescriptorType.StorageImage, 0);

        _image = new WriteableBitmap(new PixelSize((int) sz, (int) sz), new Vector(96, 96), PixelFormat.Bgra8888);
        
        //we don't need it anymore
        _context.DestroyShaderModule(shaderModule);
        _cmd = _context.AllocateCommandBuffer();
        _vkBuffer = new VkBuffer(_context, _vkImage.Width * _vkImage.Height * 4, BufferUsageFlags.TransferDstBit,
            MemoryPropertyFlags.HostCachedBit | MemoryPropertyFlags.HostCoherentBit |
            MemoryPropertyFlags.HostVisibleBit);
        _vkBuffer.MapMemory(ref _mappedData);
    }
    
    public void Dispose()
    {
        _vkBuffer.UnmapMemory();
        _image.Dispose();
        _vkBuffer.Dispose();
        _vkImage.Dispose();
        
        _context.DestroyDescriptorPool(_descriptorPool);
        _context.DestroyDescriptorSetLayout(_setLayout);
        _context.DestroyPipelineLayout(_pipelineLayout);
        _context.DestroyPipeline(_pipeline);
        GC.SuppressFinalize(this);
    }
    
    public void Render()
    {
        HandleInput();
        RenderImage();
        CopyImageToHost();
    }
    private void RenderImage()
    {
        //execute compute shader
        _context.BeginCommandBuffer(_cmd);
        _vkImage.TransitionLayout(_cmd, ImageLayout.General);
        _context.BindComputePipeline(_cmd, _pipeline);
        _context.BindComputeDescriptorSet(_cmd, _descriptorSet, _pipelineLayout);
        _context.Dispatch(_cmd, _vkImage.Width/32, _vkImage.Height/32, 1);
        _vkImage.TransitionLayout(_cmd, ImageLayout.TransferSrcOptimal);
        _vkImage.CopyToBuffer(_cmd, _vkBuffer.Buffer);
        _context.EndCommandBuffer(_cmd);
        _context.WaitForQueue();
    }

    private void CopyImageToHost()
    {
        using var buffer = _image.Lock();
        var size = _vkImage.Width * _vkImage.Height * 4;
        System.Buffer.MemoryCopy(_mappedData, (void*) buffer.Address, size, size);
    }
    
    private void HandleInput()
    {
        //camera move
    }
}
