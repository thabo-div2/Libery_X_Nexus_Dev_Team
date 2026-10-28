using API.DTOs.Invitations;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace API.Tests.DTOs
{
    /// <summary>
    /// Tests the validation rules on the invite request. [ApiController] runs these same
    /// rules before the controller action, so a request that fails here gets a 400.
    /// </summary>
    public class CreateInvitationRequestTests
    {
        /// <summary>
        /// Runs the request's validation attributes and returns any errors.
        /// </summary>
        private static List<ValidationResult> Validate(CreateInvitationRequest request)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
            return results;
        }

        /// <summary>
        /// A normal email address passes.
        /// </summary>
        [Theory]
        [InlineData("jane@nexus.test")]
        [InlineData("thandi.mokoena@example.co.za")]
        public void ValidEmail_PassesValidation(string email)
        {
            var errors = Validate(new CreateInvitationRequest { Email = email });

            Assert.Empty(errors);
        }

        /// <summary>
        /// Something that isn't an email address is rejected.
        /// </summary>
        [Theory]
        [InlineData("abc")]
        [InlineData("jane.nexus.test")]
        public void InvalidEmail_FailsValidation(string email)
        {
            var errors = Validate(new CreateInvitationRequest { Email = email });

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateInvitationRequest.Email)));
        }

        /// <summary>
        /// An empty email is rejected with the "required" message.
        /// </summary>
        [Fact]
        public void EmptyEmail_FailsValidation()
        {
            var errors = Validate(new CreateInvitationRequest { Email = string.Empty });

            Assert.Contains(errors, e => e.ErrorMessage == "Enter the client's email address.");
        }

        /// <summary>
        /// An email longer than the database column allows is rejected.
        /// </summary>
        [Fact]
        public void TooLongEmail_FailsValidation()
        {
            var email = new string('a', 250) + "@nexus.test";

            var errors = Validate(new CreateInvitationRequest { Email = email });

            Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateInvitationRequest.Email)));
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
