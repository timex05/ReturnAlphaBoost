using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ReturnAlphaBoostConfig
{
    static readonly HttpClient Http = new();
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Dictionary<string, AlphaProfile> Profiles { get; set; } = new();

    public static ReturnAlphaBoostConfig Load()
    {
        var baseDirectory = AppContext.BaseDirectory;

        // In development: try loading local config first
        var localConfigPath = Path.Combine(baseDirectory, "returnalphaboost.local.json");
        if (File.Exists(localConfigPath))
        {
            try
            {
                var json = File.ReadAllText(localConfigPath);
                var config = JsonSerializer.Deserialize<ReturnAlphaBoostConfig>(json, JsonOptions);
                return Normalize(config) ?? throw new InvalidOperationException("The local configuration is empty or invalid.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Unable to load local configuration from {localConfigPath}", ex);
            }
        }

        // Fall back to remote GitHub URL
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
    public string Type { get; set; } = "General";

    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("text_short")]
    public string TextShort { get; set; } = string.Empty;

    public List<FileMapping> Mappings { get; set; } = new();

    public AlphaProfile Normalize()
    {
        if (string.IsNullOrWhiteSpace(Type))
        {
            Type = "General";
        }

        Text = Text?.Trim() ?? string.Empty;
        TextShort = TextShort?.Trim() ?? string.Empty;

        Mappings = Mappings.Where(mapping => mapping != null && mapping.IsValid()).ToList();
        return this;
    }

    public List<FileMapping> GetValidMappings()
    {
        return Mappings.Where(mapping => mapping.IsValid()).ToList();
    }

    public string GetDisplayText(string fallback)
    {
        if (!string.IsNullOrWhiteSpace(TextShort))
        {
            return TextShort;
        }

        if (!string.IsNullOrWhiteSpace(Text))
        {
            return Text;
        }

        return fallback;
    }

    public string GetDescriptionText(string fallback)
    {
        if (!string.IsNullOrWhiteSpace(Text))
        {
            return Text;
        }

        return fallback;
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