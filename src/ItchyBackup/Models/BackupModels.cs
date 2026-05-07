using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ItchyBackup.Models;

public enum CategoryType
{
    UserFolders, Browsers, Outlook, Databases, VirtualMachines, CloudStorage, CustomFolders
}

public partial class BackupItem : ObservableObject
{
    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private bool _isDetected = true;
    [ObservableProperty] private string _estimatedSize = "";
    [ObservableProperty] private string _statusMessage = "";

    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Path { get; set; } = "";
    public string Icon { get; set; } = "";
    public bool RequiresVss { get; set; } = false;
    public BackupCategory? Parent { get; set; }

    partial void OnIsSelectedChanged(bool value) => Parent?.UpdateMasterCheckState();
}

public partial class BackupCategory : ObservableObject
{
    [ObservableProperty] private bool? _masterChecked = false;
    [ObservableProperty] private bool _isExpanded = false;

    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string AccentColor { get; set; } = "#6C5CE7";
    public CategoryType Type { get; set; }
    public ObservableCollection<BackupItem> Items { get; set; } = new();

    public string CategoryIconPath => Type switch
    {
        CategoryType.UserFolders     => "M10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6H12L10,4Z",
        CategoryType.Browsers        => "M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M11,14H9L12,6L15,14H13V18H11V14Z",
        CategoryType.Outlook         => "M20,8L12,13L4,8V6L12,11L20,6M20,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V6C22,4.89 21.1,4 20,4Z",
        CategoryType.Databases       => "M12,3C7.58,3 4,4.79 4,7C4,9.21 7.58,11 12,11C16.42,11 20,9.21 20,7C20,4.79 16.42,3 12,3M4,9V12C4,14.21 7.58,16 12,16C16.42,16 20,14.21 20,12V9C20,11.21 16.42,13 12,13C7.58,13 4,11.21 4,9M4,14V17C4,19.21 7.58,21 12,21C16.42,21 20,19.21 20,17V14C20,16.21 16.42,18 12,18C7.58,18 4,16.21 4,14Z",
        CategoryType.VirtualMachines => "M4,6H20V16H4M20,18A2,2 0 0,0 22,16V6C22,4.89 21.1,4 20,4H4C2.89,4 2,4.89 2,6V16A2,2 0 0,0 4,18H0V20H24V18H20Z",
        CategoryType.CloudStorage    => "M19.35,10.03C18.67,6.59 15.64,4 12,4C9.11,4 6.6,5.64 5.35,8.03C2.34,8.36 0,10.9 0,14A6,6 0 0,0 6,20H19A5,5 0 0,0 24,15C24,12.36 21.95,10.22 19.35,10.03Z",
        CategoryType.CustomFolders   => "M20,6H12L10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6M19,14H15V18H13V14H9V12H13V8H15V12H19V14Z",
        _ => "M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2Z"
    };

    public string Icon { get; set; } = "";
    public int SelectedCount => Items.Count(i => i.IsSelected);

    public void UpdateMasterCheckState()
    {
        var selected = Items.Count(i => i.IsSelected);
        if (selected == 0) MasterChecked = false;
        else if (selected == Items.Count) MasterChecked = true;
        else MasterChecked = null;
        OnPropertyChanged(nameof(SelectedCount));
    }

    public void SetAllSelected(bool value)
    {
        foreach (var item in Items)
            item.IsSelected = value;
        UpdateMasterCheckState();
    }
}
