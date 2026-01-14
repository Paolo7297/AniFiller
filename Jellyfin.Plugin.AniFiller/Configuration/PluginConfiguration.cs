using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AniFiller.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        // Set default options here
        TheTvdbApiKey = string.Empty;
    }

    /// <summary>
    /// Gets or sets the TheTVDB API key.
    /// </summary>
    public string TheTvdbApiKey { get; set; }
}
