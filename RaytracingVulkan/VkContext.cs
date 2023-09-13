using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;

namespace RaytracingVulkan;

public unsafe partial class VkContext : IDisposable
{
    private readonly Vk _vk = Vk.GetApi();
    
    private readonly Instance _instance;
    private readonly DebugUtilsMessengerEXT _debugUtilsMessenger;
    private readonly PhysicalDevice _physicalDevice;
    private readonly Device _device;
    private readonly CommandPool _commandPool;
    private readonly Queue _mainQueue;
    
    private readonly uint _mainQueueIndex;
    
    private readonly ExtDebugUtils _extDebugUtils;

    public Vk Vk => _vk;
    public Device Device => _device;
    public PhysicalDevice PhysicalDevice => _physicalDevice;
    
    public VkContext()
    {
        //enable debugging features
        var enabledInstanceExtensions = new List<string> {ExtDebugUtils.ExtensionName};
        var enabledLayers = new List<string> {"VK_LAYER_KHRONOS_validation"};
        var flags = InstanceCreateFlags.None;
        
        //check for ios
        if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            enabledInstanceExtensions.Add("VK_KHR_portability_enumeration");
            flags |= InstanceCreateFlags.EnumeratePortabilityBitKhr;
        }

        var pPEnabledLayers = (byte**) SilkMarshal.StringArrayToPtr(enabledLayers.ToArray());
        var pPEnabledInstanceExtensions = (byte**) SilkMarshal.StringArrayToPtr(enabledInstanceExtensions.ToArray());
        
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            ApiVersion = Vk.Version13 //Version 1.3
        };
        
        var debugInfo  = new DebugUtilsMessengerCreateInfoEXT
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity =DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt | DebugUtilsMessageSeverityFlagsEXT.WarningBitExt,
            MessageType = DebugUtilsMessageTypeFlagsEXT.ValidationBitExt | DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt | DebugUtilsMessageTypeFlagsEXT.DeviceAddressBindingBitExt,
            PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT) DebugCallback
        };
        
        var instanceInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            Flags = flags,
            EnabledLayerCount = (uint) enabledLayers.Count,
            PpEnabledLayerNames = pPEnabledLayers,
            EnabledExtensionCount = (uint) enabledInstanceExtensions.Count,
            PpEnabledExtensionNames = pPEnabledInstanceExtensions,
            PApplicationInfo = &appInfo,
            PNext = &debugInfo
        };
        if (_vk.CreateInstance(instanceInfo, null, out _instance) != Result.Success)
            throw new Exception("Instance could not be created");

        if(!_vk.TryGetInstanceExtension(_instance, out _extDebugUtils))
            throw new Exception($"Could not get instance extension {ExtDebugUtils.ExtensionName}");
        _extDebugUtils.CreateDebugUtilsMessenger(_instance, debugInfo, null, out _debugUtilsMessenger);

        //select discrete gpu - if none is available use first device
        var devices = _vk.GetPhysicalDevices(_instance);
        foreach (var gpu in devices)
        {
            var properties = _vk.GetPhysicalDeviceProperties(gpu);
            if (properties.DeviceType == PhysicalDeviceType.DiscreteGpu) _physicalDevice = gpu;
        }
        if (_physicalDevice.Handle == 0) _physicalDevice = devices.First();
        var deviceProps = _vk.GetPhysicalDeviceProperties(_physicalDevice);
        Console.WriteLine(SilkMarshal.PtrToString((nint)deviceProps.DeviceName));

        var enabledDeviceExtensions = new List<string>(); //empty for now!
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            enabledDeviceExtensions.Add("VK_KHR_portability_subset");
        var pPEnabledDeviceExtensions = (byte**)SilkMarshal.StringArrayToPtr(enabledDeviceExtensions.ToArray());
        
        var defaultPriority = 1.0f;
        var queueFamilyCount = 0u;
        _vk.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref queueFamilyCount, null);
        var queueFamilies = new QueueFamilyProperties[queueFamilyCount];
        fixed (QueueFamilyProperties* pQueueFamilies = queueFamilies)
            _vk.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref queueFamilyCount, pQueueFamilies);
        for (var i = 0u; i < queueFamilies.Length; i++)
        {
            if (queueFamilies[i].QueueFlags.HasFlag(QueueFlags.GraphicsBit) ||
                queueFamilies[i].QueueFlags.HasFlag(QueueFlags.ComputeBit))
            {
                _mainQueueIndex = i;
                break;
            }
        }
        
        var queueCreateInfo = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueCount = 1,
            QueueFamilyIndex = _mainQueueIndex,
            PQueuePriorities = &defaultPriority
        };
        
        var deviceCreateInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            EnabledLayerCount = (uint) enabledLayers.Count,
            PpEnabledLayerNames = pPEnabledLayers,
            EnabledExtensionCount = (uint) enabledDeviceExtensions.Count,
            PpEnabledExtensionNames = pPEnabledDeviceExtensions,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreateInfo
        };
        if (_vk.CreateDevice(_physicalDevice, deviceCreateInfo, null, out _device) != Result.Success)
            throw new Exception("Could not create device");

        _vk.GetDeviceQueue(_device, _mainQueueIndex, 0, out _mainQueue);
        
        SilkMarshal.Free((nint) pPEnabledLayers);
        SilkMarshal.Free((nint) pPEnabledInstanceExtensions);
        SilkMarshal.Free((nint) pPEnabledDeviceExtensions);

        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _mainQueueIndex,
            Flags = CommandPoolCreateFlags.TransientBit | CommandPoolCreateFlags.ResetCommandBufferBit
        };
        if (_vk.CreateCommandPool(_device, poolInfo, null, out _commandPool) != Result.Success)
            throw new Exception("Could not create command pool");
    }
    
    private static uint DebugCallback(DebugUtilsMessageSeverityFlagsEXT severityFlags,
                                      DebugUtilsMessageTypeFlagsEXT messageTypeFlags,
                                      DebugUtilsMessengerCallbackDataEXT* pCallbackData,
                                      void* pUserData)
    {
        var message = Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage);
        Console.WriteLine($"[Vulkan]: {severityFlags}: {message}");
        return Vk.False;
    }

    public Result SubmitMainQueue(SubmitInfo submitInfo, Fence fence) => _vk.QueueSubmit(_mainQueue, 1, submitInfo, fence);
    private Result WaitForQueue() => _vk.QueueWaitIdle(_mainQueue);
    
    public void Dispose()
    {
        _vk.DestroyCommandPool(_device, _commandPool, null);
        _vk.DestroyDevice(_device, null);
        _extDebugUtils.DestroyDebugUtilsMessenger(_instance, _debugUtilsMessenger, null);
        _vk.DestroyInstance(_instance, null);
        _vk.Dispose();
        _extDebugUtils.Dispose();
    }
}