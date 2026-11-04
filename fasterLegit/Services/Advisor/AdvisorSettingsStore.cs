using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using fasterLegit.Domain.Advisor;

namespace fasterLegit.Services.Advisor;

public sealed class AdvisorSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };
    private readonly string _directory;

    public AdvisorSettingsStore()
        : this(Path.Combine(AppContext.BaseDirectory, "UserData", "Advisor")) { }

    public AdvisorSettingsStore(string directory) => _directory = directory;

    public string DirectoryPath => _directory;

    public AdvisorSettings Load()
    {
        var path = Path.Combine(_directory, "settings.json");
        if (!File.Exists(path))
            return new AdvisorSettings();
        return JsonSerializer.Deserialize<AdvisorSettings>(File.ReadAllText(path), JsonOptions)
            ?? new AdvisorSettings();
    }

    public void Save(AdvisorSettings settings, string? newKey = null)
    {
        Validate(settings, requireModel: false);
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporary, path, overwrite: true);
        if (!string.IsNullOrWhiteSpace(newKey))
        {
            var protectedKey = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(newKey.Trim()),
                null,
                DataProtectionScope.CurrentUser
            );
            File.WriteAllBytes(Path.Combine(_directory, "key.bin"), protectedKey);
        }
    }

    public bool HasKey => File.Exists(Path.Combine(_directory, "key.bin"));

    public string ReadKey()
    {
        if (!HasKey)
            return "";
        return Encoding.UTF8.GetString(
            ProtectedData.Unprotect(
                File.ReadAllBytes(Path.Combine(_directory, "key.bin")),
                null,
                DataProtectionScope.CurrentUser
            )
        );
    }

    public void ClearKey()
    {
        var path = Path.Combine(_directory, "key.bin");
        if (File.Exists(path))
            File.Delete(path);
    }

    public static Uri Validate(AdvisorSettings settings, bool requireModel = true)
    {
        if (!Enum.IsDefined(settings.Provider) || settings.SampleSeconds is < 5 or > 60)
            throw new ArgumentException("AI.Error.Settings");
        if (
            requireModel
            && (string.IsNullOrWhiteSpace(settings.Model) || settings.Model.Length > 120)
        )
            throw new ArgumentException("AI.Error.Model");
        if (!requireModel && string.IsNullOrWhiteSpace(settings.Endpoint))
            return new Uri("http://127.0.0.1:11434/v1/");
        if (
            !Uri.TryCreate(settings.Endpoint.TrimEnd('/') + "/", UriKind.Absolute, out var endpoint)
            || !string.IsNullOrEmpty(endpoint.UserInfo)
            || !string.IsNullOrEmpty(endpoint.Query)
            || !string.IsNullOrEmpty(endpoint.Fragment)
            || (endpoint.Scheme != "https" && !(endpoint.Scheme == "http" && endpoint.IsLoopback))
        )
            throw new ArgumentException("AI.Error.Endpoint");
        if (
            settings.Provider == AdvisorProvider.Local
            && (
                !endpoint.IsLoopback
                || settings.Model.Contains(":cloud", StringComparison.OrdinalIgnoreCase)
                || settings.Model.EndsWith("-cloud", StringComparison.OrdinalIgnoreCase)
            )
        )
            throw new ArgumentException("AI.Error.LocalOnly");
        return endpoint;
    }
}
