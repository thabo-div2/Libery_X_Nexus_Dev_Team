using System.Net.Http.Json;

namespace frontend.Services;

public sealed record MarketInformationResult(
    string Query,
    DateTime RetrievedAtUtc,
    string DataFreshness,
    string? UsdZarRate,
    List<MarketNewsItem> News);

public sealed record MarketNewsItem(
    string Title,
    string Source,
    string PublishedAt,
    string Url,
    string Summary,
    string? Sentiment);

public class MarketInformationService
{
    private readonly HttpClient _http;

    public MarketInformationService(HttpClient http)
    {
        _http = http;
    }

    public async Task<(MarketInformationResult? Result, string? Error)> GetAsync(string query)
    {
        try
        {
            var response = await _http.GetAsync($"MarketInformation?q={Uri.EscapeDataString(query)}");

            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content.ReadAsStringAsync();
                return (null, string.IsNullOrWhiteSpace(message)
                    ? $"The market information service returned status {(int)response.StatusCode}."
                    : message.Trim('"'));
            }

            var result = await response.Content.ReadFromJsonAsync<MarketInformationResult>();
            return (result, null);
        }
        catch (HttpRequestException)
        {
            return (null, "Can't reach the market information service. Make sure the API project is running.");
        }
        catch (Exception ex)
        {
            return (null, $"Something went wrong while retrieving market information: {ex.Message}");
        }
    }
}
