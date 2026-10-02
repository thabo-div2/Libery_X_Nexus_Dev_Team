namespace API.Services.Interfaces
{
    public interface IMarketInformationService
    {
        Task<MarketInformationResult> GetAsync(string query, CancellationToken cancellationToken = default);
    }

    public sealed class MarketInformationResult
    {
        public string Query { get; set; } = string.Empty;
        public DateTime RetrievedAtUtc { get; set; }
        public string DataFreshness { get; set; } = "Current information retrieved from external market/news sources.";
        public string? UsdZarRate { get; set; }
        public List<MarketNewsItem> News { get; set; } = new();
    }

    public sealed class MarketNewsItem
    {
        public string Title { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string PublishedAt { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public string? Sentiment { get; set; }
    }
}
