using System.Windows;
using System.Windows.Input;
using ItchyBackup.Models;
using ItchyBackup.ViewModels;

namespace ItchyBackup.Views;

public partial class MainWindow : Window
{
    public MainWindow() { InitializeComponent(); }

    private void HourSpinner_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var vm = (MainViewModel)DataContext;
        if (e.Delta > 0) vm.IncrementHourCommand.Execute(null);
        else vm.DecrementHourCommand.Execute(null);
        e.Handled = true;
    }

    private void MinuteSpinner_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var vm = (MainViewModel)DataContext;
        if (e.Delta > 0) vm.IncrementMinuteCommand.Execute(null);
        else vm.DecrementMinuteCommand.Execute(null);
        e.Handled = true;
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    { if (e.ChangedButton == MouseButton.Left) DragMove(); }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        var vm = (MainViewModel)DataContext;
        if (vm.MinimizeToTray) Hide();
        else Close();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        var app = (App)System.Windows.Application.Current;
        if (app.IsQuitting) return;
        var vm = (MainViewModel)DataContext;
        if (vm.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void CategoryCheckBox_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is System.Windows.Controls.CheckBox cb && cb.Tag is BackupCategory cat)
            cat.SetAllSelected(cat.MasterChecked != true);
    }

    private void GitHubBtn_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/Bogazitchy/Itchy-Backup",
            UseShellExecute = true
        });
    }
}
