using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using RaytracingVulkan.UI.ViewModels;

namespace RaytracingVulkan.UI;

public partial class MainWindow : Window
{
    private Stopwatch RenderStopWatch = new();
    private Stopwatch VulkanStopWatch = new();
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }

    public override void Render(DrawingContext context)
    {
        RenderStopWatch.Start();
        base.Render(context);
        VulkanStopWatch.Start();
        (DataContext as MainViewModel)?.Render();
        VulkanStopWatch.Stop();
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
        RenderStopWatch.Stop();
        Console.WriteLine($"Elapsed Render: {RenderStopWatch.Elapsed.TotalMilliseconds:N2} ms - Vulkan {VulkanStopWatch.Elapsed.TotalMilliseconds:N2} ms");
        VulkanStopWatch.Reset();
        RenderStopWatch.Reset();
    }
}
