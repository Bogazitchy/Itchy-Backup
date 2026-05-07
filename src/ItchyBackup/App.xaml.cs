using System.Windows;
using ItchyBackup.Services;

namespace ItchyBackup;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LogService.Initialize();
        LogService.Info("Itchy Backup başlatıldı.");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LogService.Info("Itchy Backup kapatıldı.");
        base.OnExit(e);
    }
}
