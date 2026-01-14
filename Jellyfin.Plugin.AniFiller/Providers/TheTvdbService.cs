using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AniFiller.Providers;

/// <summary>
/// Service for retrieving episode information from TheTVDB API.
/// </summary>
public class TheTvdbService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TheTvdbService> _logger;
    private readonly string _apiKey;
    private string? _token;
    private DateTime _tokenExpiry = DateTime.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="TheTvdbService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{TheTvdbService}"/> interface.</param>
    /// <param name="apiKey">TheTVDB API key.</param>
    public TheTvdbService(IHttpClientFactory httpClientFactory, ILogger<TheTvdbService> logger, string apiKey)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = apiKey;
    }

    /// <summary>
    /// Gets the absolute episode number for a given series and episode.
    /// </summary>
    /// <param name="tvdbId">The TheTVDB series ID.</param>
    /// <returns>A tuple containing the series name and absolute episode number, or null if not found.</returns>
    public async Task<(string? SeriesName, int? AbsoluteEpisodeNumber)> GetAbsoluteEpisodeNumberAsync(string tvdbId)
    {
        try
        {
            await EnsureAuthenticatedAsync().ConfigureAwait(false);

            if (string.IsNullOrEmpty(_token))
            {
                _logger.LogWarning("TheTVDB: Authentication failed, no token available");
                return (null, null);
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            // Get series info
            var episodesUrl = $"https://api4.thetvdb.com/v4/episodes/{tvdbId}";
            var episodesResponse = await client.GetAsync(episodesUrl).ConfigureAwait(false);

            if (!episodesResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("TheTVDB: Failed to get series info for {TvdbId}: {StatusCode}", tvdbId, episodesResponse.StatusCode);
                return (null, null);
            }

            var episodesJson = await episodesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var episodesDoc = JsonDocument.Parse(episodesJson);
            var absoluteNumber = episodesDoc.RootElement.GetProperty("data").GetProperty("absoluteNumber").GetInt32();

            var seriesId = episodesDoc.RootElement.GetProperty("data").GetProperty("seriesId").GetInt32();

            var seriesUrl = $"https://api4.thetvdb.com/v4/series/{seriesId}/translations/eng";
            var seriesResponse = await client.GetAsync(seriesUrl).ConfigureAwait(false);

            if (!seriesResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("TheTVDB: Failed to get series info for {SeriesId}: {StatusCode}", seriesId, seriesResponse.StatusCode);
                return (null, null);
            }

            var seriesJson = await seriesResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var seriesDoc = JsonDocument.Parse(seriesJson);
            var seriesName = seriesDoc.RootElement.GetProperty("data").GetProperty("name").GetString();

            return (seriesName, absoluteNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TheTVDB: Error getting absolute episode number for {TvdbId}", tvdbId);
            return (null, null);
        }
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry)
        {
            return;
        }

        if (string.IsNullOrEmpty(_apiKey))
        {
            _logger.LogWarning("TheTVDB: No API key configured");
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var loginData = new { apikey = _apiKey };
            var content = new StringContent(
                JsonSerializer.Serialize(loginData),
                System.Text.Encoding.UTF8,
                "application/json");

            var response = await client.PostAsync("https://api4.thetvdb.com/v4/login", content).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("TheTVDB: Authentication failed with status {StatusCode}", response.StatusCode);
                return;
            }

            var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            using var doc = JsonDocument.Parse(responseJson);
            _token = doc.RootElement.GetProperty("data").GetProperty("token").GetString();
            _tokenExpiry = DateTime.UtcNow.AddHours(23); // Tokens typically last 24 hours, refresh at 23
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TheTVDB: Error during authentication");
        }
    }
}
