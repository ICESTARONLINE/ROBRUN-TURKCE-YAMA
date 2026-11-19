using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace RobRunTurkceYama.Services;

public enum PatchStatus
{
    GameNotFound,
    NotInstalled,
    Installed,
    Unknown
}

/// <summary>
/// Installs Turkish via Unity Addressables cache (LocalLow), same approach as RobRun_TR.exe.
/// Does not modify Steam game files that EAC hashes.
/// </summary>
public sealed class PatchService
{
    public const int SteamAppId = 4262460;

    public const string EnBundleFileName =
        "localization-string-tables-english(unitedstates)(en-us)_assets_all.bundle";

    public const string TrBundleFileName =
        "localization-string-tables-turkish(turkey-republic)(tr)_assets_all_turkish_.bundle";

    public const string InbBundleFileName = "inbuild_assets_all.bundle";
    public const string InbTrBundleFileName = "inbuild_assets_all_turkish_.bundle";

    public const string RelativeAa =
        @"ROB RUN_Data\StreamingAssets\aa";

    public const string RelativeStandalone =
        RelativeAa + @"\StandaloneWindows64";

    public const string MarkerFileName = ".robrun-tr-cache-patch";

    private static readonly string RuntimePathPrefix =
        @"{UnityEngine.AddressableAssets.Addressables.RuntimePath}\StandaloneWindows64\";

    private static readonly string PersistentPathPrefix =
        "{UnityEngine.Application.persistentDataPath}/com.unity.addressables/";

    private static readonly (string Old, string New)[] Redirects =
    {
        (RuntimePathPrefix + EnBundleFileName, PersistentPathPrefix + TrBundleFileName),
        (RuntimePathPrefix + InbBundleFileName, PersistentPathPrefix + InbTrBundleFileName),
    };

    public string? GameRoot { get; private set; }

