using API.Controllers;
using API.DTOs.Clients;
using API.Repositories.Interfaces;
using API.Services.Interfaces;
using API.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shared.Models.Enums;
using Xunit;

namespace API.Tests.Controllers
{
    /// <summary>
    /// Tests for the ClientsController, mainly who is allowed to see and change which client.
    /// </summary>
    public class ClientsControllerTests
    {
        private const int MyAdvisorId = 5;
        private const int OtherAdvisorId = 9;

        private readonly Mock<IClientService> _clientService = new();
        private readonly Mock<IAuditLogRepository> _auditLog = new();
        private readonly ClientsController _sut;

        /// <summary>
        /// Sets up the controller with fake services.
        /// </summary>
        public ClientsControllerTests()
        {
            _sut = new ClientsController(_clientService.Object, _auditLog.Object);
        }

        private static ClientDetailDto ClientOf(int clientId, int advisorId) => new()
        {
            ClientId = clientId,
            FirstName = "Jane",
            LastName = "Doe",
            FullName = "Jane Doe",
            Email = "jane@nexus.test",
            AdvisorId = advisorId
        };

        /// <summary>
        /// The advisor's own id from the token is used, so an advisor only searches their own clients.
        /// </summary>
        [Fact]
        public async Task Search_UsesTheAdvisorIdFromTheToken()
        {
            var items = new List<ClientListItemDto> { new() { ClientId = 1, FullName = "Jane Doe" } };
            _clientService.Setup(s => s.SearchAsync("jane", ClientStatus.Active, MyAdvisorId)).ReturnsAsync(items);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Search("jane", ClientStatus.Active);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(items, ok.Value);
            _clientService.Verify(s => s.SearchAsync("jane", ClientStatus.Active, MyAdvisorId), Times.Once);
        }

