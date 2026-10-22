using API.Controllers;
using API.DTOs.Clients;
using API.DTOs.Policies;
using API.Services.Interfaces;
using API.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Models.Enums;
using Xunit;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Tests for the PoliciesController, mainly who is allowed to see and change which policy.
    /// </summary>
    public class PoliciesControllerTests
    {
        private const int MyAdvisorId = 5;
        private const int OtherAdvisorId = 9;

        private readonly Mock<IPolicyService> _policyService = new();
        private readonly Mock<IClientService> _clientService = new();
        private readonly PoliciesController _sut;

        /// <summary>
        /// Sets up the controller with fake services.
        /// </summary>
        public PoliciesControllerTests()
        {
            _sut = new PoliciesController(_policyService.Object, _clientService.Object);
        }

        private static PolicyDto PolicyOf(int policyId, int? clientId) => new()
        {
            PolicyId = policyId,
            ClientId = clientId,
            PolicyName = "Life Cover",
            Provider = "Liberty",
            IsCatalogueItem = clientId is null
        };

        private void ClientBelongsToAdvisor(int clientId, int advisorId)
        {
            _clientService
                .Setup(s => s.GetByIdAsync(clientId))
                .ReturnsAsync(new ClientDetailDto { ClientId = clientId, AdvisorId = advisorId });
        }

        /// <summary>
        /// A policy that does not exist gives a 404.
        /// </summary>
        [Fact]
        public async Task GetById_WhenPolicyDoesNotExist_ReturnsNotFound()
        {
            _policyService.Setup(s => s.GetByIdAsync(404)).ReturnsAsync((PolicyDto?)null);
            _sut.AsClient(1);

            var result = await _sut.GetById(404);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// Catalogue policies belong to no client, so any signed-in user can see them.
        /// </summary>
        [Fact]
        public async Task GetById_WhenPolicyIsACatalogueItem_ReturnsOkToAnyClient()
        {
            var policy = PolicyOf(3, null);
            _policyService.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(policy);
            _sut.AsClient(1);

            var result = await _sut.GetById(3);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(policy, ok.Value);
        }

        /// <summary>
        /// A client can see their own policy.
        /// </summary>
        [Fact]
        public async Task GetById_WhenPolicyBelongsToTheClient_ReturnsOk()
        {
            var policy = PolicyOf(3, 1);
            _policyService.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(policy);
            _sut.AsClient(1);

            var result = await _sut.GetById(3);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(policy, ok.Value);
        }

        /// <summary>
        /// A client asking for another client's policy gets a 404, so its existence is not leaked.
        /// </summary>
        [Fact]
        public async Task GetById_WhenPolicyBelongsToAnotherClient_ReturnsNotFound()
        {
            _policyService.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(PolicyOf(3, 2));
            _sut.AsClient(1);

            var result = await _sut.GetById(3);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// An advisor can see a policy that belongs to one of their clients.
        /// </summary>
        [Fact]
        public async Task GetById_WhenPolicyBelongsToTheAdvisorsClient_ReturnsOk()
        {
            var policy = PolicyOf(3, 2);
            _policyService.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(policy);
            ClientBelongsToAdvisor(2, MyAdvisorId);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.GetById(3);

            Assert.IsType<OkObjectResult>(result.Result);
        }

        /// <summary>
        /// An advisor cannot see a policy that belongs to another advisor's client.
        /// </summary>
        [Fact]
        public async Task GetById_WhenPolicyBelongsToAnotherAdvisorsClient_ReturnsNotFound()
        {
            _policyService.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(PolicyOf(3, 2));
            ClientBelongsToAdvisor(2, OtherAdvisorId);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.GetById(3);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// A client cannot list another client's policies.
        /// </summary>
        [Fact]
        public async Task GetForClient_WhenClientAsksForAnotherClient_ReturnsForbid()
        {
            _sut.AsClient(1);

            var result = await _sut.GetForClient(2);

            Assert.IsType<ForbidResult>(result.Result);
            _policyService.Verify(s => s.GetForClientAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// A client can list their own policies.
        /// </summary>
        [Fact]
        public async Task GetForClient_WhenClientAsksForThemselves_ReturnsTheirPolicies()
        {
            var policies = new List<PolicyDto> { PolicyOf(3, 1) };
            _policyService.Setup(s => s.GetForClientAsync(1)).ReturnsAsync(policies);
            _sut.AsClient(1);

            var result = await _sut.GetForClient(1);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(policies, ok.Value);
        }

        /// <summary>
        /// An advisor cannot list the policies of a client that belongs to another advisor.
        /// </summary>
        [Fact]
        public async Task GetForClient_WhenClientBelongsToAnotherAdvisor_ReturnsForbid()
        {
            ClientBelongsToAdvisor(2, OtherAdvisorId);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.GetForClient(2);

            Assert.IsType<ForbidResult>(result.Result);
        }

        /// <summary>
        /// An advisor cannot add a policy to a client that belongs to another advisor.
        /// </summary>
        [Fact]
        public async Task CreateClientPolicy_WhenClientBelongsToAnotherAdvisor_ReturnsForbidAndDoesNotCreate()
        {
            ClientBelongsToAdvisor(2, OtherAdvisorId);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.CreateClientPolicy(2, new CreateClientPolicyRequest());

            Assert.IsType<ForbidResult>(result.Result);
            _policyService.Verify(s => s.CreateClientPolicyAsync(It.IsAny<int>(), It.IsAny<CreateClientPolicyRequest>()), Times.Never);
        }

        /// <summary>
        /// A new client policy gives a 201.
        /// </summary>
        [Fact]
        public async Task CreateClientPolicy_WhenValid_ReturnsCreated()
        {
            ClientBelongsToAdvisor(2, MyAdvisorId);
            _policyService
                .Setup(s => s.CreateClientPolicyAsync(2, It.IsAny<CreateClientPolicyRequest>()))
                .ReturnsAsync(PolicyOf(30, 2));
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.CreateClientPolicy(2, new CreateClientPolicyRequest());

            Assert.IsType<CreatedAtActionResult>(result.Result);
        }

        /// <summary>
        /// Invalid policy details give a 400 with the reason.
        /// </summary>
        [Fact]
        public async Task CreateClientPolicy_WhenDetailsAreInvalid_ReturnsBadRequest()
        {
            ClientBelongsToAdvisor(2, MyAdvisorId);
            _policyService
                .Setup(s => s.CreateClientPolicyAsync(2, It.IsAny<CreateClientPolicyRequest>()))
                .ThrowsAsync(new ArgumentException("End date must be after start date."));
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.CreateClientPolicy(2, new CreateClientPolicyRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("End date must be after start date.", ControllerTestHelper.MessageOf(badRequest.Value));
        }

        /// <summary>
        /// A missing record inside the service gives a 404.
        /// </summary>
        [Fact]
        public async Task CreateClientPolicy_WhenServiceCannotFindARecord_ReturnsNotFound()
        {
            ClientBelongsToAdvisor(2, MyAdvisorId);
            _policyService
                .Setup(s => s.CreateClientPolicyAsync(2, It.IsAny<CreateClientPolicyRequest>()))
                .ThrowsAsync(new KeyNotFoundException("Catalogue policy not found."));
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.CreateClientPolicy(2, new CreateClientPolicyRequest());

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// A status change that is not allowed gives a 409.
        /// </summary>
        [Fact]
        public async Task UpdateStatus_WhenTheChangeIsNotAllowed_ReturnsConflict()
        {
            _policyService.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(PolicyOf(3, null));
            _policyService
                .Setup(s => s.UpdateStatusAsync(3, It.IsAny<UpdatePolicyStatusRequest>()))
                .ThrowsAsync(new InvalidOperationException("A cancelled policy cannot be reactivated."));
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.UpdateStatus(3, new UpdatePolicyStatusRequest { NewStatus = PolicyStatus.Active });

            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal("A cancelled policy cannot be reactivated.", ControllerTestHelper.MessageOf(conflict.Value));
        }

        /// <summary>
        /// An advisor cannot change the status of a policy that belongs to another advisor's client.
        /// </summary>
        [Fact]
        public async Task UpdateStatus_WhenPolicyBelongsToAnotherAdvisorsClient_ReturnsNotFoundAndDoesNotUpdate()
        {
            _policyService.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(PolicyOf(3, 2));
            ClientBelongsToAdvisor(2, OtherAdvisorId);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.UpdateStatus(3, new UpdatePolicyStatusRequest { NewStatus = PolicyStatus.Lapsed });

            Assert.IsType<NotFoundObjectResult>(result.Result);
            _policyService.Verify(s => s.UpdateStatusAsync(It.IsAny<int>(), It.IsAny<UpdatePolicyStatusRequest>()), Times.Never);
        }

        /// <summary>
        /// Deleting a policy the advisor can access gives a 204.
        /// </summary>
        [Fact]
        public async Task Delete_WhenPolicyIsDeleted_ReturnsNoContent()
        {
            _policyService.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(PolicyOf(3, null));
            _policyService.Setup(s => s.DeleteAsync(3)).ReturnsAsync(true);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Delete(3);

            Assert.IsType<NoContentResult>(result);
        }

        /// <summary>
        /// An advisor cannot delete a policy that belongs to another advisor's client.
        /// </summary>
        [Fact]
        public async Task Delete_WhenPolicyBelongsToAnotherAdvisorsClient_ReturnsNotFoundAndDoesNotDelete()
        {
            _policyService.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(PolicyOf(3, 2));
            ClientBelongsToAdvisor(2, OtherAdvisorId);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Delete(3);

            Assert.IsType<NotFoundObjectResult>(result);
            _policyService.Verify(s => s.DeleteAsync(It.IsAny<int>()), Times.Never);
        }
    }
}