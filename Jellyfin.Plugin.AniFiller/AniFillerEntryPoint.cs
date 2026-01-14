using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AniFiller;

/// <summary>
/// Entry point for the AniFiller plugin to perform startup tasks.
/// </summary>
public class AniFillerEntryPoint : IHostedService
{
    private readonly ILogger<AniFillerEntryPoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AniFillerEntryPoint"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public AniFillerEntryPoint(ILogger<AniFillerEntryPoint> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AniFillerEntryPoint Starting...");

        // Attempt to register the script
        if (Plugin.Instance != null)
        {
            Plugin.Instance.RegisterYourScript(_logger);
        }
        else
        {
            _logger.LogWarning("AniFiller Plugin Instance is null during startup.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
