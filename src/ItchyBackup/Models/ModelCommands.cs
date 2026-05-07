using CommunityToolkit.Mvvm.Input;

namespace ItchyBackup.Models;

public partial class BackupItem
{
    private RelayCommand? _toggleSelectCommand;
    public RelayCommand ToggleSelectCommand =>
        _toggleSelectCommand ??= new RelayCommand(() => IsSelected = !IsSelected);
}

public partial class BackupCategory
{
    private RelayCommand? _toggleExpandCommand;
    public RelayCommand ToggleExpandCommand =>
        _toggleExpandCommand ??= new RelayCommand(() => IsExpanded = !IsExpanded);

    private RelayCommand<bool>? _toggleMasterCommand;
    public RelayCommand<bool> ToggleMasterCommand =>
        _toggleMasterCommand ??= new RelayCommand<bool>(v => SetAllSelected(v));
}
