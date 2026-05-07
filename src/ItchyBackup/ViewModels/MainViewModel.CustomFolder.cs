using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ItchyBackup.Models;
using ItchyBackup.Services;
using System.Collections.ObjectModel;

namespace ItchyBackup.ViewModels;

public partial class MainViewModel
{
    // Özel klasör paneli için
    [ObservableProperty] private string _newCustomPath = "";

    public BackupCategory? CustomCategory =>
        Categories.FirstOrDefault(c => c.Type == CategoryType.CustomFolders);

    [RelayCommand]
    public void AddCustomFolder()
    {
        var path = NewCustomPath.Trim();
        if (string.IsNullOrEmpty(path))
        {
            // Klasör seç dialogu
            using var d = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Yedeklenecek özel klasörü seçin",
                UseDescriptionForTitle = true
            };
            if (d.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            path = d.SelectedPath;
        }

        if (string.IsNullOrEmpty(path)) return;

        var cat = CustomCategory;
        if (cat == null) return;

        // Zaten var mı?
        if (cat.Items.Any(i => i.Path.Equals(path, StringComparison.OrdinalIgnoreCase)))
        {
            System.Windows.MessageBox.Show("Bu klasör zaten ekli.", "Itchy Backup",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        var item = CategoryBuilder.CreateCustomFolderItem(path);
        item.Parent = cat;
        item.IsSelected = true;
        item.PropertyChanged += (_, _) => UpdateSummary();
        cat.Items.Add(item);
        cat.UpdateMasterCheckState();
        NewCustomPath = "";
        UpdateSummary();
        SaveCustomFolders();
    }

    [RelayCommand]
    public void RemoveCustomFolder(BackupItem? item)
    {
        if (item == null) return;
        var cat = CustomCategory;
        if (cat == null) return;
        cat.Items.Remove(item);
        cat.UpdateMasterCheckState();
        UpdateSummary();
        SaveCustomFolders();
    }

    [RelayCommand]
    public void BrowseCustomFolder()
    {
        using var d = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Yedeklenecek özel klasörü seçin",
            UseDescriptionForTitle = true,
            SelectedPath = NewCustomPath
        };
        if (d.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            NewCustomPath = d.SelectedPath;
    }

    private void SaveCustomFolders()
    {
        var cat = CustomCategory;
        if (cat == null) return;
        var paths = cat.Items.Select(i => i.Path).ToList();
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(paths);
        var dir = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ItchyBackup");
        System.IO.Directory.CreateDirectory(dir);
        System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "custom_folders.json"), json);
    }

    private void LoadCustomFolders()
    {
        var file = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ItchyBackup", "custom_folders.json");
        if (!System.IO.File.Exists(file)) return;
        try
        {
            var cat = CustomCategory;
            if (cat == null) return;
            var json = System.IO.File.ReadAllText(file);
            var paths = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json);
            if (paths == null) return;
            foreach (var path in paths)
            {
                var item = CategoryBuilder.CreateCustomFolderItem(path);
                item.Parent = cat;
                item.PropertyChanged += (_, _) => UpdateSummary();
                cat.Items.Add(item);
            }
            cat.UpdateMasterCheckState();
        }
        catch { }
    }
}
