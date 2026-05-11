using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ItchyBackup.Models;

namespace ItchyBackup.Views;

public partial class SaveProfileDialog : Window
{
    public string ProfileName => NameBox.Text.Trim();
    public string SelectedIcon { get; private set; } = "person";

    private Border? _activeBorder;

    public SaveProfileDialog(string? initialName = null, string? initialIcon = null)
    {
        InitializeComponent();
        if (initialIcon != null) SelectedIcon = initialIcon;
        BuildIconGrid();
        if (initialName != null)
        {
            NameBox.Text = initialName;
            DialogTitleBlock.Text = "Profili Düzenle";
        }
        Loaded += (_, _) =>
        {
            NameBox.Focus();
            NameBox.SelectAll();
        };
    }

    private void BuildIconGrid()
    {
        foreach (var icon in ProfileIcons.All)
        {
            var tile = MakeIconTile(icon);
            IconPanel.Children.Add(tile);

            if (icon.Key == SelectedIcon)
                Activate(tile);
        }
    }

    private Border MakeIconTile(ProfileIcons.IconDef icon)
    {
        var path = new System.Windows.Shapes.Path
        {
            Data = Geometry.Parse(icon.Path),
            Fill = (SolidColorBrush)FindResource("Accent"),
            Stretch = Stretch.Uniform,
            Width = 18,
            Height = 18
        };

        var label = new TextBlock
        {
            Text = icon.Label,
            FontSize = 8,
            FontFamily = new System.Windows.Media.FontFamily("Consolas"),
            Foreground = (SolidColorBrush)FindResource("TextTertiary"),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(0, 3, 0, 0),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var inner = new StackPanel
        {
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Center
        };
        inner.Children.Add(path);
        inner.Children.Add(label);

        var tile = new Border
        {
            Width = 60,
            Height = 52,
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1.5),
            Background = (SolidColorBrush)FindResource("BgQuaternary"),
            BorderBrush = (SolidColorBrush)FindResource("BorderBrush"),
            Margin = new Thickness(2),
            Cursor = System.Windows.Input.Cursors.Hand,
            Child = inner,
            Tag = icon.Key,
            ToolTip = icon.Label
        };

        tile.MouseLeftButtonUp += (_, _) =>
        {
            SelectedIcon = (string)tile.Tag;
            Activate(tile);
        };

        tile.MouseEnter += (_, _) =>
        {
            if (tile != _activeBorder)
                tile.BorderBrush = (SolidColorBrush)FindResource("BorderHoverBrush");
        };

        tile.MouseLeave += (_, _) =>
        {
            if (tile != _activeBorder)
                tile.BorderBrush = (SolidColorBrush)FindResource("BorderBrush");
        };

        return tile;
    }

    private void Activate(Border tile)
    {
        if (_activeBorder != null)
        {
            _activeBorder.Background = (SolidColorBrush)FindResource("BgQuaternary");
            _activeBorder.BorderBrush = (SolidColorBrush)FindResource("BorderBrush");
            if (_activeBorder.Child is StackPanel sp)
                foreach (var c in sp.Children.OfType<System.Windows.Shapes.Path>())
                    c.Fill = (SolidColorBrush)FindResource("Accent");
        }

        tile.Background = (SolidColorBrush)FindResource("AccentSubtle");
        tile.BorderBrush = (SolidColorBrush)FindResource("Accent");

        if (tile.Child is StackPanel sp2)
            foreach (var c in sp2.Children.OfType<System.Windows.Shapes.Path>())
                c.Fill = (SolidColorBrush)FindResource("AccentLight");

        _activeBorder = tile;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            System.Windows.MessageBox.Show("Profil adı girin.", "Itchy Backup",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            NameBox.Focus();
            return;
        }
        DialogResult = true;
    }
}
