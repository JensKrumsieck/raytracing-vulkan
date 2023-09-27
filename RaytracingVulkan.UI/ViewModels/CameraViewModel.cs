using System.Numerics;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace RaytracingVulkan.UI.ViewModels;

public partial class CameraViewModel : ObservableObject
{
    [ObservableProperty] private Camera _activeCamera;
    [ObservableProperty] private float _movementSpeed = 5f;
    [ObservableProperty] private float _mouseSensitivity = 20f;
    
    public CameraViewModel(Camera activeCamera) => _activeCamera = activeCamera;

    public Vector3 Position
    {
        get => _activeCamera.Position;
        set
        {
            _activeCamera.Position = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PositionX));
            OnPropertyChanged(nameof(PositionY));
            OnPropertyChanged(nameof(PositionZ));
            _activeCamera.RecalculateView();
        }
    }
    
    public Vector3 Rotation
    {
        get => _activeCamera.Rotation;
        set
        {
            _activeCamera.Rotation = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(RotationX));
            OnPropertyChanged(nameof(RotationY));
            OnPropertyChanged(nameof(RotationZ));
            _activeCamera.RecalculateView();
        }
    }

    public float PositionX
    {
        get => Position.X;
        set => Position = Position with {X = value};
    }
    
    public float PositionY
    {
        get => Position.Y;
        set => Position = Position with {Y = value};
    }
    
    public float PositionZ
    {
        get => Position.Z;
        set => Position = Position with {Z = value};
    }
    
    public float RotationX
    {
        get => Rotation.X;
        set => Rotation = Rotation with {X = value};
    }
    
    public float RotationY
    {
        get => Rotation.Y;
        set => Rotation = Rotation with {Y = value};
    }
    
    public float RotationZ
    {
        get => Rotation.Z;
        set => Rotation = Rotation with {Z = value};
    }
}
