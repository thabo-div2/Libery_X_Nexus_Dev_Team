using API.Identity;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace API.Tests.TestHelpers
{
    // ASP.NET Identity's UserManager<TUser> has a large constructor, but every
    // method we need to stub (FindByEmailAsync, CheckPasswordAsync, etc.) is
    // virtual, so it can be mocked directly without a real database or a real
    // password hasher behind it. This is the standard pattern for unit
    // testing code that depends on UserManager.
    public static class MockUserManagerHelper
    {
        public static Mock<UserManager<ApplicationUser>> Create()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();

            var manager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null, null, null, null, null, null, null, null);

            return manager;
        }
    }
}
