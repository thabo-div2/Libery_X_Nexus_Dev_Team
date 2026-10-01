using System.Net.Http.Json;

namespace frontend.Services
{
    public record Faq(int Id, int Section, string Category, string Question, string Answer, bool RequiresHandoff);

    public class FaqService
    {
        private readonly HttpClient _http;

        public FaqService(HttpClient http)
        {
            _http = http;
        }

        public async Task<(List<Faq> Faqs, string? Error)> GetAllAsync()
        {
            try
            {
                var response = await _http.GetAsync("Faq");
                if (!response.IsSuccessStatusCode)
                {
                    return (new List<Faq>(), $"The server reported an error (status {(int)response.StatusCode}).");
                }

                var result = await response.Content.ReadFromJsonAsync<List<Faq>>();
                return (result ?? new List<Faq>(), null);
            }
            catch (HttpRequestException)
            {
                return (new List<Faq>(), "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (new List<Faq>(), $"Something went wrong: {ex.Message}");
            }
        }

        public async Task<(List<Faq> Faqs, string? Error)> SearchAsync(string query)
        {
            try
            {
                var response = await _http.GetAsync($"Faq/search?q={Uri.EscapeDataString(query)}");
                if (!response.IsSuccessStatusCode)
                {
                    return (new List<Faq>(), $"The server reported an error (status {(int)response.StatusCode}).");
                }

                var result = await response.Content.ReadFromJsonAsync<List<Faq>>();
                return (result ?? new List<Faq>(), null);
            }
            catch (HttpRequestException)
            {
                return (new List<Faq>(), "Can't reach the server. Make sure the Backend project is running.");
            }
            catch (Exception ex)
            {
                return (new List<Faq>(), $"Something went wrong: {ex.Message}");
            }
        }
    }
}
