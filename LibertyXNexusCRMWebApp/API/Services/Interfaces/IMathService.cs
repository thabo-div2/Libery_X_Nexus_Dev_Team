namespace API.Services.Interfaces
{
    public interface IMathService
    {
        bool LooksLikeMathQuestion(string input);
        string Solve(string input);
    }
}
