using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.AniFiller.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AniFiller;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <inheritdoc />
    public override string Name => "AniFiller";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("93A5C6F2-4315-4122-9013-17D31111620D");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        ];
    }

    /// <summary>
    /// Registers the AniFiller client-side script with the JavaScript Injector plugin.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public void RegisterYourScript(Microsoft.Extensions.Logging.ILogger logger)
    {
        try
        {
            System.Reflection.Assembly? jsInjectorAssembly = System.Runtime.Loader.AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains("Jellyfin.Plugin.JavaScriptInjector", StringComparison.Ordinal) ?? false);

            if (jsInjectorAssembly != null)
            {
                Type? pluginInterfaceType = jsInjectorAssembly.GetType("Jellyfin.Plugin.JavaScriptInjector.PluginInterface");
                if (pluginInterfaceType != null)
                {
                    // Read JS Code from Embedded Resource
                    string jsCode = GetEmbeddedScript();

                    if (string.IsNullOrEmpty(jsCode))
                    {
                        logger.LogError("AniFiller: Failed to read embedded JavaScript file.");
                        return;
                    }

                    // Create the registration payload
                    var scriptRegistration = new Newtonsoft.Json.Linq.JObject
                    {
                        { "id", $"{Id}-anifiller-script" }, // Unique ID for your script
                        { "name", "AniFiller UI Injection" },
                        { "script", jsCode },
                        { "enabled", true },
                        { "requiresAuthentication", false },
                        { "pluginId", Id.ToString() },
                        { "pluginName", Name },
                        { "pluginVersion", Version.ToString() }
                    };

                    // Register the script
                    var registerResult = pluginInterfaceType.GetMethod("RegisterScript")?.Invoke(null, new object[] { scriptRegistration });

                    if (registerResult is bool success && success)
                    {
                        logger.LogInformation("Successfully registered JavaScript with JavaScript Injector plugin.");
                    }
                    else
                    {
                        logger.LogWarning("Failed to register JavaScript with JavaScript Injector plugin. RegisterScript returned false.");
                    }
                }
                else
                {
                    logger.LogWarning("Failed to register JavaScript with JavaScript Injector plugin. PluginInterface type not found.");
                }
            }
            else
            {
                logger.LogWarning("Failed to register JavaScript with JavaScript Injector plugin. JavaScript Injector plugin not found.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register JavaScript with JavaScript Injector plugin.");
        }
    }

    private string GetEmbeddedScript()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var resourceName = "Jellyfin.Plugin.AniFiller.Client.anifiller_ui.js";

        using (var stream = assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                return string.Empty;
            }

            using (var reader = new System.IO.StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
