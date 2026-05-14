using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Sotaj.ViewModels;


namespace Sotaj.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void InputElement_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.HotKeyString = e.KeyModifiers.ToString() + " " +  e.Key.ToString();
        }
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        // Get top level from the current control. Alternatively, you can use Window reference instead.
        var topLevel = TopLevel.GetTopLevel(this);

        // Start async operation to open the dialog.
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Text File",
            AllowMultiple = false
        });

        
        if (files.Count >= 1)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.AhkExePath = files[0].Path.AbsolutePath;
                vm.SaveSettings();
            }
        }
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            var info = await vm.CheckForUpdates();
            if (info != null)
            {
               var messageBox =  MessageBoxManager.GetMessageBoxStandard("Update",
                    "There is an update available. Do you want to install it?", ButtonEnum.YesNo);
               var  result = await messageBox.ShowAsync();

               if (result == ButtonResult.Yes)
               {
                   await vm.DownloadAndInstallUpdateAsync(info);
               }
            }
            
            vm.LoadSettings();
        }
    }
}