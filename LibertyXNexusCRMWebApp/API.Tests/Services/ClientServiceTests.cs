using API.Repositories.Interfaces;
using API.Services.Implementations;
using API.DTOs.Clients;
using Moq;
using Shared.Models;
using Shared.Models.Enums;
using Xunit;

namespace API.Tests.Services
{
    /// <summary>
    /// Unit tests for the ClientService: getting, searching, adding, updating and deleting clients.
    /// </summary>
    public class ClientServiceTests
    {
        private readonly Mock<IClientRepository> _clientRepository = new();
        private readonly ClientService _sut;

        /// <summary>
        /// Sets up the ClientService with a fake client repository.
        /// </summary>
        public ClientServiceTests()
        {
            _sut = new ClientService(_clientRepository.Object);
        }

        /// <summary>
        /// Getting a client that doesn't exist should give back null.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WhenClientDoesNotExist_ReturnsNull()
        {
            _clientRepository.Setup(r => r.GetWithDetailsAsync(404)).ReturnsAsync((Client?)null);

            var result = await _sut.GetByIdAsync(404);

            Assert.Null(result);
        }

        /// <summary>
        /// Getting a client that exists should give back their details.
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_WhenClientExists_ReturnsMappedDto()
        {
            var client = new Client { ClientId = 1, FirstName = "Jane", LastName = "Doe", Email = "jane@nexus.test", AdvisorId = 5 };
            _clientRepository.Setup(r => r.GetWithDetailsAsync(1)).ReturnsAsync(client);

            var result = await _sut.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Jane Doe", result!.FullName);
            Assert.Equal(5, result.AdvisorId);
        }

        /// <summary>
        /// Searching should pass the filters to the repository and give back the results.
        /// </summary>
        [Fact]
        public async Task SearchAsync_PassesFiltersThroughToRepository_AndMapsResults()
        {
            var clients = new List<Client>
            {
                new() { ClientId = 1, FirstName = "Jane", LastName = "Doe", Email = "jane@nexus.test", AdvisorId = 5 }
            };

            _clientRepository
                .Setup(r => r.SearchAsync("jane", ClientStatus.Registered, 5))
                .ReturnsAsync(clients);

            var results = (await _sut.SearchAsync("jane", ClientStatus.Registered, 5)).ToList();

            Assert.Single(results);
            Assert.Equal("Jane Doe", results[0].FullName);
            _clientRepository.Verify(r => r.SearchAsync("jane", ClientStatus.Registered, 5), Times.Once);
        }

        /// <summary>
        /// Adding a client with an email that's already used should throw an error.
        /// </summary>
        [Fact]
        public async Task CreateAsync_WithEmailAlreadyInUse_ThrowsInvalidOperationException()
        {
            _clientRepository
                .Setup(r => r.GetByEmailAsync("taken@nexus.test"))
                .ReturnsAsync(new Client { ClientId = 1, FirstName = "A", LastName = "B", Email = "taken@nexus.test" });

            var request = new CreateClientRequest
            {
                IdentityProviderSubjectId = "sub-1",
                FirstName = "Jane",
                LastName = "Doe",
                Email = "taken@nexus.test",
                RiskProfile = "Moderate"
            };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateAsync(request));
        }

        /// <summary>
        /// Adding a client with a new email should create them.
        /// </summary>
        [Fact]
        public async Task CreateAsync_WithNewEmail_CreatesAndReturnsClient()
        {
            _clientRepository.Setup(r => r.GetByEmailAsync("new@nexus.test")).ReturnsAsync((Client?)null);

            Client? captured = null;
            _clientRepository
                .Setup(r => r.AddAsync(It.IsAny<Client>()))
                .Callback<Client>(c => { c.ClientId = 7; captured = c; })
                .ReturnsAsync(() => captured!);

            var request = new CreateClientRequest
            {
                IdentityProviderSubjectId = "sub-2",
                FirstName = "Jane",
                LastName = "Doe",
                Email = "new@nexus.test",
                RiskProfile = "Moderate",
                AdvisorId = 3,
                PopiaConsent = true
            };

            var result = await _sut.CreateAsync(request);

            Assert.Equal(7, result.ClientId);
            Assert.Equal("Registered", result.Status);
            Assert.Equal(3, result.AdvisorId);
        }

        /// <summary>
        /// Updating a client that doesn't exist should give back null.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WhenClientDoesNotExist_ReturnsNull()
        {
            _clientRepository.Setup(r => r.GetByIdAsync(404)).ReturnsAsync((Client?)null);

            var request = new UpdateClientRequest { FirstName = "Jane", LastName = "Doe" };

            var result = await _sut.UpdateAsync(404, request);

            Assert.Null(result);
        }

        /// <summary>
        /// Updating a client that exists should save the changes.
        /// </summary>
        [Fact]
        public async Task UpdateAsync_WhenClientExists_UpdatesAndReturnsClient()
        {
            var client = new Client { ClientId = 1, FirstName = "Old", LastName = "Name", Email = "jane@nexus.test" };
            _clientRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(client);
            _clientRepository.Setup(r => r.UpdateAsync(client)).Returns(Task.CompletedTask);

            var request = new UpdateClientRequest { FirstName = "Jane", LastName = "Doe", RiskProfile = "High" };

            var result = await _sut.UpdateAsync(1, request);

            Assert.NotNull(result);
            Assert.Equal("Jane Doe", result!.FullName);
            Assert.Equal("High", result.RiskProfile);
        }

        /// <summary>
        /// Deleting a client that doesn't exist should give back false.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WhenClientDoesNotExist_ReturnsFalse()
        {
            _clientRepository.Setup(r => r.ExistsAsync(404)).ReturnsAsync(false);

            var result = await _sut.DeleteAsync(404);

            Assert.False(result);
        }

        /// <summary>
        /// Deleting a client that exists should delete them and give back true.
        /// </summary>
        [Fact]
        public async Task DeleteAsync_WhenClientExists_DeletesAndReturnsTrue()
        {
            _clientRepository.Setup(r => r.ExistsAsync(1)).ReturnsAsync(true);
            _clientRepository.Setup(r => r.DeleteAsync(1)).Returns(Task.CompletedTask);

            var result = await _sut.DeleteAsync(1);

            Assert.True(result);
            _clientRepository.Verify(r => r.DeleteAsync(1), Times.Once);
        }
    }
}

//-----------------------------------------------------------------------------0o0o0o End of File 0o0o0o0o0o-------------------------------------------------------------------------------------------------//
