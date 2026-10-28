using API.Controllers;
using API.DTOs.Clients;
using API.DTOs.Documents;
using API.DTOs.Policies;
using API.Services.Interfaces;
using API.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Tests for getting the documents on a policy.
    /// </summary>
    public class DocumentsControllerTests
    {
        private const int MyAdvisorId = 5;
        private const int OtherAdvisorId = 6;

        private readonly Mock<IDocumentService> _documentService = new();
        private readonly Mock<IClientService> _clientService = new();
        private readonly Mock<IPolicyService> _policyService = new();
        private readonly DocumentsController _sut;

        /// <summary>
        /// Sets up the controller with fake services, signed in as advisor 5.
        /// </summary>
        public DocumentsControllerTests()
        {
            _sut = new DocumentsController(_documentService.Object, _clientService.Object, _policyService.Object)
                .AsAdvisor(MyAdvisorId);
        }

        /// <summary>
        /// A policy that isn't linked to a client returns 404 with a message instead of crashing.
        /// </summary>
        [Fact]
        public async Task GetForPolicy_WhenPolicyHasNoClient_ReturnsNotFoundWithMessage()
        {
            _policyService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new PolicyDto { PolicyId = 1, ClientId = null });

            var result = await _sut.GetForPolicy(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Policy is not linked to a client.", ControllerTestHelper.MessageOf(notFound.Value));
            _clientService.Verify(s => s.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// A policy that doesn't exist returns 404 with a message.
        /// </summary>
        [Fact]
        public async Task GetForPolicy_WhenPolicyDoesNotExist_ReturnsNotFoundWithMessage()
        {
            _policyService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync((PolicyDto?)null);

            var result = await _sut.GetForPolicy(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Policy was not found.", ControllerTestHelper.MessageOf(notFound.Value));
        }

        /// <summary>
        /// Another advisor's policy looks exactly like a missing one, so it isn't revealed.
        /// </summary>
        [Fact]
        public async Task GetForPolicy_WhenPolicyBelongsToAnotherAdvisor_ReturnsNotFound()
        {
            _policyService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new PolicyDto { PolicyId = 1, ClientId = 7 });
            _clientService.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(new ClientDetailDto { ClientId = 7, AdvisorId = OtherAdvisorId });

            var result = await _sut.GetForPolicy(1);

            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Policy was not found.", ControllerTestHelper.MessageOf(notFound.Value));
            _documentService.Verify(s => s.GetForPolicyAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// The advisor's own policy returns its documents.
        /// </summary>
        [Fact]
        public async Task GetForPolicy_WhenPolicyBelongsToMe_ReturnsTheDocuments()
        {
            var documents = new List<DocumentDto> { new() };
            _policyService.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new PolicyDto { PolicyId = 1, ClientId = 7 });
            _clientService.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(new ClientDetailDto { ClientId = 7, AdvisorId = MyAdvisorId });
            _documentService.Setup(s => s.GetForPolicyAsync(1)).ReturnsAsync(documents);

            var result = await _sut.GetForPolicy(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(documents, ok.Value);
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
