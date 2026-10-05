using API.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
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
    ///
    /// PERFORMANCE NOTES (read before changing the fetch logic):
    /// 1. The news and FX requests are independent of each other and now run
    ///    CONCURRENTLY via Task.WhenAll instead of one after another -
    ///    roughly halves latency on every call that actually reaches
    ///    Alpha Vantage, since neither request depends on the other's result.
    /// 2. The "query" parameter does not affect which Alpha Vantage data is
    ///    fetched - topics and the currency pair are fixed, not derived from
    ///    it. That means the underlying data is identical for every caller,
    ///    which makes a single shared cache entry valid here - it is NOT a
    ///    per-user or per-query cache. If query-specific fetching is added
    ///    later (e.g. a specific stock symbol), the cache key below needs to
    ///    incorporate that, or different queries will incorrectly share
    ///    cached results.
    /// 3. Cache "freshness" window is 3 minutes, but this is
    ///    stale-while-revalidate, not a hard expiry: once ANY data has been
    ///    fetched once, it never falls out of the cache entirely. A request
    ///    arriving after the 3-minute window gets that slightly-old data
    ///    back IMMEDIATELY, while a background task quietly refreshes it for
    ///    next time. Net effect: only the very first call the app ever
    ///    makes (empty cache, nothing to serve yet) blocks on Alpha Vantage.
    ///    Every request after that returns near-instantly, and "current
    ///    market news" is still never more than ~3 minutes stale in
    ///    practice. _refreshGate ensures only one background refresh runs
    ///    at a time, so a burst of requests in the stale window doesn't fire
    ///    off a dozen redundant calls and blow through Alpha Vantage's
    ///    5-calls/minute free-tier limit.
    /// </summary>
    public sealed class MarketInformationService : IMarketInformationService
    {
        private const string CacheKey = "AlphaVantage:MarketInformation";
        private static readonly TimeSpan FreshnessWindow = TimeSpan.FromMinutes(3);

        // Not disposed - this service is registered via AddHttpClient, which
        // makes it transient, but a static gate is intentional here: the
        // "only one background refresh at a time" guarantee needs to hold
        // across ALL instances, not just within one.
        private static readonly SemaphoreSlim _refreshGate = new(1, 1);

        private readonly HttpClient _httpClient;
        private readonly AlphaVantageOptions _options;
        private readonly IMemoryCache _cache;
        private readonly ILogger<MarketInformationService> _logger;

        public MarketInformationService(
            HttpClient httpClient,
            IOptions<AlphaVantageOptions> options,
            IMemoryCache cache,
            ILogger<MarketInformationService> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _cache = cache;
            _logger = logger;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves market information based on the provided query, including news and USD/ZAR exchange rate.
        /// Stale-while-revalidate: returns cached data immediately if any
        /// exists (even if past the freshness window), refreshing it in the
        /// background rather than making the caller wait. Only ever blocks
        /// on Alpha Vantage when the cache is completely empty.
        /// </summary>
        public async Task<MarketInformationResult> GetAsync(
            string query,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException(
                    "Alpha Vantage API key is not configured. Set AlphaVantage:ApiKey in user secrets or deployment settings.");
            }

            if (_cache.TryGetValue(CacheKey, out CachedMarketData? cached) && cached is not null)
            {
                var age = DateTime.UtcNow - cached.RetrievedAtUtc;
                if (age > FreshnessWindow)
                {
                    _logger.LogDebug(
                        "Serving stale market information ({AgeSeconds}s old) and refreshing in the background.",
                        (int)age.TotalSeconds);
                    TriggerBackgroundRefresh();
                }
                else
                {
                    _logger.LogDebug("Serving fresh market information from cache (fetched {FetchedAt}).", cached.RetrievedAtUtc);
                }

                return BuildResult(query, cached);
            }

            // Nothing cached at all yet - this is the one and only path that
            // actually blocks the caller on Alpha Vantage.
            var freshData = await FetchFromAlphaVantageAsync(cancellationToken);
            _cache.Set(CacheKey, freshData);
            return BuildResult(query, freshData);
        }

        /// <summary>
        /// Fire-and-forget refresh, guarded so only one runs at a time. Uses
        /// CancellationToken.None deliberately - this keeps running even
        /// after the triggering HTTP request completes and its own
        /// cancellation token is disposed; it's refreshing shared cache for
        /// future callers, not serving the current one.
        /// </summary>
        private void TriggerBackgroundRefresh()
        {
            if (!_refreshGate.Wait(0))
            {
                // A refresh is already in flight from an earlier request in
                // this same stale window - don't start a second one.
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    var freshData = await FetchFromAlphaVantageAsync(CancellationToken.None);
                    _cache.Set(CacheKey, freshData);
                    _logger.LogDebug("Background market information refresh completed.");
                }
                catch (Exception ex)
                {
                    // Deliberately swallowed beyond logging - this is a
                    // background best-effort refresh. The stale cached data
                    // stays in place and gets served/retried on the next
                    // request; there is no caller waiting on this result to
                    // propagate a failure to.
                    _logger.LogWarning(ex, "Background market information refresh failed; stale data remains cached.");
                }
                finally
                {
                    _refreshGate.Release();
                }
            });
        }

        private async Task<CachedMarketData> FetchFromAlphaVantageAsync(CancellationToken cancellationToken)
        {
            // Both requests are independent - fire them together rather than
            // awaiting one fully before starting the other. Each keeps its
            // own try/catch internally (see FetchNewsAsync/FetchFxRateAsync)
            // so one failing doesn't prevent the other's result from being used.
            var newsTask = FetchNewsAsync(cancellationToken);
            var fxTask = FetchFxRateAsync(cancellationToken);
            await Task.WhenAll(newsTask, fxTask);

            var news = await newsTask;
            var usdZarRate = await fxTask;

            if (news.Count == 0 && usdZarRate is null)
            {
                throw new InvalidOperationException(
                    "Current market information could not be retrieved right now. Please try again shortly.");
            }

            return new CachedMarketData
            {
                News = news,
                UsdZarRate = usdZarRate,
                RetrievedAtUtc = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Wraps cached (or freshly fetched) data into a result for this
        /// specific call. RetrievedAtUtc reflects when the underlying data
        /// was actually fetched from Alpha Vantage, not "now" - serving a
        /// cached response should never claim to be fresher than it is.
        /// </summary>
        private static MarketInformationResult BuildResult(string query, CachedMarketData data) => new()
        {
            Query = query,
            RetrievedAtUtc = data.RetrievedAtUtc,
            UsdZarRate = data.UsdZarRate,
            News = data.News
        };

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        private async Task<List<MarketNewsItem>> FetchNewsAsync(CancellationToken cancellationToken)
        {
            var news = new List<MarketNewsItem>();

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

                        news.Add(new MarketNewsItem
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

            return news;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        private async Task<string?> FetchFxRateAsync(CancellationToken cancellationToken)
        {
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
                        return parsed.ToString("0.0000", CultureInfo.InvariantCulture);
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                _logger.LogWarning(ex, "Unable to retrieve USD/ZAR exchange rate from Alpha Vantage.");
            }

            return null;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
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

        private static string GetString(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var value)
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }

        private static string? GetSentiment(JsonElement item)
        {
            if (!item.TryGetProperty("overall_sentiment_label", out var label))
            {
                return null;
            }

            return label.GetString();
        }

        /// <summary>Internal cache shape - deliberately not MarketInformationResult,
        /// since Query is per-caller and shouldn't be part of the cached/shared data.</summary>
        private sealed class CachedMarketData
        {
            public List<MarketNewsItem> News { get; init; } = new();
            public string? UsdZarRate { get; init; }
            public DateTime RetrievedAtUtc { get; init; }
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//