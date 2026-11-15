using Microsoft.JSInterop;
using Shared.DTOs.Insights;

namespace frontend.Services;

/// <summary>
/// Dashboard numbers, renewals, client timelines, bulk document requests and CSV exports for advisors.
/// </summary>
public class InsightsService(HttpClient http, IJSRuntime js)
{
    public Task<(PracticeInsightsDto? Value, string? Error)> GetDashboardAsync() =>
        ApiErrors.SendAsync<PracticeInsightsDto>(() => http.GetAsync("insights/dashboard"));

    public Task<(List<RenewalDto>? Value, string? Error)> GetRenewalsAsync(int days) =>
        ApiErrors.SendAsync<List<RenewalDto>>(() => http.GetAsync($"insights/renewals?days={days}"));

    public Task<(TimelinePageDto? Value, string? Error)> GetTimelineAsync(int clientId, int take, string? kind) =>
        ApiErrors.SendAsync<TimelinePageDto>(() => http.GetAsync(
            $"insights/clients/{clientId}/timeline?take={take}" + (string.IsNullOrEmpty(kind) ? "" : $"&kind={Uri.EscapeDataString(kind)}")));

    public Task<(BulkDocumentRequestResult? Value, string? Error)> RequestDocumentsAsync(BulkDocumentRequestInput input) =>
        ApiErrors.SendAsync<BulkDocumentRequestResult>(() => http.PostAsJsonAsync("documentrequests/bulk", input));

    public Task<string?> DownloadClientSummaryAsync(int clientId) => DownloadAsync($"insights/clients/{clientId}/export", "client-summary.csv");

    public Task<string?> DownloadDocumentListAsync() => DownloadAsync("insights/documents/export", "documents.csv");

    /// <summary>
    /// Fetches a file from the API (with the advisor's sign-in) and hands it to the browser to save.
    /// Returns an error message, or null when the download started.
    /// </summary>
    private async Task<string?> DownloadAsync(string url, string fallbackName)
    {
        try
        {
            using var response = await http.GetAsync(url);
            if (!response.IsSuccessStatusCode) return await ApiErrors.ReadAsync(response);

            var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? fallbackName;
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "text/csv";

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var streamRef = new DotNetStreamReference(stream);
            await js.InvokeVoidAsync("nxDownloadFile", fileName, contentType, streamRef);
            return null;
        }
        catch (HttpRequestException)
        {
            return ApiErrors.CantReachServer;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
        catch (Exception)
        {
            return ApiErrors.Unexpected;
        }
    }
}
