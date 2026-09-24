namespace frontend.Services
{
    public class CurrentUserService
    {
        public int? Id { get; private set; }
        public string? Role { get; private set; }
        public string? FirstName { get; private set; }
        public string? LastName { get; private set; }
        public string? Email { get; private set; }

        public bool IsClient => Role == "RegisteredClient";
        public bool IsAdvisor => Role == "FinancialAdviser";

        public void SignIn(AuthResult result)
        {
            Id = result.Id;
            Role = result.Role;
            FirstName = result.FirstName;
            LastName = result.LastName;
            Email = result.Email;
        }

        public void SignOut()
        {
            Id = null;
            Role = null;
            FirstName = null;
            LastName = null;
            Email = null;
        }
    }
}
