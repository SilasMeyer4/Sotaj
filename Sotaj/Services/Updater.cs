using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Sotaj.Services;

public class Updater
{
    private readonly UpdateManager _updateManager;
    private readonly string _repoUrl = "https://github.com/SilasMeyer4/Satoj";

    public Updater()
    {
        _updateManager = new UpdateManager(new GithubSource(_repoUrl, null, false));
    }
    
    public async Task<UpdateInfo?> CheckForUpdatesAsync()
    {
        try
        {
            return await _updateManager.CheckForUpdatesAsync();
        }
        catch (Exception e)
        {
            Trace.WriteLine("Update failed: " + e.Message);
            return null;
        }
    }
    
    public async Task<bool?> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
    {
        try
        {
            await _updateManager.DownloadUpdatesAsync(updateInfo);
            _updateManager.ApplyUpdatesAndRestart(updateInfo);
            return true;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Update failed: {e.Message}");
            return false;
        }
    }
}