using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Reflection;
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
    [ObservableProperty] private FolderViewModel _folderViewModel;
    [ObservableProperty] private float _frameTime;
    [ObservableProperty] private float _ioTime;
    [ObservableProperty] private float _fps;
    [ObservableProperty] private bool _isRunning = true;
    
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
        _cameraViewModel = new CameraViewModel(new Camera(40, 0.1f, 1000f)
        {
            Position = new Vector3(0,0,5),
            Rotation = new Vector3(0, -180, 0)
        });
        _cameraViewModel.ActiveCamera.RecalculateView();
        _folderViewModel = new FolderViewModel(Path.GetDirectoryName(typeof(Renderer).Assembly.Location) + @"\assets");
        
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
        Fps = 1 / (FrameTime / 1000);
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
        var forward = ActiveCamera.CalculateForward();
        var up = ActiveCamera.CalculateUp();
        var right = Vector3.Cross(forward, up);
        var moved = false;
        
        var moveVector = Vector3.Zero;
        if (_input.PressedKeys.Contains(Key.W))
        {
            moveVector += forward;
            moved = true;
        }
        if (_input.PressedKeys.Contains(Key.S))
        {
            moveVector -= forward;
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
            moveVector += up;
            moved = true;
        }
        if (_input.PressedKeys.Contains(Key.E))
        {
            moveVector -= up;
            moved = true;
        }

        if (moved && moveVector.Length() > 0)
        {
            moveVector = Vector3.Normalize(moveVector) * _cameraViewModel.MovementSpeed * deltaTime;
            _cameraViewModel.Position += moveVector;
            ActiveCamera.RecalculateView();
            Reset();
        }

        if (_input.MouseDelta != Vector2.Zero)
        {
            var rotation = new Vector3(-_input.MouseDelta.Y, _input.MouseDelta.X, 0);
            rotation *= _cameraViewModel.MouseSensitivity * deltaTime;
            _cameraViewModel.Rotation += rotation;
            _cameraViewModel.Rotation = Vector3.Clamp(_cameraViewModel.Rotation, new Vector3(-360), new Vector3(360));
            ActiveCamera.RecalculateView();
            Reset();
        }
    }

    private void CopyImageToHost()
    {
        using var buffer = _image.Lock();
        _renderer.CopyDataTo(buffer.Address);
    }

    public void Reset()
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
