using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ItchyBackup.Models;
using ItchyBackup.ViewModels;

namespace ItchyBackup.Views;

public partial class MainWindow : Window
{
    private bool _themeAnimating = false;

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

    private async void ThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_themeAnimating) return;
        _themeAnimating = true;

        var vm = (MainViewModel)DataContext;
        bool goingToLight = vm.ThemeName == "Dark";

        // Hedef temanın arka plan rengini overlay'e ver
        ThemeTransitionOverlay.Background = new SolidColorBrush(goingToLight
            ? System.Windows.Media.Color.FromRgb(0xEC, 0xE6, 0xFC)   // aydınlık tema bg
            : System.Windows.Media.Color.FromRgb(0x0C, 0x0B, 0x18)); // koyu tema bg

        // Overlay fade-in
        ThemeTransitionOverlay.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 0.65, new Duration(TimeSpan.FromMilliseconds(130))));
        await Task.Delay(155);

        // Tema uygula (overlay arkasında, kullanıcı görmez)
        vm.SetThemeCommand.Execute(goingToLight ? "Light" : "Dark");

        // Overlay fade-out — yeni tema ortaya çıkar
        ThemeTransitionOverlay.BeginAnimation(OpacityProperty,
            new DoubleAnimation(0.65, 0, new Duration(TimeSpan.FromMilliseconds(230))));
        await Task.Delay(250);

        _themeAnimating = false;
    }

    private void GitHubBtn_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://github.com/Bogazitchy/Itchy-Backup",
            UseShellExecute = true
        });
    }

    private void NetworkPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is System.Windows.Controls.PasswordBox box)
            vm.NetworkPassword = box.Password;
    }

    private void ZipPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is System.Windows.Controls.PasswordBox box)
            vm.ZipPassword = box.Password;
    }

    private void RestoreZipPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is System.Windows.Controls.PasswordBox box)
            vm.RestoreZipPassword = box.Password;
    }

    private void SmtpPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is System.Windows.Controls.PasswordBox box)
            vm.SmtpPassword = box.Password;
    }

    private void NetworkPasswordReveal_Click(object sender, RoutedEventArgs e)
        => TogglePasswordReveal(NetworkPasswordBox, NetworkPasswordRevealBox);

    private void ZipPasswordReveal_Click(object sender, RoutedEventArgs e)
        => TogglePasswordReveal(ZipPasswordBox, ZipPasswordRevealBox);

    private void RestoreZipPasswordReveal_Click(object sender, RoutedEventArgs e)
        => TogglePasswordReveal(RestoreZipPasswordBox, RestoreZipPasswordRevealBox);

    private void SmtpPasswordReveal_Click(object sender, RoutedEventArgs e)
        => TogglePasswordReveal(SmtpPasswordBox, SmtpPasswordRevealBox);

    private static void TogglePasswordReveal(System.Windows.Controls.PasswordBox passwordBox, System.Windows.Controls.TextBox revealBox)
    {
        if (revealBox.Visibility == Visibility.Visible)
        {
            passwordBox.Password = revealBox.Text;
            revealBox.Visibility = Visibility.Collapsed;
            passwordBox.Visibility = Visibility.Visible;
            passwordBox.Focus();
        }
        else
        {
            revealBox.Text = passwordBox.Password;
            passwordBox.Visibility = Visibility.Collapsed;
            revealBox.Visibility = Visibility.Visible;
            revealBox.Focus();
            revealBox.CaretIndex = revealBox.Text.Length;
        }
    }
}
