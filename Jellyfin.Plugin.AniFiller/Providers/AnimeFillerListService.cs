using System;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AniFiller.Providers;

/// <summary>
/// Service to scrape anime filler status from AnimeFillerList.
/// </summary>
public class AnimeFillerListService
{
    private const string BaseUrl = "https://www.animefillerlist.com";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnimeFillerListService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnimeFillerListService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public AnimeFillerListService(IHttpClientFactory httpClientFactory, ILogger<AnimeFillerListService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the filler status for a specific anime episode.
    /// </summary>
    /// <param name="seriesName">The name of the anime series.</param>
    /// <param name="absoluteEpisodeNumber">The absolute episode number.</param>
    /// <returns>The filler status (e.g., "Filler", "Canon").</returns>
    public async Task<string> GetFillerStatusAsync(string seriesName, int absoluteEpisodeNumber)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            // 1. Search for the anime
            // The search URL is typically /search/node/{Query}
            var searchUrl = $"{BaseUrl}/search/node/{Uri.EscapeDataString(seriesName)}";
            var searchHtml = await client.GetStringAsync(searchUrl).ConfigureAwait(false);
            var searchDoc = new HtmlDocument();
            searchDoc.LoadHtml(searchHtml);

            // Find the best match
            // Search results are usually in <li class="search-result">...<a href="...">Title</a>
            // We need to implement fuzzy matching or just pick the first exact-ish match

            // For now, let's assume we find a link.
            // NOTE: A better approach might be to try direct URL slugification first: /shows/series-name-dashed
            // But search is safer.

            var resultNode = searchDoc.DocumentNode.SelectSingleNode("//h3[@class='title']/a");
            if (resultNode == null)
            {
                _logger.LogWarning("AnimeFillerList: No results found for {SeriesName}", seriesName);
                return "Unknown";
            }

            var animePath = resultNode.GetAttributeValue("href", string.Empty);
            if (string.IsNullOrEmpty(animePath))
            {
                return "Unknown";
            }

            var animeUrl = animePath.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? animePath : $"{BaseUrl}{animePath}"; // Usually relative
            if (!animeUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                animeUrl = $"{BaseUrl}{animePath}"; // Fix if it was just /shows/...
            }

            // 2. Get the anime page
            var animeHtml = await client.GetStringAsync(animeUrl).ConfigureAwait(false);
            var animeDoc = new HtmlDocument();
            animeDoc.LoadHtml(animeHtml);

            // 3. Find the episode status
            // The list is usually a table: <table class="EpisodeList">...
            // Rows: <tr class="... episode-row"> <td class="Number">1</td> <td class="Type">Filler</td> ...

            // XPath to find the row with the specific absolute number
            var row = animeDoc.DocumentNode.SelectSingleNode($"//table[contains(@class, 'EpisodeList')]//tr[td[@class='Number' and normalize-space(text())='{absoluteEpisodeNumber}']]");

            if (row != null)
            {
                var typeNode = row.SelectSingleNode(".//td[contains(@class, 'Type')]/span");
                if (typeNode != null)
                {
                    return typeNode.InnerText.Trim();
                }

                // Fallback if no span
                typeNode = row.SelectSingleNode(".//td[contains(@class, 'Type')]");
                if (typeNode != null)
                {
                    return typeNode.InnerText.Trim();
                }
            }

            return "Unknown";
        }
        catch (HttpRequestException ex)
        {
             _logger.LogError(ex, "Error scraping AnimeFillerList");
             return "Error";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scraping AnimeFillerList");
            return "Error";
        }
    }
}
