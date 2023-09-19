using System.Numerics;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace RaytracingVulkan.UI.ViewModels;

public partial class CameraViewModel : ObservableObject
{
    [ObservableProperty] private Camera _activeCamera;
    public CameraViewModel(Camera activeCamera) => _activeCamera = activeCamera;

    public Vector3 Position
    {
        get => _activeCamera.Position;
        set
        {
            _activeCamera.Position = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(X));
            OnPropertyChanged(nameof(Y));
            OnPropertyChanged(nameof(Z));
            _activeCamera.RecalculateView();
        }
    }

    public float X
    {
        get => Position.X;
        set => Position = Position with {X = value};
    }
    
    public float Y
    {
        get => Position.Y;
        set => Position = Position with {Y = value};
    }
    
    public float Z
    {
        get => Position.Z;
        set => Position = Position with {Z = value};
    }
}
