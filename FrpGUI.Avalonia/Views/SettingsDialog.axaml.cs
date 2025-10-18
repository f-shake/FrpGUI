using FrpGUI.Avalonia.ViewModels;

using FzLib.Avalonia.Dialogs;

namespace FrpGUI.Avalonia.Views;

public partial class SettingsDialog : DialogHost
{
    public SettingsDialog()
    {
        InitializeComponent();
    }

    protected override async void OnCloseButtonClick()
    {
        IsCloseButtonEnabled = false;
        if (await ((SettingViewModel)DataContext).TryCloseAsync())
        {
          Close();  
        }
        else
        {
            IsCloseButtonEnabled = true;
        }
    }
}