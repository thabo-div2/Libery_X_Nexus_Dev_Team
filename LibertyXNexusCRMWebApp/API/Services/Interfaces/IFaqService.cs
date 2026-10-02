namespace API.Services.Interfaces
{
    public record Faq(int Id, int Section, string Category, string Question, string Answer, bool RequiresHandoff);

    public interface IFaqService
    {
        Task<List<Faq>> GetAllAsync();
        Task<List<Faq>> SearchAsync(string query);
    }
}
