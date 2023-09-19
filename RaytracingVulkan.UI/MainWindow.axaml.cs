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
        Image.SizeChanged += Resize;
    }

    private void Resize(object? sender, SizeChangedEventArgs e) => _viewModel.Resize((uint) e.NewSize.Width, (uint)  e.NewSize.Height);

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        _viewModel.Render();
        Dispatcher.UIThread.Post(InvalidateVisual, DispatcherPriority.Render);
        Dispatcher.UIThread.Post(Image.InvalidateVisual, DispatcherPriority.Render);
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
        Image.SizeChanged -= Resize;
        _viewModel.Dispose();
    }
}
