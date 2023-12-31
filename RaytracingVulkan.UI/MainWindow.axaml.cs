using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using RaytracingVulkan.UI.ViewModels;

namespace RaytracingVulkan.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly InputHandler _input = new();

    private Compositor? _compositor;

    private bool _isInitialized;
    private bool _updateRequested;
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel = new MainViewModel(_input);
        Initialize();
    }
    private void Initialize()
    {
        _isInitialized = true;
        Image.SizeChanged += Resize;
        var selfVisual = ElementComposition.GetElementVisual(this)!;
        _compositor = selfVisual.Compositor;
        UpdateFrame();
    }
    
    private void UpdateFrame()
    {
        _updateRequested = false;
        if (_updateRequested || !_isInitialized) return;
        
        _viewModel.Render();
        _compositor?.RequestCompositionUpdate(UpdateFrame);
        Dispatcher.UIThread.Post(Image.InvalidateVisual, DispatcherPriority.Render);
    }
    
    private void Resize(object? sender, SizeChangedEventArgs e) => _viewModel.Resize((uint) e.NewSize.Width, (uint)  e.NewSize.Height);
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
        _isInitialized = false;
        Image.SizeChanged -= Resize;
        base.OnClosing(e);
    }
    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
