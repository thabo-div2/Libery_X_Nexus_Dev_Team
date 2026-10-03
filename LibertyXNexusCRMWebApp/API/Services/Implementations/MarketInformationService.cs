using API.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;

namespace API.Services.Implementations
{
    /// <summary>
    /// Options for configuring the Alpha Vantage API integration.
    /// </summary>
    public sealed class AlphaVantageOptions
    {
        public string BaseUrl { get; set; } = "https://www.alphavantage.co/query";
        public string ApiKey { get; set; } = string.Empty;
    }

    //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
    /// <summary>
    /// Service for retrieving market information such as news and exchange rates from the Alpha Vantage API.
    /// </summary>
    public sealed class MarketInformationService : IMarketInformationService
    {
        private readonly HttpClient _httpClient;
        private readonly AlphaVantageOptions _options;
        private readonly ILogger<MarketInformationService> _logger;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="MarketInformationService"/> class.
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        public MarketInformationService(
            HttpClient httpClient,
            IOptions<AlphaVantageOptions> options,
            ILogger<MarketInformationService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves market information based on the provided query, including news and USD/ZAR exchange rate.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<MarketInformationResult> GetAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException(
                    "Alpha Vantage API key is not configured. Set AlphaVantage:ApiKey in user secrets or deployment settings.");
            }

            var result = new MarketInformationResult
            {
                Query = query,
                RetrievedAtUtc = DateTime.UtcNow
            };

            // The news endpoint is useful for broad market/economic questions and
            // supports topics such as financial markets, monetary policy and macro economy.
            var newsUrl = BuildUrl(
                "NEWS_SENTIMENT",
                ("topics", "financial_markets,economy_monetary,economy_macro,finance"),
                ("sort", "LATEST"),
                ("limit", "8"));

            try
            {
                using var newsResponse = await _httpClient.GetAsync(newsUrl, cancellationToken);
                newsResponse.EnsureSuccessStatusCode();

                await using var newsStream = await newsResponse.Content.ReadAsStreamAsync(cancellationToken);
                using var newsDocument = await JsonDocument.ParseAsync(newsStream, cancellationToken: cancellationToken);

                if (newsDocument.RootElement.TryGetProperty("feed", out var feed) &&
                    feed.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in feed.EnumerateArray())
                    {
                        var title = GetString(item, "title");
                        if (string.IsNullOrWhiteSpace(title))
                        {
                            continue;
                        }

                        result.News.Add(new MarketNewsItem
                        {
                            Title = title,
                            Source = GetString(item, "source"),
                            PublishedAt = GetString(item, "time_published"),
                            Url = GetString(item, "url"),
                            Summary = GetString(item, "summary"),
                            Sentiment = GetSentiment(item)
                        });
                    }
                }
                else if (newsDocument.RootElement.TryGetProperty("Note", out var note))
                {
                    _logger.LogWarning("Alpha Vantage returned a rate-limit note: {Note}", note.GetString());
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                _logger.LogError(ex, "Unable to retrieve market news from Alpha Vantage.");
            }

            // USD/ZAR is particularly relevant to the South African advisor dashboard.
            try
            {
                var fxUrl = BuildUrl(
                    "CURRENCY_EXCHANGE_RATE",
                    ("from_currency", "USD"),
                    ("to_currency", "ZAR"));

                using var fxResponse = await _httpClient.GetAsync(fxUrl, cancellationToken);
                fxResponse.EnsureSuccessStatusCode();

                await using var fxStream = await fxResponse.Content.ReadAsStreamAsync(cancellationToken);
                using var fxDocument = await JsonDocument.ParseAsync(fxStream, cancellationToken: cancellationToken);

                if (fxDocument.RootElement.TryGetProperty("Realtime Currency Exchange Rate", out var rate))
                {
                    var value = GetString(rate, "5. Exchange Rate");
                    if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                    {
                        result.UsdZarRate = parsed.ToString("0.0000", CultureInfo.InvariantCulture);
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                _logger.LogWarning(ex, "Unable to retrieve USD/ZAR exchange rate from Alpha Vantage.");
            }

            if (result.News.Count == 0 && result.UsdZarRate is null)
            {
                throw new InvalidOperationException(
                    "Current market information could not be retrieved right now. Please try again shortly.");
            }

            return result;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Builds a URL for the Alpha Vantage API request with the specified function and parameters.
        /// </summary>
        /// <param name="function"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private string BuildUrl(string function, params (string Name, string Value)[] parameters)
        {
            var query = new List<string>
        {
            $"function={Uri.EscapeDataString(function)}",
            $"apikey={Uri.EscapeDataString(_options.ApiKey)}"
        };

            query.AddRange(parameters.Select(p =>
                $"{Uri.EscapeDataString(p.Name)}={Uri.EscapeDataString(p.Value)}"));

            return $"{_options.BaseUrl}?{string.Join("&", query)}";
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets a string property from a JsonElement, returning an empty string if the property does not exist or is null.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value)
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Gets the sentiment label from a news item JsonElement, returning null if the property does not exist.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static string? GetSentiment(JsonElement item)
        {
            if (!item.TryGetProperty("overall_sentiment_label", out var label))
            {
                return null;
            }

            return label.GetString();
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
