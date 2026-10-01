using Microsoft.AspNetCore.Identity;

namespace API.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
