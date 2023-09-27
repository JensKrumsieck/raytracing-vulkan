using System.Collections.Generic;
using System.Numerics;
using Avalonia;
using Avalonia.Input;

namespace RaytracingVulkan.UI;

public class InputHandler
{
    public bool CaptureMouseMove = false;
    public Point MouseLastPosition;
    public Vector2 MouseDelta;
    public HashSet<Key> PressedKeys { get; } = new();
    public void Down (Key k) => PressedKeys.Add(k);
    public void Up (Key k) => PressedKeys.Remove(k);
    public override string ToString() => string.Join(", ", PressedKeys);
}
