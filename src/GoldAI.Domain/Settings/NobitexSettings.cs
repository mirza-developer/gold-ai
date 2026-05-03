namespace GoldAI.Domain.Settings;

/// <summary>
/// Configuration settings for the Nobitex cryptocurrency exchange API.
/// Bind from the "Nobitex" section of appsettings.json.
/// </summary>
public class NobitexSettings
{
    public const string SectionName = "Nobitex";

    /// <summary>
    /// Bearer token for authenticating against the Nobitex API.
    /// Obtain from https://app.nobitex.ir/settings/security
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for the Nobitex REST API.
    /// </summary>
    public string BaseUrl { get; set; } = "https://apiv2.nobitex.ir";
}
