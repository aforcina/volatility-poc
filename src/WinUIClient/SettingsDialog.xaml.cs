using Microsoft.UI.Xaml.Controls;
using WinUIClient.ViewModels;

namespace WinUIClient;

public sealed partial class SettingsDialog : ContentDialog
{
    private readonly MainViewModel _vm;
    public SettingsDialog(MainViewModel vm)
    {
        this.InitializeComponent();
        _vm = vm;

        ChartModeCombo.Items.Add("Web (WebView2)");
        ChartModeCombo.Items.Add("Native (Win2D)");
        ChartModeCombo.SelectedIndex = _vm.SelectedChartModeIndex;
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Save selection back to ViewModel
        _vm.SelectedChartModeIndex = ChartModeCombo.SelectedIndex;
    }

    private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // do nothing; dialog closes automatically
    }
}
