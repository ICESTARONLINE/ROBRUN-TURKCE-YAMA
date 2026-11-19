using System.Diagnostics;
using System.Text;

namespace RobRunTurkceYama.Services;

/// <summary>
/// Chrome "ROBRUN_TR (1).exe" gibi isimlerde WinUI extract yolu bozulabiliyor.
/// Yalnızca sorunlu dosya adında, şeffaf biçimde LocalAppData'ya kopyalar
/// (gizli kalıcılık değil — ABOUT.txt ile açıklanır).
/// </summary>
internal static class LaunchGuard
{
    private const string ExeName = "ROBRUN_TR.exe";
    private const string AppFolderName = "RobRunTurkceYama";

    public static string AppDataDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolderName);

    public static string CanonicalExePath => Path.Combine(AppDataDir, ExeName);

    public static bool EnsureCanonicalProcess()
    {
        string? self;
        try
        {
            self = Environment.ProcessPath;
        }
        catch
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(self) || !File.Exists(self))
        {
            return true;
        }

        var fileName = Path.GetFileName(self);
        // Sadece (1), (2), bosluklu kopyalar — normal ROBRUN_TR.exe yerinde calisir
        if (IsCleanExeName(fileName))
        {
            WriteAboutFile();
            return true;
        }

        string canonical;
        string selfFull;
        try
        {
            Directory.CreateDirectory(AppDataDir);
            WriteAboutFile();
            canonical = Path.GetFullPath(CanonicalExePath);
            selfFull = Path.GetFullPath(self);
        }
        catch
        {
            return true;
        }

        if (string.Equals(selfFull, canonical, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            File.Copy(selfFull, canonical, overwrite: true);
        }
        catch (IOException)
        {
            if (!File.Exists(canonical))
            {
                return true;
            }
        }
        catch (UnauthorizedAccessException)
        {
            if (!File.Exists(canonical))
            {
                return true;
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = canonical,
                UseShellExecute = true,
                WorkingDirectory = AppDataDir
            });
        }
        catch
        {
            return true;
        }

        return false;
    }

    private static bool IsCleanExeName(string fileName) =>
        fileName.Equals(ExeName, StringComparison.OrdinalIgnoreCase);

    private static void WriteAboutFile()
    {
        try
        {
            Directory.CreateDirectory(AppDataDir);
            var about = Path.Combine(AppDataDir, "ABOUT.txt");
            var text = new StringBuilder()
                .AppendLine("ROB RUN Türkçe Yama — ICESTAR")
                .AppendLine("Site: https://robrunturkceyama.com/")
                .AppendLine()
                .AppendLine("Bu klasör uygulama verisi içindir (yama önbelleği / kararlı EXE yolu).")
                .AppendLine("Steam oyun dosyalarına yazılmaz.")
                .AppendLine("Kaldırmak için uygulamadaki «Yamayı Kaldır» veya bu klasörü silebilirsin.")
                .AppendLine()
                .AppendLine("Sürüm: " + UpdateService.LocalVersion)
                .ToString();
            File.WriteAllText(about, text, Encoding.UTF8);
        }
        catch
        {
            // ignore
        }
    }
}
