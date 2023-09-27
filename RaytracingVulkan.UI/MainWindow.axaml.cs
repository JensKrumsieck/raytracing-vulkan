using System;
using System.Numerics;
using Avalonia;
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
    
    public MainWindow()
    {
        InitializeComponent();
        DataContext = _viewModel = new MainViewModel(_input);
        Initialize();
    }
    private void Initialize()
    {
        _isInitialized = true;
        Image.SizeChanged += OnResize;
        Image.KeyDown += OnKeyDown;
        Image.KeyUp += OnKeyUp;
        Image.PointerPressed += OnMouseDown;
        Image.PointerReleased += OnMouseUp;
        Image.PointerMoved += OnMouseMove;
        
        var selfVisual = ElementComposition.GetElementVisual(this)!;
        _compositor = selfVisual.Compositor;
        UpdateFrame();
    }

    public void UpdateFrame()
    {
        if (!_viewModel.IsRunning || !_isInitialized) return;
        
        _viewModel.Render();
        _compositor?.RequestCompositionUpdate(UpdateFrame);
        Dispatcher.UIThread.Post(Image.InvalidateVisual, DispatcherPriority.Render);
    }
    
    private void OnResize(object? sender, SizeChangedEventArgs e) => _viewModel.Resize((uint) e.NewSize.Width, (uint)  e.NewSize.Height);
    private void OnKeyDown(object? sender, KeyEventArgs e) => _input.Down(e.Key);
    private void OnKeyUp(object? sender, KeyEventArgs e) => _input.Up(e.Key);
    private void OnMouseDown(object? sender, PointerPressedEventArgs e)
    {
        if(!Image.IsFocused) return;
        var point = e.GetCurrentPoint(Image);
        if (!point.Properties.IsMiddleButtonPressed) return;
        _input.MouseLastPosition = point.Position;
        _input.CaptureMouseMove = true;
    }
    private void OnMouseUp(object? sender, PointerReleasedEventArgs e)
    {
        if(!Image.IsFocused) return;
        if (e.InitialPressMouseButton != MouseButton.Middle) return;
        _input.MouseDelta = Vector2.Zero;
        _input.MouseLastPosition = new Point(0, 0);
        _input.CaptureMouseMove = false;
    }

    private void OnMouseMove(object? sender, PointerEventArgs e)
    {
        if (!_input.CaptureMouseMove || !Image.IsFocused) return;
        var currentPosition = e.GetPosition(Image);
        var delta = _input.MouseLastPosition - currentPosition;
        _input.MouseDelta = new Vector2((float) delta.X, (float) delta.Y);
        _input.MouseLastPosition = currentPosition;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        _isInitialized = false;
        Image.SizeChanged -= OnResize;
        Image.KeyDown -= OnKeyDown;
        Image.KeyUp -= OnKeyUp;
        base.OnClosing(e);
    }
    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }
}