        /// <summary>
        /// A client that does not exist gives a 404.
        /// </summary>
        [Fact]
        public async Task GetById_WhenClientDoesNotExist_ReturnsNotFound()
        {
            _clientService.Setup(s => s.GetByIdAsync(404)).ReturnsAsync((ClientDetailDto?)null);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.GetById(404);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// A client can open their own profile.
        /// </summary>
        [Fact]
        public async Task GetById_WhenClientRequestsTheirOwnProfile_ReturnsOk()
        {
            var client = ClientOf(12, MyAdvisorId);
            _clientService.Setup(s => s.GetByIdAsync(12)).ReturnsAsync(client);
            _sut.AsClient(12);

            var result = await _sut.GetById(12);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(client, ok.Value);
        }

        /// <summary>
        /// A client cannot open someone else's profile.
        /// </summary>
        [Fact]
        public async Task GetById_WhenClientRequestsAnotherClientsProfile_ReturnsForbid()
        {
            _clientService.Setup(s => s.GetByIdAsync(13)).ReturnsAsync(ClientOf(13, MyAdvisorId));
            _sut.AsClient(12);

            var result = await _sut.GetById(13);

            Assert.IsType<ForbidResult>(result.Result);
        }

        /// <summary>
        /// An advisor can open a client assigned to them.
        /// </summary>
        [Fact]
        public async Task GetById_WhenAdvisorRequestsTheirOwnClient_ReturnsOk()
        {
            var client = ClientOf(12, MyAdvisorId);
            _clientService.Setup(s => s.GetByIdAsync(12)).ReturnsAsync(client);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.GetById(12);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(client, ok.Value);
        }

        /// <summary>
        /// An advisor asking for another advisor's client gets a 404, so the client's existence is not leaked.
        /// </summary>
        [Fact]
        public async Task GetById_WhenClientBelongsToAnotherAdvisor_ReturnsNotFound()
        {
            _clientService.Setup(s => s.GetByIdAsync(12)).ReturnsAsync(ClientOf(12, OtherAdvisorId));
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.GetById(12);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        /// <summary>
        /// A signed-in user with an unknown role gets nothing.
        /// </summary>
        [Fact]
        public async Task GetById_WhenRoleIsUnknown_ReturnsForbid()
        {
            _clientService.Setup(s => s.GetByIdAsync(12)).ReturnsAsync(ClientOf(12, MyAdvisorId));
            _sut.AsRoleWithoutIds("Stranger");

            var result = await _sut.GetById(12);

            Assert.IsType<ForbidResult>(result.Result);
        }

        /// <summary>
        /// The new client is always assigned to the signed-in advisor, even if the request body names someone else.
        /// </summary>
        [Fact]
        public async Task Create_OverridesAdvisorIdFromTheRequestWithTheOneInTheToken()
        {
            var request = new CreateClientRequest { FirstName = "Jane", LastName = "Doe", Email = "jane@nexus.test", AdvisorId = OtherAdvisorId };
            _clientService
                .Setup(s => s.CreateAsync(It.IsAny<CreateClientRequest>()))
                .ReturnsAsync(ClientOf(21, MyAdvisorId));
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Create(request);

            Assert.IsType<CreatedAtActionResult>(result.Result);
            _clientService.Verify(s => s.CreateAsync(It.Is<CreateClientRequest>(r => r.AdvisorId == MyAdvisorId)), Times.Once);
        }

        /// <summary>
        /// Creating a client writes an audit log entry.
        /// </summary>
        [Fact]
        public async Task Create_WhenSuccessful_WritesAnAuditLogEntry()
        {
            _clientService
                .Setup(s => s.CreateAsync(It.IsAny<CreateClientRequest>()))
                .ReturnsAsync(ClientOf(21, MyAdvisorId));
            _sut.AsAdvisor(MyAdvisorId);

            await _sut.Create(new CreateClientRequest());

            _auditLog.Verify(a => a.LogAsync(
                MyAdvisorId,
                UserRole.FinancialAdviser,
                AuditActionType.Create,
                "Client",
                It.IsAny<int?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()), Times.Once);
        }

        /// <summary>
        /// A duplicate client gives a 409 and nothing is logged as created.
        /// </summary>
        [Fact]
        public async Task Create_WhenClientAlreadyExists_ReturnsConflictAndDoesNotLog()
        {
            _clientService
                .Setup(s => s.CreateAsync(It.IsAny<CreateClientRequest>()))
                .ThrowsAsync(new InvalidOperationException("Email already in use."));
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Create(new CreateClientRequest());

            var conflict = Assert.IsType<ConflictObjectResult>(result.Result);
            Assert.Equal("Email already in use.", ControllerTestHelper.MessageOf(conflict.Value));
            _auditLog.Verify(a => a.LogAsync(
                It.IsAny<int?>(), It.IsAny<UserRole>(), It.IsAny<AuditActionType>(), It.IsAny<string>(),
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Never);
        }

        /// <summary>
        /// Invalid details give a 400 with the reason.
        /// </summary>
        [Fact]
        public async Task Create_WhenDetailsAreInvalid_ReturnsBadRequest()
        {
            _clientService
                .Setup(s => s.CreateAsync(It.IsAny<CreateClientRequest>()))
                .ThrowsAsync(new ArgumentException("POPIA consent is required."));
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Create(new CreateClientRequest());

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("POPIA consent is required.", ControllerTestHelper.MessageOf(badRequest.Value));
        }

        /// <summary>
        /// An advisor cannot update another advisor's client, and nothing is changed.
        /// </summary>
        [Fact]
        public async Task Update_WhenClientBelongsToAnotherAdvisor_ReturnsNotFoundAndDoesNotUpdate()
        {
            _clientService.Setup(s => s.GetByIdAsync(12)).ReturnsAsync(ClientOf(12, OtherAdvisorId));
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Update(12, new UpdateClientRequest());

            Assert.IsType<NotFoundObjectResult>(result.Result);
            _clientService.Verify(s => s.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateClientRequest>()), Times.Never);
        }

        /// <summary>
        /// An advisor can update their own client, and the change is logged.
        /// </summary>
        [Fact]
        public async Task Update_WhenClientBelongsToTheAdvisor_UpdatesAndLogs()
        {
            var updated = ClientOf(12, MyAdvisorId);
            _clientService.Setup(s => s.GetByIdAsync(12)).ReturnsAsync(ClientOf(12, MyAdvisorId));
            _clientService.Setup(s => s.UpdateAsync(12, It.IsAny<UpdateClientRequest>())).ReturnsAsync(updated);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Update(12, new UpdateClientRequest());

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(updated, ok.Value);
            _auditLog.Verify(a => a.LogAsync(
                MyAdvisorId, UserRole.FinancialAdviser, AuditActionType.Update, "Client",
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        }

        /// <summary>
        /// An advisor cannot delete another advisor's client, and nothing is deleted.
        /// </summary>
        [Fact]
        public async Task Delete_WhenClientBelongsToAnotherAdvisor_ReturnsNotFoundAndDoesNotDelete()
        {
            _clientService.Setup(s => s.GetByIdAsync(12)).ReturnsAsync(ClientOf(12, OtherAdvisorId));
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Delete(12);

            Assert.IsType<NotFoundObjectResult>(result);
            _clientService.Verify(s => s.DeleteAsync(It.IsAny<int>()), Times.Never);
        }

        /// <summary>
        /// An advisor can delete their own client, and the delete is logged.
        /// </summary>
        [Fact]
        public async Task Delete_WhenClientBelongsToTheAdvisor_DeletesAndLogs()
        {
            _clientService.Setup(s => s.GetByIdAsync(12)).ReturnsAsync(ClientOf(12, MyAdvisorId));
            _clientService.Setup(s => s.DeleteAsync(12)).ReturnsAsync(true);
            _sut.AsAdvisor(MyAdvisorId);

            var result = await _sut.Delete(12);

            Assert.IsType<NoContentResult>(result);
            _clientService.Verify(s => s.DeleteAsync(12), Times.Once);
            _auditLog.Verify(a => a.LogAsync(
                MyAdvisorId, UserRole.FinancialAdviser, AuditActionType.Delete, "Client",
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        }
    }
}