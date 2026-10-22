using API.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Tests.TestHelpers
{
    /// <summary>
    /// Helpers for giving a controller a fake signed-in user, so tests can check
    /// who is allowed to see what without running the real API.
    /// </summary>
    public static class ControllerTestHelper
    {
        /// <summary>
        /// Makes the controller act as an advisor with the given advisor id.
        /// </summary>
        public static T AsAdvisor<T>(this T controller, int advisorId) where T : ControllerBase
        {
            return controller.WithClaims(AppRoles.Advisor, ("advisorId", advisorId.ToString()));
        }

        /// <summary>
        /// Makes the controller act as a client with the given client id.
        /// </summary>
        public static T AsClient<T>(this T controller, int clientId) where T : ControllerBase
        {
            return controller.WithClaims(AppRoles.Client, ("clientId", clientId.ToString()));
        }

        /// <summary>
        /// Makes the controller act as a signed-in user with a role but no advisor or client id claim.
        /// </summary>
        public static T AsRoleWithoutIds<T>(this T controller, string role) where T : ControllerBase
        {
            return controller.WithClaims(role);
        }

        /// <summary>
        /// Gives the controller a user built the same way the API builds one from a JWT:
        /// the role is read from the "role" claim, not the default Microsoft claim type.
        /// </summary>
        public static T WithClaims<T>(this T controller, string role, params (string Type, string Value)[] extraClaims)
            where T : ControllerBase
        {
            var claims = new List<Claim>
            {
                new("sub", "user-1"),
                new("email", "user@nexus.test"),
                new("role", role)
            };
            claims.AddRange(extraClaims.Select(c => new Claim(c.Type, c.Value)));

            var identity = new ClaimsIdentity(claims, "TestAuth", "sub", "role");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            return controller;
        }

        /// <summary>
        /// Reads the "message" property of an error object returned by a controller.
        /// Controllers return anonymous objects such as <c>new { message = "..." }</c>.
        /// </summary>
        public static string? MessageOf(object? errorBody)
        {
            return errorBody?.GetType().GetProperty("message")?.GetValue(errorBody) as string;
        }
    }
}