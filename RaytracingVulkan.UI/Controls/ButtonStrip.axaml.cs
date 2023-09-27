using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Projektanker.Icons.Avalonia;
using RaytracingVulkan.UI.ViewModels;

namespace RaytracingVulkan.UI.Controls;

public partial class ButtonStrip : UserControl
{
    private MainViewModel? _viewModel;
    private MainWindow? _parent;
    
    private readonly Icon _playIcon = new() {Value = "fa-play"};
    private readonly Icon _pauseIcon = new() {Value = "fa-pause"};
    
    public ButtonStrip() => InitializeComponent();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _viewModel ??= DataContext as MainViewModel;
        _parent ??= e.Root as MainWindow;
        base.OnAttachedToVisualTree(e);
    }

    private void BtnStop_OnClick(object? sender, RoutedEventArgs e)
    {
        _viewModel!.IsRunning = false;
        _viewModel.Reset();
    }
    private void BtnPlay_OnClick(object? sender, RoutedEventArgs e)
    {
        _viewModel!.IsRunning = !_viewModel.IsRunning;
        BtnPlay.Content = _viewModel.IsRunning ? _playIcon : _pauseIcon;
        _parent!.UpdateFrame();
    }
}