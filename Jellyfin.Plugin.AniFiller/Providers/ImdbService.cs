using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AniFiller.Providers;

/// <summary>
/// Service to scrape IMDb for episode information.
/// </summary>
public class ImdbService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ImdbService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImdbService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public ImdbService(IHttpClientFactory httpClientFactory, ILogger<ImdbService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the absolute episode number and series name for a given IMDb Episode ID.
    /// </summary>
    /// <param name="imdbEpisodeId">The IMDb ID of the episode.</param>
    /// <returns>A tuple containing the series name and the absolute episode index.</returns>
    public async Task<int?> GetAbsoluteEpisodeNumberAsync(string imdbEpisodeId)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            // 1. Get the episode page
            var episodeUrl = $"https://www.imdb.com/title/{imdbEpisodeId}";
            var episodeHtml = await client.GetStringAsync(episodeUrl).ConfigureAwait(false);
            var episodeDoc = new HtmlDocument();
            episodeDoc.LoadHtml(episodeHtml);

            // Variables to hold extracted data
            string seriesName = string.Empty;
            int episodeNum = 0;

            var seasonEpisodeNode = episodeDoc.DocumentNode.SelectSingleNode("//div[@data-testid='hero-subnav-bar-season-episode-numbers-section']");
            _logger.LogInformation("Season Episode Node: {SeasonEpisodeNode}", seasonEpisodeNode?.InnerText);
            if (seasonEpisodeNode != null && !string.IsNullOrWhiteSpace(seasonEpisodeNode.InnerText))
            {
                try
                {
                    var seasonEpisodeParts = seasonEpisodeNode.InnerText.Split(".");
                    if (seasonEpisodeParts.Length >= 2)
                    {
                        if (int.TryParse(seasonEpisodeParts[1].Replace("E", string.Empty, StringComparison.OrdinalIgnoreCase), out int eIdx))
                        {
                            episodeNum = eIdx;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse JSON-LD for episode {ImdbId}", imdbEpisodeId);
                }
            }

            return episodeNum;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error scraping IMDb");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAbsoluteEpisodeNumberAsync");
            return null;
        }
    }
}