    public static string AddressablesCacheDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            @"AppData\LocalLow\Kefir\ROB RUN\com.unity.addressables");

    public string? MarkerPath =>
        Path.Combine(AddressablesCacheDir, MarkerFileName);

    public string? StandalonePath =>
        string.IsNullOrWhiteSpace(GameRoot)
            ? null
            : Path.Combine(GameRoot, RelativeStandalone);

    public string? AaPath =>
        string.IsNullOrWhiteSpace(GameRoot)
            ? null
            : Path.Combine(GameRoot, RelativeAa);

    public string EmbeddedTrBundlePath => ResolveEmbeddedAssetPath(TrBundleFileName);

    public bool TrySetGameRoot(string? path, out string message)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            message = "Geçersiz klasör.";
            return false;
        }

        path = path.Trim().Trim('"');
        if (File.Exists(path) &&
            path.EndsWith("ROB RUN.exe", StringComparison.OrdinalIgnoreCase))
        {
            path = Path.GetDirectoryName(path)!;
        }

        var exe = Path.Combine(path, "ROB RUN.exe");
        var en = Path.Combine(path, RelativeStandalone, EnBundleFileName);
        if (!File.Exists(exe))
        {
            message = "Bu klasörde ROB RUN.exe bulunamadı.";
            return false;
        }

        if (!File.Exists(en))
        {
            message = "Dil dosyaları eksik. Steam’de ROB RUN için “Dosyaları doğrula” yap.";
            return false;
        }

        var standalone = Path.Combine(path, RelativeStandalone);
        if (!Directory.Exists(standalone))
        {
            message = "Oyun veri klasörü (StandaloneWindows64) yok. Steam doğrulama dene.";
            return false;
        }

        GameRoot = path;
        message = HasInbuildBundle(standalone)
            ? "Oyun klasörü doğrulandı."
            : "Oyun klasörü doğrulandı (hazır Türkçe font paketi kullanılacak).";
        return true;
    }

    private static bool HasInbuildBundle(string standalone)
    {
        var direct = Path.Combine(standalone, InbBundleFileName);
        if (File.Exists(direct))
        {
            return true;
        }

        try
        {
            foreach (var f in Directory.EnumerateFiles(standalone, "inbuild_assets_all_*.bundle"))
            {
                if (f.Contains("turkish", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (new FileInfo(f).Length > 1000)
                {
                    return true;
                }
            }
        }
        catch
        {
            // ignore
        }

        return false;
    }

    public string? AutoDetectGameRoot()
    {
        foreach (var library in EnumerateSteamLibraries())
        {
            foreach (var folder in new[] { "ROB RUN", "Rob Run" })
            {
                var candidate = Path.Combine(library, "steamapps", "common", folder);
                if (File.Exists(Path.Combine(candidate, "ROB RUN.exe")) &&
                    File.Exists(Path.Combine(candidate, RelativeStandalone, EnBundleFileName)))
                {
                    GameRoot = candidate;
                    return candidate;
                }
            }
        }

        return null;
    }

    public PatchStatus GetStatus()
    {
        if (string.IsNullOrWhiteSpace(GameRoot) || StandalonePath is null)
        {
            return PatchStatus.GameNotFound;
        }

        if (!Directory.Exists(StandalonePath))
        {
            return PatchStatus.GameNotFound;
        }

        var cache = AddressablesCacheDir;
        var hasStrings = File.Exists(Path.Combine(cache, TrBundleFileName));
        var hasFonts = File.Exists(Path.Combine(cache, InbTrBundleFileName));
        if ((File.Exists(MarkerPath) || hasStrings) && hasStrings && hasFonts)
        {
            return PatchStatus.Installed;
        }

        if (hasStrings)
        {
            // Old install without font glyphs — treat as installed but incomplete
            return PatchStatus.Installed;
        }

        return PatchStatus.NotInstalled;
    }

    public void Install()
    {
        EnsureReady();
        EnsureGameNotRunning();
        RunCacheWorker(requireInstalled: true, "--game", GameRoot!);
        if (!File.Exists(Path.Combine(AddressablesCacheDir, InbTrBundleFileName)) ||
            !File.Exists(Path.Combine(AddressablesCacheDir, TrBundleFileName)))
        {
            throw new InvalidOperationException(
                "Kurulum tamamlanamadı (Türkçe font veya metin eksik). " +
                "Oyunu kapatıp Gözat ile doğru ROB RUN klasörünü seç.");
        }

        TryRestoreLegacyGameDirPatch();
    }

    public void Uninstall()
    {
        EnsureGameNotRunning();
        RunCacheWorker(requireInstalled: false, "--kaldir");
        TryRestoreLegacyGameDirPatch();
    }

    private void RunCacheWorker(bool requireInstalled, params string[] args)
    {
        var worker = ResolveWorkerPath();
        if (!File.Exists(worker))
        {
            throw new FileNotFoundException(
                "Kurucu bileşen bulunamadı (tr_cache_worker.exe).",
                worker);
        }

        // Bütünlük: worker + bundle SHA-256 (degistirilmis payload engeli)
        IntegrityService.VerifyPayloads(worker);

        // Worker yalnizca bilinen argumanlarla: --game <secilen> veya --kaldir
        foreach (var a in args)
        {
            if (a.StartsWith("-", StringComparison.Ordinal) &&
                a is not ("--game" or "--kaldir"))
            {
                throw new InvalidOperationException("Geçersiz kurucu parametresi.");
            }
        }

        if (args.Contains("--game", StringComparer.Ordinal))
        {
            var idx = Array.FindIndex(args, a => a.Equals("--game", StringComparison.Ordinal));
            if (idx < 0 || idx + 1 >= args.Length ||
                !string.Equals(args[idx + 1], GameRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Oyun yolu yalnızca senin seçtiğin klasör olabilir.");
            }
        }

        var psi = new ProcessStartInfo
        {
            FileName = worker,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(worker)!
        };
        foreach (var a in args)
        {
            psi.ArgumentList.Add(a);
        }

        psi.Environment["PYTHONUTF8"] = "1";
        psi.Environment["PYTHONIOENCODING"] = "utf-8";

        using var proc = Process.Start(psi)
                         ?? throw new InvalidOperationException("Kurucu başlatılamadı.");
        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        var trPath = Path.Combine(AddressablesCacheDir, TrBundleFileName);
        var inbPath = Path.Combine(AddressablesCacheDir, InbTrBundleFileName);
        var installedOk = File.Exists(trPath) && File.Exists(inbPath);
        var combined = stdout + "\n" + stderr;
        var okLog = combined.Contains("OK: kurulum tamam (Turkce font", StringComparison.Ordinal)
                    || combined.Contains("AddressablesCache+FontGlyphs", StringComparison.Ordinal);

        if (requireInstalled && installedOk && (okLog || File.Exists(MarkerPath!) || proc.ExitCode == 0))
        {
            try
            {
                Directory.CreateDirectory(AddressablesCacheDir);
                File.WriteAllText(
                    MarkerPath!,
                    "RobRunTurkceYama\nMode=AddressablesCache+FontGlyphs\n");
            }
            catch
            {
                // ignore
            }

            return;
        }

        if (proc.ExitCode != 0)
        {
            var detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException(FormatWorkerError(detail, proc.ExitCode));
        }

        if (requireInstalled && !installedOk)
        {
            throw new InvalidOperationException(
                "Kurulum doğrulanamadı. Gözat ile doğru ROB RUN klasörünü seçip tekrar dene.");
        }
    }

    private static string FormatWorkerError(string? detail, int exitCode)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            return $"Kurulum başarısız (kod {exitCode}).";
        }

        var text = detail.Trim();

        if (text.Contains("StandaloneWindows64 yok", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("klasoru bulunamadi", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("klasörü bulunamadı", StringComparison.OrdinalIgnoreCase))
        {
            return "Oyun klasörü hatalı veya eksik. Gözat ile ROB RUN klasörünü seç (içinde ROB RUN.exe olmalı).";
        }

        if (text.Contains("Ingilizce dil paketi yok", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("katalogda beklenen", StringComparison.OrdinalIgnoreCase))
        {
            return "Oyun dosyaları eksik/bozuk. Steam’de ROB RUN → Dosyaları doğrula, sonra yamayı tekrar kur.";
        }

        if (text.Contains("Traceback", StringComparison.OrdinalIgnoreCase))
        {
            if (text.Contains("UnityPy", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("BundleFile", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("parse_file", StringComparison.OrdinalIgnoreCase))
            {
                return "Dil/font paketi okunamadı. Steam’de dosyaları doğrula, oyunu kapatıp yeniden dene.";
            }

            var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            var last = lines.LastOrDefault(l =>
                !l.StartsWith("File ", StringComparison.Ordinal) &&
                !l.StartsWith("  File ", StringComparison.Ordinal) &&
                !l.TrimStart().StartsWith("^")) ?? "Bilinmeyen hata.";
            return "Kurulum başarısız: " + last;
        }

        // Ilerleme logunu ham gosterme
        if (Regex.IsMatch(text, @"(?im)^(oyun|metin|font|katalog|OK):"))
        {
            return "Kurulum tamamlanamadı. Gözat ile ROB RUN klasörünü tekrar seçip bir kez daha dene.";
        }

        if (text.Length > 280)
        {
            text = text[^280..];
        }

        return text;
    }

    private static string ResolveWorkerPath()
    {
        var beside = Path.Combine(AppContext.BaseDirectory, "PatchData", "tr_cache_worker.exe");
        if (File.Exists(beside))
        {
            return beside;
        }

        var flat = Path.Combine(AppContext.BaseDirectory, "tr_cache_worker.exe");
        if (File.Exists(flat))
        {
            return flat;
        }

        // Single-file extract dir may nest content
        var local = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RobRunTurkceYama",
            "tr_cache_worker.exe");
        if (File.Exists(local))
        {
            return local;
        }

        // Copy from beside publish folder if present next to source EXE after extract
        return beside;
    }

    public static bool IsGameRunning()
    {
        foreach (var p in Process.GetProcesses())
        {
            try
            {
                var name = p.ProcessName;
                if (name.Equals("ROB RUN", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("ROBRUN", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
                // ignore access-denied processes
            }
            finally
            {
                p.Dispose();
            }
        }

        return false;
    }

    private void TryRestoreLegacyGameDirPatch()
    {
        if (string.IsNullOrWhiteSpace(GameRoot))
        {
            return;
        }

        var target = Path.Combine(GameRoot, RelativeStandalone, EnBundleFileName);
        var bak = target + ".en-original.bak";
        var legacyMarker = Path.Combine(GameRoot, RelativeStandalone, ".robrun-tr-patch");
        if (File.Exists(bak) && File.Exists(target))
        {
            File.Copy(bak, target, overwrite: true);
        }

        if (File.Exists(legacyMarker))
        {
            File.Delete(legacyMarker);
        }
    }

    private void EnsureReady()
    {
        if (string.IsNullOrWhiteSpace(GameRoot) || StandalonePath is null)
        {
            throw new InvalidOperationException(
                "Önce oyun klasörünü seç. Bul veya Gözat ile ROB RUN klasörünü göster.");
        }

        if (!Directory.Exists(StandalonePath))
        {
            throw new InvalidOperationException(
                "Seçilen klasörde oyun verisi yok. Doğru ROB RUN klasörünü seç (ROB RUN.exe’nin olduğu yer).");
        }

        if (!File.Exists(Path.Combine(StandalonePath, EnBundleFileName)))
        {
            throw new InvalidOperationException(
                "Dil dosyası eksik. Steam’de ROB RUN için Dosyaları doğrula.");
        }
    }

    private static void EnsureGameNotRunning()
    {
        if (IsGameRunning())
        {
            throw new InvalidOperationException("ROB RUN açık. Kurulumdan önce oyunu kapatın.");
        }
    }

    private static void ValidateRedirectLengths()
    {
        foreach (var (oldPath, newPath) in Redirects)
        {
            if (oldPath.Length != newPath.Length)
            {
                throw new InvalidOperationException("Yönlendirme uzunlukları eşit olmalı.");
            }
        }
    }

    private static (string BinName, string HashName) ResolveCatalogNames(string aa, string sa)
    {
        var settingsPath = Path.Combine(aa, "settings.json");
        if (File.Exists(settingsPath))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(settingsPath));
                if (doc.RootElement.TryGetProperty("m_CatalogLocations", out var locs))
                {
                    foreach (var loc in locs.EnumerateArray())
                    {
                        var blob = loc.ToString();
                        if (!blob.Contains("AddressablesMainContentCatalogCacheHash", StringComparison.Ordinal))
                        {
                            continue;
                        }

                        if (!loc.TryGetProperty("m_InternalId", out var idEl))
                        {
                            continue;
                        }

                        var internalId = idEl.GetString() ?? "";
                        var baseName = Path.GetFileName(internalId.Replace('\\', '/'));
                        if (string.IsNullOrWhiteSpace(baseName))
                        {
                            continue;
                        }

                        if (baseName.EndsWith(".hash", StringComparison.OrdinalIgnoreCase))
                        {
                            return (baseName[..^5] + ".bin", baseName);
                        }

                        if (baseName.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
                        {
                            return (baseName, Path.ChangeExtension(baseName, ".hash"));
                        }

                        return (baseName + ".bin", baseName + ".hash");
                    }
                }
            }
            catch
            {
                // fall through
            }
        }

        var bin = Directory.EnumerateFiles(sa, "catalog_*.bin").OrderBy(f => f).FirstOrDefault()
                  ?? throw new FileNotFoundException("catalog_*.bin yok.", sa);
        var binName = Path.GetFileName(bin);
        return (binName, Path.ChangeExtension(binName, ".hash"));
    }

    private static int CountOccurrences(byte[] haystack, byte[] needle)
    {
        if (needle.Length == 0)
        {
            return 0;
        }

        var count = 0;
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                count++;
            }
        }

        return count;
    }

    private static byte[] ReplaceBytes(byte[] source, byte[] oldBytes, byte[] newBytes)
    {
        if (oldBytes.Length != newBytes.Length)
        {
            throw new ArgumentException("ReplaceBytes requires equal lengths.");
        }

        var result = (byte[])source.Clone();
        for (var i = 0; i <= result.Length - oldBytes.Length; i++)
        {
            var match = true;
            for (var j = 0; j < oldBytes.Length; j++)
            {
                if (result[i + j] != oldBytes[j])
                {
                    match = false;
                    break;
                }
            }

            if (!match)
            {
                continue;
            }

            Buffer.BlockCopy(newBytes, 0, result, i, newBytes.Length);
        }

        return result;
    }

    private static string ResolveEmbeddedAssetPath(string fileName)
    {
        var besideExe = Path.Combine(AppContext.BaseDirectory, "PatchData", fileName);
        if (File.Exists(besideExe))
        {
            return besideExe;
        }

        var cacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RobRunTurkceYama",
            "PatchData");
        Directory.CreateDirectory(cacheDir);
        var cached = Path.Combine(cacheDir, fileName);
        if (File.Exists(cached))
        {
            return cached;
        }

        var asm = Assembly.GetExecutingAssembly();
        var resource = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase)
                                 || n.Equals("PatchData." + fileName, StringComparison.OrdinalIgnoreCase));
        if (resource is null)
        {
            return besideExe;
        }

        using var stream = asm.GetManifestResourceStream(resource)
                           ?? throw new FileNotFoundException("Gömülü kaynak okunamadı.", resource);
        using var file = File.Create(cached);
        stream.CopyTo(file);
        return cached;
    }

    private static IEnumerable<string> EnumerateSteamLibraries()
    {
        var steamRoot = GetSteamRoot();
        if (steamRoot is null)
        {
            yield break;
        }

        yield return steamRoot;

        var vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(vdf))
        {
            yield break;
        }

        var text = File.ReadAllText(vdf);
        foreach (Match match in Regex.Matches(text, "\"path\"\\s+\"([^\"]+)\""))
        {
            var path = match.Groups[1].Value.Replace(@"\\", @"\");
            if (Directory.Exists(path))
            {
                yield return path;
            }
        }
    }

    private static string? GetSteamRoot()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var path = key?.GetValue("SteamPath") as string;
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                return path.Replace('/', '\\');
            }
        }
        catch
        {
            // ignored
        }

        foreach (var guess in new[]
                 {
                     @"C:\Program Files (x86)\Steam",
                     @"C:\Program Files\Steam",
                     @"D:\Steam",
                     @"E:\Steam"
                 })
        {
            if (Directory.Exists(guess))
            {
                return guess;
            }
        }

        return null;
    }
}
