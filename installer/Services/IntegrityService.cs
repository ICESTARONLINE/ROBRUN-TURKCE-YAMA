using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace RobRunTurkceYama.Services;

/// <summary>
/// Yama bileşenlerinin SHA-256 bütünlüğünü doğrular (değiştirilmiş worker/bundle engeli).
/// </summary>
public static class IntegrityService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public sealed class Manifest
    {
        public string Version { get; set; } = "0";
        public string Product { get; set; } = "";
        public string Publisher { get; set; } = "";
        public string Homepage { get; set; } = "";
        public Dictionary<string, string> Files { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> WriteTargets { get; set; } = [];
        public string Notes { get; set; } = "";
    }

    public static Manifest LoadManifest()
    {
        foreach (var path in CandidateManifestPaths())
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var json = File.ReadAllText(path);
            var m = JsonSerializer.Deserialize<Manifest>(json, JsonOpts);
            if (m?.Files is { Count: > 0 })
            {
                return m;
            }
        }

        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("integrity.json", StringComparison.OrdinalIgnoreCase));
        if (name is not null)
        {
            using var stream = asm.GetManifestResourceStream(name);
            if (stream is not null)
            {
                using var reader = new StreamReader(stream);
                var m = JsonSerializer.Deserialize<Manifest>(reader.ReadToEnd(), JsonOpts);
                if (m?.Files is { Count: > 0 })
                {
                    return m;
                }
            }
        }

        throw new InvalidOperationException(
            "Bütünlük listesi (integrity.json) bulunamadı. Yeniden indirip tekrar dene.");
    }

    public static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash);
    }

    public static void VerifyFile(string path, string expectedHex)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Beklenen yama dosyası yok: " + Path.GetFileName(path), path);
        }

        var actual = Sha256File(path);
        if (!actual.Equals(expectedHex.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Bütünlük hatası: {Path.GetFileName(path)} değişmiş veya bozulmuş.\n" +
                "Resmi siteden yeniden indir: https://robrunturkceyama.com/");
        }
    }

    public static Manifest VerifyPayloads(string workerPath)
    {
        var manifest = LoadManifest();
        if (manifest.Files.TryGetValue("tr_cache_worker.exe", out var workerHash))
        {
            VerifyFile(workerPath, workerHash);
        }

        var patchDir = Path.GetDirectoryName(workerPath) ?? AppContext.BaseDirectory;
        foreach (var (file, hash) in manifest.Files)
        {
            if (file.Equals("tr_cache_worker.exe", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var beside = Path.Combine(patchDir, file);
            if (File.Exists(beside))
            {
                VerifyFile(beside, hash);
                continue;
            }

            // Gömülü TR bundle extract edilmiş olabilir
            var cached = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RobRunTurkceYama",
                "PatchData",
                file);
            if (File.Exists(cached))
            {
                VerifyFile(cached, hash);
            }
        }

        return manifest;
    }

    public static string DescribeWriteScope(Manifest? manifest = null)
    {
        manifest ??= LoadManifest();
        var cache = PatchService.AddressablesCacheDir;
        return
            "Yazılacak yer:\n" +
            "• " + cache + "\n\n" +
            "Steam oyun klasöründeki dosyalar kurulumda değiştirilmez.\n" +
            "Varsa eski oyun-içi yama yedeği geri alınır.\n\n" +
            (string.IsNullOrWhiteSpace(manifest.Notes) ? "" : manifest.Notes);
    }

    private static IEnumerable<string> CandidateManifestPaths()
    {
        yield return Path.Combine(AppContext.BaseDirectory, "PatchData", "integrity.json");
        yield return Path.Combine(AppContext.BaseDirectory, "integrity.json");
        yield return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RobRunTurkceYama",
            "PatchData",
            "integrity.json");
    }
}
