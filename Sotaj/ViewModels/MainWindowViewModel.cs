using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Sotaj.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty] private string? _hotKeyString;

    [ObservableProperty] private string? _pasteString;
    
    [ObservableProperty] private string? _fileName;

    [ObservableProperty] private int _tabIndex;
    
    [ObservableProperty] private ObservableCollection<string> _fileNames = new ObservableCollection<string>();

    [ObservableProperty] private string? _selectedScript;

    [ObservableProperty] private string? _ahkExePath;

    private const string AppName = "Sotaj";
    
    private readonly string _appFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppName);

    [RelayCommand]
    private void WriteHotKeyScript()
    {
        if (string.IsNullOrWhiteSpace(FileName))
        {
            return;
        }
        // Create an AutoHotkey script from HotKeyString and PasteString and write it to ScriptFilePath.
        Directory.CreateDirectory(_appFolder);
        
        var rawHotKey = HotKeyString;
        if (string.IsNullOrWhiteSpace(rawHotKey))
        {
            return;
        }

        var hotKeyInput = rawHotKey.Trim();
        string ahkHotKey = ConvertToAhkHotkey(hotKeyInput);

        string paste = PasteString ?? string.Empty;
        string escapedForAssignment = paste.Replace("\"", "\"\"");

        var scriptText = $"{ahkHotKey}::\n{{\n\tA_Clipboard := \"{escapedForAssignment}\"\n\tSend \"^v\"\n}}\n";

        try
        {
            var path = Path.Combine(_appFolder, FileName);
            if (string.IsNullOrEmpty(path))
            {
                Debug.WriteLine("ScriptFilePath is empty - nothing written.");
                return;
            }

            File.WriteAllText(path, scriptText);
            Debug.WriteLine($"AutoHotkey script written to: {path}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to write AutoHotkey script: {ex}");
            throw;
        }
    }

    private static string ConvertToAhkHotkey(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var s = input.Trim();

        // If already using AHK symbols, remove spaces and return
        if (Regex.IsMatch(s, @"[\^!+#]"))
            return s.Replace(" ", "");

        string[] tokens;
        if (s.Contains("+") || s.Contains(" "))
        {
            tokens = s.Split(new[] { '+', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }
        else
        {
            // Try to split "ControlH" -> ["Control","H"] (case-insensitive)
            var m = Regex.Match(s, "^(?i)(ctrl|control|alt|shift|win|windows|super)(.+)$");
            if (m.Success)
                tokens = new[] { m.Groups[1].Value, m.Groups[2].Value };
            else
                tokens = new[] { s };
        }

        var prefix = string.Empty;
        var key = string.Empty;

        foreach (var t in tokens)
        {
            var low = t.Trim();
            if (Regex.IsMatch(low, "^(?i:ctrl|control)$"))
                prefix += "^";
            else if (Regex.IsMatch(low, "^(?i:alt)$"))
                prefix += "!";
            else if (Regex.IsMatch(low, "^(?i:shift)$"))
                prefix += "+";
            else if (Regex.IsMatch(low, "^(?i:win|windows|super)$"))
                prefix += "#";
            else
                key = low;
        }

        if (string.IsNullOrEmpty(key))
            return prefix; // only modifiers

        // Normalize single-letter keys to lower-case (AHK typically uses lower-case letters for hotkeys)
        if (key.Length == 1)
            key = key.ToLower();

        return prefix + key;
    }
    
    

    [RelayCommand]
    private void RunScript()
    {
        // Determine which script to run: prefer SelectedScript, otherwise first entry in FileNames
        var scriptPath = SelectedScript;
        if (string.IsNullOrWhiteSpace(scriptPath) && FileNames.Count > 0)
        {
            scriptPath = FileNames[0];
        }

        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            Debug.WriteLine("No script selected to run.");
            return;
        }

        // Make sure we have an absolute path
        if (!Path.IsPathRooted(scriptPath))
            scriptPath = Path.Combine(_appFolder, scriptPath);

        if (!File.Exists(scriptPath))
        {
            Debug.WriteLine($"Script not found: {scriptPath}");
            return;
        }
        
        AhkExePath = AhkExePath?.Replace("%20", " ");

        try
        {
            var psi = new ProcessStartInfo { FileName = AhkExePath, UseShellExecute = true, Arguments = $"\"{scriptPath}\"" };
            Trace.WriteLine($"Started script via file association: {psi.FileName} + {psi.Arguments}");
            Process.Start(psi);
          
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to start script '{scriptPath}': {ex}");
        }
    }

    partial void OnTabIndexChanged(int value)
    {
        if (value == 1)
        {
            UpdateScriptList();
        }
    }

    private void UpdateScriptList()
    {
        FileNames = new ObservableCollection<string>(Directory.GetFileSystemEntries(Path.Combine(_appFolder)));
    }

    public void SaveSettings()
    {
        var json = JsonSerializer.Serialize(AhkExePath);
        var folderPath = Path.Combine(_appFolder, "settings");
        Directory.CreateDirectory(folderPath);
        var filePath = Path.Combine(folderPath, "settings.json");
        File.WriteAllText(filePath, json);
    }

    public void LoadSettings()
    {
        var path = Path.Combine(_appFolder, "settings", "settings.json");
        if (File.Exists(path))
        {
            AhkExePath = JsonSerializer.Deserialize<string>(File.ReadAllText(path));
        }
    }
}
