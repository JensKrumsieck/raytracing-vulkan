using System.Collections.Generic;
using Avalonia.Input;

namespace RaytracingVulkan.UI;

public class InputHandler
{
    public HashSet<Key> PressedKeys { get; } = new();
    public void Down (Key k) => PressedKeys.Add(k);
    public void Up (Key k) => PressedKeys.Remove(k);
    public override string ToString() => string.Join(", ", PressedKeys);
}
