using Shared.Models.Enums;

namespace API.Identity
{

    public static class AppRoles
    {
        public const string Advisor = "Advisor";
        public const string Client = "Client";

        public static UserRole ToUserRole(string roleClaim) => roleClaim switch
        {
            Advisor => UserRole.FinancialAdviser,
            Client => UserRole.RegisteredClient,
            _ => throw new ArgumentOutOfRangeException(nameof(roleClaim), roleClaim, "Unknown role claim")
        };
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
