using System.Windows;
using System.Windows.Threading;

namespace ItchyBackup.Views;

public partial class SplashWindow : Window
{
    private readonly DispatcherTimer _timer;
    private double _progress;

    public SplashWindow()
    {
        InitializeComponent();
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(14)
        };
        _timer.Tick += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _progress = Math.Min(100, _progress + 1.6);
        LoadingBar.Width = 220 * _progress / 100;
        if (_progress >= 100) _timer.Stop();
    }
}
