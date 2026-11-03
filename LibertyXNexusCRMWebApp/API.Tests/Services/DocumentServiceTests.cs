using API.DTOs.Documents;
using API.Repositories.Interfaces;
using API.Services.Implementations;
using API.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Moq;
using Shared.Models;
using Xunit;

namespace API.Tests.Services
{
    /// <summary>
    /// Tests for the DocumentService.
    /// </summary>
    public class DocumentServiceTests
    {
        private readonly Mock<IDocumentRepository> _documents = new();
        private readonly Mock<IClientRepository> _clients = new();
        private readonly Mock<IPolicyRepository> _policies = new();
        private readonly Mock<IBlobStorageService> _blobs = new();
        private readonly DocumentService _sut;

        public DocumentServiceTests()
        {
            _sut = new DocumentService(_documents.Object, _clients.Object, _policies.Object, _blobs.Object);
        }

        private static IFormFile MakeFile(string name, string contentType, byte[] bytes)
        {
            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(name);
            file.Setup(f => f.ContentType).Returns(contentType);
            file.Setup(f => f.Length).Returns(bytes.Length);
            file.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(bytes));
            return file.Object;
        }

        [Fact]
        public async Task UploadAsync_PdfNameButExecutableBytes_IsRejected()
        {
            var request = new UploadDocumentRequest
            {
                ClientId = 1,
                PolicyId = 1,
                File = MakeFile("report.pdf", "application/pdf", new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03 })
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.UploadAsync(request));
            _blobs.Verify(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UploadAsync_DisallowedExtension_IsRejected()
        {
            var request = new UploadDocumentRequest
            {
                ClientId = 1,
                PolicyId = 1,
                File = MakeFile("run.exe", "application/pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D })
            };

            await Assert.ThrowsAsync<ArgumentException>(() => _sut.UploadAsync(request));
        }

        [Fact]
        public async Task GetByIdAsync_WhenMissing_ReturnsNull()
        {
            _documents.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Document?)null);

            Assert.Null(await _sut.GetByIdAsync(99));
        }
    }
}