using System.Runtime.InteropServices;
using Silk.NET.Vulkan;

namespace RaytracingVulkan;

public unsafe partial class VkContext
{
    public DescriptorPool CreateDescriptorPool(DescriptorPoolSize[] poolSizes)
    {
        fixed (DescriptorPoolSize* pPoolSizes = poolSizes)
        {
            var createInfo = new DescriptorPoolCreateInfo
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint) poolSizes.Length,
                PPoolSizes = pPoolSizes,
                MaxSets = 1,
                Flags = DescriptorPoolCreateFlags.FreeDescriptorSetBit
            };
            _vk.CreateDescriptorPool(_device, createInfo, null, out var descriptorPool);
            return descriptorPool;
        }
    }
    public void DestroyDescriptorPool(DescriptorPool descriptorPool) =>
        _vk.DestroyDescriptorPool(_device, descriptorPool, null);
    public DescriptorSet AllocateDescriptorSet(DescriptorPool pool, DescriptorSetLayout setLayout)
    {
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = pool,
            DescriptorSetCount = 1,
            PSetLayouts = &setLayout
        };
        _vk.AllocateDescriptorSets(_device, allocInfo, out var descriptorSet);
        return descriptorSet;
    }
    public void UpdateDescriptorSetImage(ref DescriptorSet set, DescriptorImageInfo imageInfo, DescriptorType type,
        uint binding)
    {
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = binding,
            DstArrayElement = 0,
            DescriptorCount = 1,
            PImageInfo = &imageInfo,
            DescriptorType = type
        };
        _vk.UpdateDescriptorSets(_device, 1, &write, 0, default);
    }
    
    public void UpdateDescriptorSetBuffer(ref DescriptorSet set, DescriptorBufferInfo bufferInfo, DescriptorType type,
                                         uint binding)
    {
        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DstSet = set,
            DstBinding = binding,
            DstArrayElement = 0,
            DescriptorCount = 1,
            PBufferInfo = &bufferInfo,
            DescriptorType = type
        };
        _vk.UpdateDescriptorSets(_device, 1, &write, 0, default);
    }
    public void BindComputeDescriptorSet(CommandBuffer cmd, DescriptorSet set, PipelineLayout layout) =>
        _vk.CmdBindDescriptorSets(cmd, PipelineBindPoint.Compute, layout, 0, 1, set, 0, null);
    public void Dispatch(CommandBuffer cmd, uint groupCountX, uint groupCountY, uint groupCountZ) =>
        _vk.CmdDispatch(cmd, groupCountX, groupCountY, groupCountZ);
    public DescriptorSetLayout CreateDescriptorSetLayout(DescriptorSetLayoutBinding[] bindings)
    {
        fixed (DescriptorSetLayoutBinding* pBindings = bindings)
        {
            var layoutCreateInfo = new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)bindings.Length,
                PBindings = pBindings
            };
            _vk.CreateDescriptorSetLayout(_device, layoutCreateInfo, null, out var setLayout);
            return setLayout;
        }
    }
    public void DestroyDescriptorSetLayout(DescriptorSetLayout setLayout) =>
        _vk.DestroyDescriptorSetLayout(_device, setLayout, null);
    
    public ShaderModule LoadShaderModule(string filename)
    {
        var shaderCode = File.ReadAllBytes(filename);
        fixed (byte* pShaderCode = shaderCode)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint) shaderCode.Length,
                PCode = (uint*)pShaderCode,
            };
            _vk.CreateShaderModule(_device, createInfo, null, out var module);
            return module;
        }
    }
    public void DestroyShaderModule(ShaderModule shaderModule) => _vk.DestroyShaderModule(_device, shaderModule, null);
    public PipelineLayout CreatePipelineLayout(DescriptorSetLayout setLayout)
    {
        var layoutInfo = new PipelineLayoutCreateInfo
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &setLayout
        };
        _vk.CreatePipelineLayout(_device, layoutInfo, null, out var pipelineLayout);
        return pipelineLayout;
    }
    public void DestroyPipelineLayout(PipelineLayout layout) => _vk.DestroyPipelineLayout(_device, layout, null);

    public Pipeline CreateComputePipeline(PipelineLayout layout, ShaderModule shaderModule)
    {
        var entryPoint = "main";
        var pEntryPoint = (byte*)Marshal.StringToHGlobalAnsi(entryPoint);
        var stageInfo = new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            Stage = ShaderStageFlags.ComputeBit,
            Module = shaderModule,
            PName = pEntryPoint,
            Flags = PipelineShaderStageCreateFlags.None
        };
        var computeInfo = new ComputePipelineCreateInfo
        {
            SType = StructureType.ComputePipelineCreateInfo,
            Layout = layout,
            Stage = stageInfo
        };
        _vk.CreateComputePipelines(_device, default, 1, computeInfo, null, out var pipeline);
        return pipeline;
    }
    public void DestroyPipeline(Pipeline pipeline) => _vk.DestroyPipeline(_device, pipeline, null);
    public void BindComputePipeline(CommandBuffer cmd, Pipeline pipeline) => _vk.CmdBindPipeline(cmd, PipelineBindPoint.Compute, pipeline);
}
