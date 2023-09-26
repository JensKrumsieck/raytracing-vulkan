using System;
using System.Diagnostics;
using System.Numerics;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Vector = Avalonia.Vector;

namespace RaytracingVulkan.UI.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    //observables
    [ObservableProperty] private WriteableBitmap _image;
    [ObservableProperty] private CameraViewModel _cameraViewModel;
    [ObservableProperty] private float _frameTime;
    [ObservableProperty] private float _ioTime;
    
    //camera and input
    private Camera ActiveCamera => _cameraViewModel.ActiveCamera;
    private readonly InputHandler _input;
    private readonly Renderer _renderer;
    
    //stopwatches
    private readonly Stopwatch _frameTimeStopWatch = new();
    private readonly Stopwatch _ioStopWatch = new();
    
    private uint _viewportWidth;
    private uint _viewportHeight;
    
    public MainViewModel(InputHandler input)
    {
        _input = input;
        _cameraViewModel = new CameraViewModel(new Camera(40, 0.1f, 1000f){Position = new Vector3(0,0,5)});
        _cameraViewModel.ActiveCamera.RecalculateView();
        //needed for initial binding
        _image = new WriteableBitmap(new PixelSize(1, 1), new Vector(96, 96), PixelFormat.Bgra8888);
        _renderer = new Renderer((Application.Current as App)!.VkContext);
    }
    public void Render()
    {
        if (!_renderer.IsReady) return;
        _frameTimeStopWatch.Start();
        
        _renderer.PrepareImage();
        _ioStopWatch.Start();
        CopyImageToHost();
        _ioStopWatch.Stop();
        
        HandleInput(FrameTime / 1000f);
        _renderer.Render(ActiveCamera);
        IoTime = (float) _ioStopWatch.Elapsed.TotalMilliseconds;
        _ioStopWatch.Reset();
        
        _frameTimeStopWatch.Stop();
        FrameTime = (float) _frameTimeStopWatch.Elapsed.TotalMilliseconds;
        _frameTimeStopWatch.Reset();
    }
    public void Resize(uint x, uint y)
    {
        _viewportWidth = x;
        _viewportHeight = y;
        _renderer.Resize(x, y);
        _image = new WriteableBitmap(new PixelSize((int) _viewportWidth, (int) _viewportHeight), new Vector(96, 96), PixelFormat.Bgra8888); 
        ActiveCamera.Resize(x, y);
        Reset();
    }
    private void HandleInput(float deltaTime)
    {
        //camera move
        var speed = 5f * deltaTime;
        var right = Vector3.Cross(ActiveCamera.Forward, Vector3.UnitY);
        var moved = false;
        
        var moveVector = Vector3.Zero;
        if (_input.PressedKeys.Contains(Key.W))
        {
            moveVector += ActiveCamera.Forward;
            moved = true;
        }
        if (_input.PressedKeys.Contains(Key.S))
        { 
            moveVector -= ActiveCamera.Forward;
            moved = true;
            
        }
        if (_input.PressedKeys.Contains(Key.D))
        {
            moveVector += right;
            moved = true;
        }
        if (_input.PressedKeys.Contains(Key.A))
        {
            moveVector -= right;
            moved = true;
        }
        if (_input.PressedKeys.Contains(Key.Q))
        {
            moveVector += Vector3.UnitY;
            moved = true;
        }
        if (_input.PressedKeys.Contains(Key.E))
        {
            moveVector -= Vector3.UnitY;
            moved = true;
        }

        if (!moved) return;
        if(moveVector.Length() == 0) return;
        
        moveVector = Vector3.Normalize(moveVector) * speed;
        _cameraViewModel.Position += moveVector;
        ActiveCamera.RecalculateView();
        Reset();
    }

    private void CopyImageToHost()
    {
        using var buffer = _image.Lock();
        _renderer.CopyDataTo(buffer.Address);
    }

    private void Reset()
    {
        _renderer.Reset();
        //save old image
        var tmp = _image;
        _image = new WriteableBitmap(new PixelSize((int) _viewportWidth, (int) _viewportHeight), new Vector(96, 96), PixelFormat.Bgra8888);
        OnPropertyChanged(nameof(Image));
        //dispose old
        tmp?.Dispose();
    }
    
    public void Dispose()
    {
        _renderer.Dispose();
        _image.Dispose();
        GC.SuppressFinalize(this);
    }
}
