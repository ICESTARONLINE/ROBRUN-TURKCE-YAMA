using Microsoft.Toolkit.Uwp.Notifications;

namespace RobRunTurkceYama.Services;

public static class ToastService
{
    public static void ShowInstallSuccess()
    {
        Show(title: "Kurulum tamamlandı", body: "Türkçe yama hazır.");
    }

    public static void ShowUpdateSuccess()
    {
        Show(title: "Güncelleme tamam", body: "Türkçe yama hazır.");
    }

    public static void ShowUninstallSuccess()
    {
        Show(title: "Yama kaldırıldı", body: "Orijinal dil geri geldi.");
    }

    private static void Show(string title, string body)
    {
        try
        {
            var iconPath = ResolveIconPath();
            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(body)
                .SetToastDuration(ToastDuration.Short);

            if (!string.IsNullOrWhiteSpace(iconPath) && File.Exists(iconPath))
            {
                builder.AddAppLogoOverride(
                    new Uri(Path.GetFullPath(iconPath)),
                    ToastGenericAppLogoCrop.Default);
            }

            builder.Show(toast =>
            {
                toast.ExpirationTime = DateTimeOffset.Now.AddSeconds(12);
                toast.Tag = "robrun-tr-patch";
                toast.Group = "robrun-tr";
            });
        }
        catch
        {
            // Toast optional — don't break install flow.
        }
    }

    private static string? ResolveIconPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RobRunTurkceYama", "AppIcon.png")
        };

        foreach (var path in candidates.Where(File.Exists))
        {
            return path;
        }

        // Extract embedded/content icon next to cached patch if needed
        try
        {
            var embedded = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png");
            var cache = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RobRunTurkceYama",
                "AppIcon.png");
            if (File.Exists(embedded) && !File.Exists(cache))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cache)!);
                File.Copy(embedded, cache, overwrite: true);
                return cache;
            }
        }
        catch
        {
            // ignored
        }

        return null;
    }
}
