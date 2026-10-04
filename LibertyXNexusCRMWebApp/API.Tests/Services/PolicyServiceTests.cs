using API.Repositories.Interfaces;
using API.Services.Implementations;
using Moq;
using API.DTOs.Policies;
using Shared.Models;
using Shared.Models.Enums;
using Xunit;

namespace API.Tests.Services
{
    /// <summary>
    /// Tests for the PolicyService.
    /// </summary>
    public class PolicyServiceTests
    {
        private readonly Mock<IPolicyRepository> _policyRepository = new();
        private readonly Mock<IClientRepository> _clientRepository = new();
        private readonly Mock<ICaseRepository> _caseRepository = new();
        private readonly PolicyService _sut;

        /// <summary>
        /// Sets up the service with fakes.
        /// </summary>
        public PolicyServiceTests()
        {
            _sut = new PolicyService(_policyRepository.Object, _clientRepository.Object, _caseRepository.Object);
        }

        /// <summary>
        /// Catalogue policy has no client.
        /// </summary>
        [Fact]
        public async Task CreateCatalogueItemAsync_CreatesAsCatalogueItemWithNoClient()
        {
            Policy? captured = null;
            _policyRepository
                .Setup(r => r.AddAsync(It.IsAny<Policy>()))
                .Callback<Policy>(p => { p.PolicyId = 10; captured = p; })
                .ReturnsAsync(() => captured!);

            var request = new CreateCataloguePolicyRequest
            {
                PolicyName = "Life Cover",
                Provider = "Liberty",
                PremiumAmount = 500,
                CoverAmount = 1_000_000
            };

            var result = await _sut.CreateCatalogueItemAsync(request);

            Assert.Equal(10, result.PolicyId);
            Assert.True(result.IsCatalogueItem);
            Assert.Null(result.ClientId);
            Assert.Equal("Active", result.Status);
        }

        /// <summary>
        /// Unknown client throws an error.
        /// </summary>
        [Fact]
        public async Task CreateClientPolicyAsync_WithUnknownClient_ThrowsKeyNotFoundException()
        {
            _clientRepository.Setup(r => r.ExistsAsync(99)).ReturnsAsync(false);

            var request = new CreateClientPolicyRequest { PolicyName = "Life Cover", Provider = "Liberty" };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.CreateClientPolicyAsync(99, request));
        }

        /// <summary>
        /// End date before start date throws an error.
        /// </summary>
        [Fact]
        public async Task CreateClientPolicyAsync_WithEndDateBeforeStartDate_ThrowsArgumentException()
        {
            _clientRepository.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);

            var request = new CreateClientPolicyRequest
            {
                PolicyName = "Life Cover",
                Provider = "Liberty",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(-5)
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.CreateClientPolicyAsync(1, request));
        }

        /// <summary>
        /// Valid policy is created as pending.
        /// </summary>
        [Fact]
        public async Task CreateClientPolicyAsync_WithValidRequest_CreatesPendingPolicyForClient()
        {
            _clientRepository.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);

            Policy? captured = null;
            _policyRepository
                .Setup(r => r.AddAsync(It.IsAny<Policy>()))
                .Callback<Policy>(p => { p.PolicyId = 11; captured = p; })
                .ReturnsAsync(() => captured!);

            var request = new CreateClientPolicyRequest { PolicyName = "Life Cover", Provider = "Liberty" };

            var result = await _sut.CreateClientPolicyAsync(1, request);

            Assert.Equal(1, result.ClientId);
            Assert.False(result.IsCatalogueItem);
            Assert.Equal("Pending", result.Status);
        }

        /// <summary>
        /// Updating a missing policy returns null.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WhenPolicyDoesNotExist_ReturnsNull()
        {
            _policyRepository.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Policy?)null);

            var request = new UpdatePolicyRequest { PolicyName = "x", Provider = "y" };

            var result = await _sut.UpdateAsync(404, request);

            Assert.Null(result);
        }

        /// <summary>
        /// Bad dates on update throw an error.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WithEndDateBeforeStartDate_ThrowsArgumentException()
        {
            var policy = new Policy { PolicyId = 1, PolicyName = "Old", Provider = "Liberty", Status = PolicyStatus.Active };
            _policyRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(policy);

            var request = new UpdatePolicyRequest
            {
                PolicyName = "New",
                Provider = "Liberty",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(-1)
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.UpdateAsync(1, request));
        }

        /// <summary>
        /// Can't change a cancelled policy.
        /// </summary>
        [Fact]
        public async Task UpdateStatusAsync_WhenPolicyIsCancelled_ThrowsInvalidOperationException()
        {
            var policy = new Policy { PolicyId = 1, PolicyName = "x", Provider = "y", Status = PolicyStatus.Cancelled };
            _policyRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(policy);

            var request = new UpdatePolicyStatusRequest { NewStatus = PolicyStatus.Active };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateStatusAsync(1, request));
        }

        /// <summary>
        /// Missing policy throws an error.
        /// </summary>
        [Fact]
        public async Task UpdateStatusAsync_WhenPolicyDoesNotExist_ThrowsKeyNotFoundException()
        {
            _policyRepository.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Policy?)null);

            var request = new UpdatePolicyStatusRequest { NewStatus = PolicyStatus.Active };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.UpdateStatusAsync(404, request));
        }

        /// <summary>
        /// Valid status change works.
        /// </summary>
        [Fact]
        public async Task UpdateStatusAsync_WithValidTransition_UpdatesStatus()
        {
            var policy = new Policy { PolicyId = 1, PolicyName = "x", Provider = "y", Status = PolicyStatus.Pending };
            _policyRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(policy);
            _policyRepository.Setup(r => r.UpdateAsync(policy)).Returns(Task.CompletedTask);

            var request = new UpdatePolicyStatusRequest { NewStatus = PolicyStatus.Active };

            var result = await _sut.UpdateStatusAsync(1, request);

            Assert.Equal("Active", result.Status);
        }

        /// <summary>
        /// Deleting a missing policy returns false.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WhenPolicyDoesNotExist_ReturnsFalse()
        {
            _policyRepository.Setup(r => r.ExistsAsync(404)).ReturnsAsync(false);

            var result = await _sut.DeleteAsync(404);

            Assert.False(result);
            _policyRepository.Verify(r => r.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// Deleting a policy returns true.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WhenPolicyExists_DeletesAndReturnsTrue()
        {
            _policyRepository.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _policyRepository.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

            var result = await _sut.DeleteAsync(1);

            Assert.True(result);
            _policyRepository.Verify(r => r.DeleteAsync(1), Times.Once);
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
