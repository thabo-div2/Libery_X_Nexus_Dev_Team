using System.Text.Json;
using API.Services.Interfaces;

namespace API.Services.Implementations
{
    // FAQ content is static reference data (not user records), so unlike the
    // rest of the API it's read straight from a JSON file instead of the
    // EF Core / SQL database - the same approach the old Backend project
    // used before it was dropped from the solution.
    public class FaqService : IFaqService
    {
        private readonly string _faqsPath;

        public FaqService(IWebHostEnvironment env)
        {
            var dataDirectory = Path.Combine(env.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDirectory);
            _faqsPath = Path.Combine(dataDirectory, "faqs.json");
        }

        public async Task<List<Faq>> GetAllAsync()
        {
            return await ReadAsync<Faq>(_faqsPath);
        }

        public async Task<List<Faq>> SearchAsync(string query)
        {
            var faqs = await ReadAsync<Faq>(_faqsPath);

            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<Faq>();
            }

            var words = query
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(w => w.Length > 2)
                .ToArray();

            if (words.Length == 0)
            {
                return new List<Faq>();
            }

            return faqs
                .Select(f => new { Faq = f, Score = words.Count(w => f.Question.ToLowerInvariant().Contains(w)) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Faq)
                .ToList();
        }

        private static async Task<List<T>> ReadAsync<T>(string path)
        {
            if (!File.Exists(path))
            {
                return new List<T>();
            }

            await using var stream = File.OpenRead(path);
            var data = await JsonSerializer.DeserializeAsync<List<T>>(stream);
            return data ?? new List<T>();
        }
    }
}
