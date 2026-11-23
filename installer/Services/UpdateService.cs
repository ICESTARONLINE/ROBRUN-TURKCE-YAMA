using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace RobRunTurkceYama.Services;

/// <summary>
/// Resmi siteden sürüm + SHA-256 kontrolü (isteğe bağlı, HTTPS).
/// </summary>
public static class UpdateService
{
    public const string ReleaseUrl = "https://robrunturkceyama.com/release.json";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(8)
    };

    public sealed class ReleaseInfo
    {
        public string Version { get; set; } = "";
        public string Sha256 { get; set; } = "";
        public string DownloadUrl { get; set; } = "https://robrunturkceyama.com/";
        public string Notes { get; set; } = "";
    }

    public static string LocalVersion
    {
        get
        {
            var info = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info))
            {
                return info.Split('+', StringSplitOptions.TrimEntries)[0];
            }

            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v is null ? "0.0" : (v.Build > 0 ? $"{v.Major}.{v.Minor}.{v.Build}" : $"{v.Major}.{v.Minor}");
        }
    }

    public static string? LocalExeSha256()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return null;
            }

            using var stream = File.OpenRead(path);
            return Convert.ToHexString(SHA256.HashData(stream));
        }
        catch
        {
            return null;
        }
    }

    public static async Task<(bool UpdateAvailable, ReleaseInfo? Info, string? Message)> CheckAsync()
    {
        try
        {
            using var resp = await Http.GetAsync(ReleaseUrl).ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
            {
                return (false, null, null);
            }

            await using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var info = await JsonSerializer.DeserializeAsync<ReleaseInfo>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }).ConfigureAwait(false);

            if (info is null || string.IsNullOrWhiteSpace(info.Version))
            {
                return (false, null, null);
            }

            // Her zaman resmi siteye yönlendir (GitHub değil).
            info.DownloadUrl = "https://robrunturkceyama.com/";

            if (IsNewer(info.Version, LocalVersion))
            {
                return (true, info,
                    "Bu EXE güncel versiyon değil. Tekrar https://robrunturkceyama.com/ adresinden indirin.");
            }

            var localHash = LocalExeSha256();
            if (!string.IsNullOrWhiteSpace(info.Sha256) &&
                !string.IsNullOrWhiteSpace(localHash) &&
                !localHash.Equals(info.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return (true, info,
                    "Bu EXE güncel versiyon değil. Tekrar https://robrunturkceyama.com/ adresinden indirin.");
            }

            return (false, info, null);
        }
        catch
        {
            // Çevrimdışı / site yok — sessizce geç
            return (false, null, null);
        }
    }

    private static bool IsNewer(string remote, string local)
    {
        static Version Parse(string s)
        {
            var parts = s.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            int M = parts.Length > 0 && int.TryParse(parts[0], out var a) ? a : 0;
            int m = parts.Length > 1 && int.TryParse(parts[1], out var b) ? b : 0;
            int bld = parts.Length > 2 && int.TryParse(parts[2], out var c) ? c : 0;
            return new Version(M, m, bld);
        }

        try
        {
            return Parse(remote) > Parse(local);
        }
        catch
        {
            return false;
        }
    }
}
