using System.Net.Http.Json;

namespace frontend.Services
{
    // DTOss
    public record Faq(int Id, int Section, string Category, string Question, string Answer, bool RequiresHandoff);

    /// <summary>
    /// Service that handles the FAQs of the advisor
    /// </summary>
    public class FaqService
    {
        private readonly HttpClient _http;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="FaqService"/> class with the specified <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="http"></param>
        public FaqService(HttpClient http)
        {
            _http = http;
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Get all the FAQs
        /// </summary>
        /// <returns></returns>
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
                return (new List<Faq>(), "Can't reach the server. Make sure the API project is running.");
            }
            catch (Exception ex)
            {
                return (new List<Faq>(), $"Something went wrong: {ex.Message}");
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// To search the FAQs and give a response
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
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
                return (new List<Faq>(), "Can't reach the server. Make sure the API project is running.");
            }
            catch (Exception ex)
            {
                return (new List<Faq>(), $"Something went wrong: {ex.Message}");
            }
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
