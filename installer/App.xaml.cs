using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Navigation;
using RobRunTurkceYama.Services;
using Windows.Graphics;

namespace RobRunTurkceYama;

public partial class App : Application
{
    public static Window MainWindow { get; private set; } = null!;

    public App()
    {
        // ROBRUN_TR (1).exe vb. → LocalAppData\RobRunTurkceYama\ROBRUN_TR.exe
        if (!LaunchGuard.EnsureCanonicalProcess())
        {
            Environment.Exit(0);
            return;
        }

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        MainWindow = new Window
        {
            Title = "ROB RUN Türkçe Yama"
        };

        // Alt boşluk yok; içeriğe sıkı yükseklik
        MainWindow.AppWindow.Resize(new SizeInt32(1040, 318));
        if (MainWindow.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
        }

        // Hide default white title bar; page provides dark custom title bar.
        MainWindow.ExtendsContentIntoTitleBar = true;

        var rootFrame = new Frame();
        rootFrame.NavigationFailed += OnNavigationFailed;
        MainWindow.Content = rootFrame;
        _ = rootFrame.Navigate(typeof(MainPage), e.Arguments);
        MainWindow.Activate();
    }

    private static void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
    }
}
