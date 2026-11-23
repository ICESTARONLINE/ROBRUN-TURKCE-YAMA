using Windows.Storage.Pickers;
using WinRT.Interop;
using RobRunTurkceYama.Services;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;

namespace RobRunTurkceYama.Views;

public partial class MainPage : Page
{
    private readonly PatchService _patch = new();

    public MainPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            SetupCustomTitleBar();
            TryAutoDetect();
            RefreshUi();
        };
    }

    private void SetupCustomTitleBar()
    {
        var window = App.MainWindow;
        window.ExtendsContentIntoTitleBar = true;
        window.SetTitleBar(AppTitleBar);

        var titleBar = window.AppWindow.TitleBar;
        titleBar.ButtonBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        titleBar.ButtonInactiveBackgroundColor = Windows.UI.Color.FromArgb(0, 0, 0, 0);
        titleBar.ButtonForegroundColor = Windows.UI.Color.FromArgb(255, 240, 240, 240);
        titleBar.ButtonInactiveForegroundColor = Windows.UI.Color.FromArgb(255, 160, 160, 160);
        titleBar.ButtonHoverBackgroundColor = Windows.UI.Color.FromArgb(40, 255, 255, 255);
        titleBar.ButtonPressedBackgroundColor = Windows.UI.Color.FromArgb(70, 255, 255, 255);
    }

    private void OnDetectClicked(object sender, RoutedEventArgs e)
    {
        TryAutoDetect();
        RefreshUi();
    }

    private async void OnBrowseClicked(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));

            var folder = await picker.PickSingleFolderAsync();
            if (folder is null)
            {
                return;
            }

            if (_patch.TrySetGameRoot(folder.Path, out var message))
            {
                PathBox.Text = _patch.GameRoot;
                DetailText.Text = message;
            }
            else
            {
                DetailText.Text = message;
            }

            RefreshUi();
        }
        catch (Exception ex)
        {
            DetailText.Text = "Gözat hatası: " + ex.Message;
        }
    }

    private async void OnInstallClicked(object sender, RoutedEventArgs e)
    {
        var updating = _patch.GetStatus() == PatchStatus.Installed;
        await RunActionAsync(
            updating ? "Yama güncelleme" : "Kurulum",
            () => _patch.Install(),
            onSuccess: updating ? ToastService.ShowUpdateSuccess : ToastService.ShowInstallSuccess,
            successMessage: updating
                ? "Türkçe yama güncellendi. Steam’den ROB RUN’u açabilirsin."
                : "Türkçe yama kuruldu. Steam’den ROB RUN’u açabilirsin.");
    }

    private async void OnUninstallClicked(object sender, RoutedEventArgs e)
    {
        await RunActionAsync(
            "Yama kaldırma",
            () => _patch.Uninstall(),
            onSuccess: ToastService.ShowUninstallSuccess,
            successMessage: "Yama kaldırıldı.");
    }

    private void OnRefreshClicked(object sender, RoutedEventArgs e) => RefreshUi();

    private async Task RunActionAsync(
        string title,
        Action action,
        Action? onSuccess = null,
        string? successMessage = null)
    {
        SetBusy(true);
        try
        {
            await Task.Run(action);
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            var dialog = new ContentDialog
            {
                Title = title + " başarısız",
                Content = ex.Message,
                CloseButtonText = "Tamam",
                XamlRoot = XamlRoot
            };
            _ = await dialog.ShowAsync();
        }
        finally
        {
            SetBusy(false);
            RefreshUi();
        }
    }

    private void TryAutoDetect()
    {
        var found = _patch.AutoDetectGameRoot();
        if (found is null)
        {
            PathBox.Text = string.Empty;
            return;
        }

        PathBox.Text = found;
    }

    private void SetStatusBadge(string text, string glyph, byte r, byte g, byte b)
    {
        StatusText.Text = text;
        StatusIcon.Glyph = glyph;
        var color = Windows.UI.Color.FromArgb(255, r, g, b);
        var brush = new SolidColorBrush(color);
        StatusText.Foreground = brush;
        StatusIcon.Foreground = brush;
        StatusBadge.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(40, r, g, b));
    }

    private void RefreshUi()
    {
        var status = _patch.GetStatus();
        switch (status)
        {
            case PatchStatus.Installed:
                SetStatusBadge("Türkçe yama kurulu", "\uE73E", 76, 175, 80);
                DetailText.Text = "Oyunu Steam’den açabilirsin.";
                InstallButton.IsEnabled = true;
                InstallButtonText.Text = "Yamayı Güncelle";
                InstallButtonIcon.Glyph = "\uE72C";
                UninstallButton.IsEnabled = true;
                break;
            case PatchStatus.NotInstalled:
                SetStatusBadge("Yama kurulu değil", "\uE7BA", 210, 153, 34);
                DetailText.Text = "Kur’a basarak Türkçe yamayı yükle.";
                InstallButton.IsEnabled = true;
                InstallButtonText.Text = "Türkçe Yamayı Kur";
                InstallButtonIcon.Glyph = "\uE896";
                UninstallButton.IsEnabled = false;
                break;
            case PatchStatus.GameNotFound:
                SetStatusBadge("Oyun bulunamadı", "\uE783", 207, 70, 70);
                DetailText.Text = "Gözat ile ROB RUN klasörünü seç.";
                InstallButton.IsEnabled = false;
                InstallButtonText.Text = "Türkçe Yamayı Kur";
                InstallButtonIcon.Glyph = "\uE896";
                UninstallButton.IsEnabled = false;
                break;
            default:
                SetStatusBadge("Durum belirsiz", "\uE9CE", 160, 160, 160);
                DetailText.Text = "";
                InstallButton.IsEnabled = false;
                UninstallButton.IsEnabled = false;
                break;
        }

        if (PatchService.IsGameRunning())
        {
            SetStatusBadge("Oyun açık", "\uE7BA", 229, 57, 53);
            DetailText.Text = "ROB RUN açık — önce kapat, sonra kur ya da güncelle.";
            InstallButton.IsEnabled = false;
            UninstallButton.IsEnabled = false;
        }
    }

    private void SetBusy(bool busy)
    {
        InstallButton.IsEnabled = !busy;
        UninstallButton.IsEnabled = !busy;
        DetectButton.IsEnabled = !busy;
        BrowseButton.IsEnabled = !busy;
        RefreshButton.IsEnabled = !busy;
        if (busy)
        {
            SetStatusBadge("İşleniyor…", "\uE895", 229, 57, 53);
            DetailText.Text = "Lütfen bekle — kurulum biraz sürebilir.";
        }
    }
}
