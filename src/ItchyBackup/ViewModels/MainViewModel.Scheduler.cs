using CommunityToolkit.Mvvm.Input;

namespace ItchyBackup.ViewModels;

public partial class MainViewModel
{
    [RelayCommand] public void ToggleMon() { SchedMon = !SchedMon; }
    [RelayCommand] public void ToggleTue() { SchedulerTue = !SchedulerTue; }
    [RelayCommand] public void ToggleWed() { SchedWed = !SchedWed; }
    [RelayCommand] public void ToggleThu() { SchedThu = !SchedThu; }
    [RelayCommand] public void ToggleFri() { SchedFri = !SchedFri; }
    [RelayCommand] public void ToggleSat() { SchedSat = !SchedSat; }
    [RelayCommand] public void ToggleSun() { SchedSun = !SchedSun; }
}
