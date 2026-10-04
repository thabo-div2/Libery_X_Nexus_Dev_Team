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
    /// <summary>
    /// Helper for making a fake UserManager so the tests don't need a real database.
    /// </summary>
    public static class MockUserManagerHelper
    {
        /// <summary>
        /// Makes a fake UserManager we can set up in each test.
        /// </summary>
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

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
