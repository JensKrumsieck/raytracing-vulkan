using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Silk.NET.Vulkan;

namespace RaytracingVulkan.UI.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    [ObservableProperty] private WriteableBitmap _image = null!;

    private readonly VkContext _context = (Application.Current as App)!.VkContext;
    private Vk Vk => _context.Vk;
    private Device Device => _context.Device;

    private readonly DescriptorPool _descriptorPool;
    private readonly DescriptorSet _descriptorSet;
    private readonly DescriptorSetLayout _setLayout;
    private readonly PipelineLayout _pipelineLayout;
    private readonly Pipeline _pipeline;

    private readonly CommandBuffer _cmd;

    private readonly VkImage _vkImage;

    public MainViewModel()
    {
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
        
        //image creation
        _vkImage = new VkImage(_context, 500, 500, Format.R8G8B8A8Unorm,ImageUsageFlags.StorageBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit);
        _vkImage.TransitionLayout(ImageLayout.General);
        _context.UpdateDescriptorSetImage(ref _descriptorSet, _vkImage.GetImageInfo(), DescriptorType.StorageImage, 0);

        _image = new WriteableBitmap(new PixelSize(500, 500), new Vector(96, 96), PixelFormat.Rgba8888);
        
        //we don't need it anymore
        _context.DestroyShaderModule(shaderModule);
        _cmd = _context.AllocateCommandBuffer();
    }
    public void Dispose()
    {
        _context.DestroyDescriptorPool(_descriptorPool);
        _context.DestroyDescriptorSetLayout(_setLayout);
        _context.DestroyPipelineLayout(_pipelineLayout);
        _context.DestroyPipeline(_pipeline);
        _image.Dispose();
        GC.SuppressFinalize(this);
    }
    public void Render()
    {
        Console.WriteLine("Run compute shader");
        //execute compute shader
        _context.BeginCommandBuffer(_cmd);
        _context.BindComputePipeline(_cmd, _pipeline);
        _context.BindComputeDescriptorSet(_cmd, _descriptorSet, _pipelineLayout);
        _context.Dispatch(_cmd, 32, 32, 1);
        _context.EndCommandBuffer(_cmd);
        _context.WaitForQueue();
        unsafe
        {
            using var buffer = _image.Lock();
            _vkImage.CopyTo((void*) buffer.Address);
        }
    }
}
