using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

public sealed class ReturnAlphaBoostConfig
{
    static readonly HttpClient Http = new();
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Dictionary<string, AlphaProfile> Profiles { get; set; } = new();

    public static ReturnAlphaBoostConfig Load(string baseDirectory)
    {
        // Development/local override: if a file named returnalphaboost.local.json exists, use it.
        var localDevPath = Path.Combine(baseDirectory, "returnalphaboost.local.json");
        if (File.Exists(localDevPath))
        {
            var json = File.ReadAllText(localDevPath);
            var config = JsonSerializer.Deserialize<ReturnAlphaBoostConfig>(json, JsonOptions);
            return Normalize(config) ?? throw new InvalidOperationException("The local configuration is empty or invalid.");
        }

        // Environment variable to force using the bundled config next to the exe during development
        var useLocal = Environment.GetEnvironmentVariable("RAB_USE_LOCAL_CONFIG");
        if (!string.IsNullOrEmpty(useLocal) && (useLocal == "1" || useLocal.Equals("true", StringComparison.OrdinalIgnoreCase)))
        {
            var bundledPath = Path.Combine(baseDirectory, "returnalphaboost.config.json");
            if (!File.Exists(bundledPath))
            {
                throw new InvalidOperationException("RAB_USE_LOCAL_CONFIG is set but no bundled config file was found.");
            }

            var json = File.ReadAllText(bundledPath);
            var config = JsonSerializer.Deserialize<ReturnAlphaBoostConfig>(json, JsonOptions);
            return Normalize(config) ?? throw new InvalidOperationException("The bundled local configuration is empty or invalid.");
        }

        // Default behavior: load from remote GitHub URL. Throw if unreachable or invalid.
        try
        {
            var json = Http.GetStringAsync(RemoteConfigUrl).GetAwaiter().GetResult();
            var config = JsonSerializer.Deserialize<ReturnAlphaBoostConfig>(json, JsonOptions);
            return Normalize(config) ?? throw new InvalidOperationException("The remote configuration is empty or invalid.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unable to load configuration from GitHub URL: {RemoteConfigUrl}", ex);
        }
    }

    public AlphaProfile? GetProfile(string name)
    {
        return Profiles.FirstOrDefault(profile => string.Equals(profile.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
    }

    static ReturnAlphaBoostConfig? Normalize(ReturnAlphaBoostConfig? config)
    {
        if (config == null)
        {
            return null;
        }

        var normalized = new ReturnAlphaBoostConfig();

        foreach (var profile in config.Profiles)
        {
            normalized.Profiles[profile.Key] = profile.Value?.Normalize() ?? new AlphaProfile();
        }

        return normalized;
    }

    public static ReturnAlphaBoostConfig CreateDefault()
    {
        return new ReturnAlphaBoostConfig();
    }

    public const string RemoteConfigUrl = "https://raw.githubusercontent.com/timex05/ReturnAlphaBoost/main/returnalphaboost.config.json";
}

public sealed class AlphaProfile
{
    public string Type { get; set; } = "local";

    public string SourceRoot { get; set; } = "TAGame\\CookedPCConsole";

    public List<FileMapping> Mappings { get; set; } = new();

    public AlphaProfile Normalize()
    {
        if (string.IsNullOrWhiteSpace(Type))
        {
            Type = "local";
        }

        if (string.IsNullOrWhiteSpace(SourceRoot))
        {
            SourceRoot = "TAGame\\CookedPCConsole";
        }

        Mappings = Mappings.Where(mapping => mapping != null && mapping.IsValid()).ToList();
        return this;
    }

    public List<FileMapping> GetValidMappings()
    {
        return Mappings.Where(mapping => mapping.IsValid()).ToList();
    }
}

public sealed class FileMapping
{
    public string Source { get; set; } = string.Empty;

    public string Target { get; set; } = string.Empty;

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Source) && !string.IsNullOrWhiteSpace(Target);
    }
}