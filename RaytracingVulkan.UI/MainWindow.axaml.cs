using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using RaytracingVulkan.UI.ViewModels;

namespace RaytracingVulkan.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _viewModel.Render();
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        _viewModel.Dispose();
    }
}
