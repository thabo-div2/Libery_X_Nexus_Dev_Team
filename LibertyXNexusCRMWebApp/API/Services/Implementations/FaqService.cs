using System.Text.Json;
using API.Services.Interfaces;

namespace API.Services.Implementations
{
    /// <summary>
    /// Service for managing frequently asked questions (FAQs).
    /// </summary>
    public class FaqService : IFaqService
    {
        private readonly string _faqsPath;

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Initializes a new instance of the <see cref="FaqService"/> class.
        /// </summary>
        /// <param name="env"></param>
        public FaqService(IWebHostEnvironment env)
        {
            var dataDirectory = Path.Combine(env.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDirectory);
            _faqsPath = Path.Combine(dataDirectory, "faqs.json");
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Retrieves all FAQs from the data source.
        /// </summary>
        /// <returns></returns>
        public async Task<List<Faq>> GetAllAsync()
        {
            return await ReadAsync<Faq>(_faqsPath);
        }

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Searches for FAQs that match the given query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
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

        //-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------//
        /// <summary>
        /// Reads a list of objects of type <typeparamref name="T"/> from a JSON file at the specified path.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
