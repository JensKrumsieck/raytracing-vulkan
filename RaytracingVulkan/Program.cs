using RaytracingVulkan;
using Silk.NET.Vulkan;

using var ctx = new VkContext();
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
var image = new VkImage(ctx, 500, 500, Format.R8G8B8A8Unorm,ImageUsageFlags.StorageBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.TransferSrcBit);
image.TransitionLayout(ImageLayout.General);
var imageInfo = image.GetImageInfo();
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

//save image
image.Save("./render.png");

//destroy image objects
image.Dispose();