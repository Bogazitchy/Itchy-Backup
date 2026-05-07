using System.Windows;
using System.Windows.Input;
using ItchyBackup.Models;

namespace ItchyBackup.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
        => Close();

    private void CategoryCheckBox_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        if (sender is System.Windows.Controls.CheckBox cb && cb.Tag is BackupCategory cat)
        {
            bool newValue = cat.MasterChecked != true;
            cat.SetAllSelected(newValue);
        }
    }
}
