using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace RaytracingVulkan.UI.ViewModels;

public partial class FolderViewModel : ObservableObject
{
    [ObservableProperty] private string _folder;
    public ObservableCollection<string> Files { get; }
    
    public FolderViewModel(string path)
    {
        var entries = Directory.EnumerateFileSystemEntries(path);
        Files = new ObservableCollection<string>(entries.Select(Path.GetFileName)!);
        Console.WriteLine(string.Join(", ", Files));
        _folder = path;
    }
}
