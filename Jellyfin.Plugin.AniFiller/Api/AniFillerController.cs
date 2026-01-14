using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.AniFiller.Providers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AniFiller.Api;

/// <summary>
/// Controller for AniFiller API.
/// </summary>
[ApiController]
[Route("AniFiller")]
public class AniFillerController : ControllerBase
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<AniFillerController> _logger;
    private readonly TheTvdbService _tvdbService;
    private readonly AnimeFillerListService _animeFillerListService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AniFillerController"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{AniFillerController}"/> interface.</param>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    public AniFillerController(
        ILibraryManager libraryManager,
        ILogger<AniFillerController> logger,
        IHttpClientFactory httpClientFactory)
    {
        _libraryManager = libraryManager;
        _logger = logger;

        var loggerFactory = LoggerFactory.Create(b => b.AddConsole());
        var tvdbLogger = loggerFactory.CreateLogger<TheTvdbService>();
        var aflLogger = loggerFactory.CreateLogger<AnimeFillerListService>();

        var apiKey = Plugin.Instance?.Configuration.TheTvdbApiKey ?? string.Empty;
        _tvdbService = new TheTvdbService(httpClientFactory, tvdbLogger, apiKey);
        _animeFillerListService = new AnimeFillerListService(httpClientFactory, aflLogger);
    }

    /// <summary>
    /// Gets the filler status for a specific episode.
    /// </summary>
    /// <param name="itemId">The Jellyfin Item ID of the episode.</param>
    /// <returns>A JSON object containing the filler status.</returns>
    [HttpGet("Episode/{itemId}")]
    public async Task<ActionResult<object>> GetEpisodeFillerStatus([FromRoute] string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return BadRequest("ItemId is required");
        }

        // 1. Get the item from LibraryManager
        var item = _libraryManager.GetItemById(Guid.Parse(itemId));

        if (item == null)
        {
             return NotFound(new { Status = "Error", Message = "Item not found" });
        }

        _logger.LogInformation("Getting all provider IDs for Item ID: {ItemId} {Providers}", itemId, item.ProviderIds);
        var tvdbId = item.GetProviderId("Tvdb");
        _logger.LogInformation("Retrieved TheTVDB ID: {TvdbId} for Item ID: {ItemId}", tvdbId, itemId);
        if (!string.IsNullOrEmpty(tvdbId))
        {
            _logger.LogInformation("Using TheTVDB for {TvdbId}", tvdbId);

            string? seriesName;

            int? absoluteNumber;
            (seriesName, absoluteNumber) = await _tvdbService.GetAbsoluteEpisodeNumberAsync(tvdbId).ConfigureAwait(false);

            _logger.LogInformation("TheTVDB returned SeriesName: {SeriesName}, AbsoluteNumber: {AbsoluteNumber} for TvdbId: {TvdbId}", seriesName, absoluteNumber, tvdbId);

            seriesName = item is Episode episode && episode.Series != null
                ? episode.Series.Name
                : seriesName;

            if (seriesName != null && absoluteNumber != null)
            {
                _logger.LogInformation("Retrieved series {SeriesName} with absolute episode number {AbsoluteNumber} from TheTVDB", seriesName, absoluteNumber);
                var status = await _animeFillerListService.GetFillerStatusAsync(seriesName, absoluteNumber ?? 0).ConfigureAwait(false);

                return Ok(new
                {
                    SeriesName = seriesName,
                    AbsoluteEpisodeNumber = absoluteNumber,
                    Status = status,
                });
            }
            else
            {
                _logger.LogWarning("Failed to retrieve absolute episode number from TheTVDB for {TvdbId}", tvdbId);
            }
        }

        return NotFound(new { Status = "Error", Message = "Item not found" });
    }
}
