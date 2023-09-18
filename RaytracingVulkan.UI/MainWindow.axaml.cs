using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using RaytracingVulkan.UI.ViewModels;

namespace RaytracingVulkan.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly InputHandler _input = new();
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel = new MainViewModel(_input);
    }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _viewModel.Render();
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
    }
    
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _input.Down(e.Key);
    }
    
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _input.Up(e.Key);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        _viewModel.Dispose();
    }
}
