using biolens.Api.Controllers;
using biolens.Api.Services;
using biolens.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace biolens.Api.Tests;

/// <summary>
/// Unit tests for DocumentsController with mocked services.
/// </summary>
public class DocumentsControllerTests
{
    private readonly Mock<IDocumentParserService> _parserMock = new();
    private readonly Mock<IStorageService> _storageMock = new();
    private readonly Mock<IExtractionService> _extractionMock = new();
    private readonly Mock<ILogger<DocumentsController>> _loggerMock = new();
    private readonly DocumentsController _controller;

    public DocumentsControllerTests()
    {
        _controller = new DocumentsController(
            _parserMock.Object,
            _storageMock.Object,
            _extractionMock.Object,
            _loggerMock.Object);
    }

    private void SetUserId(string userId)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-User-Id"] = userId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private void SetHeaders(string userId, string? aiToken = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-User-Id"] = userId;
        if (aiToken != null)
            httpContext.Request.Headers["X-AI-Token"] = aiToken;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    // --- Upload Tests ---

    [Fact]
    public async Task Upload_NoUserIdHeader_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await _controller.Upload(null!, "John", CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Upload_NoFile_ReturnsBadRequest()
    {
        SetUserId("user-1");

        var result = await _controller.Upload(null!, "John", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_NonDocxFile_ReturnsBadRequest()
    {
        SetUserId("user-1");
        var file = CreateMockFile("test.pdf", new byte[] { 1, 2, 3 });

        var result = await _controller.Upload(file, "John", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_EmptySubjectNames_ReturnsBadRequest()
    {
        SetUserId("user-1");
        var file = CreateMockFile("test.docx", new byte[] { 1, 2, 3 });

        var result = await _controller.Upload(file, "", CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_ValidRequest_ReturnsCreated()
    {
        SetUserId("user-1");
        var file = CreateMockFile("test.docx", new byte[] { 1, 2, 3 });

        _parserMock
            .Setup(p => p.ParseDocxAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentParagraph>
            {
                new(0, "Hello", "Normal"),
                new(1, "World", "Normal")
            });

        _storageMock
            .Setup(s => s.SaveParsedDocumentAsync(It.IsAny<ParsedDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParsedDocument d, CancellationToken _) => d);

        var result = await _controller.Upload(file, "John, Johnny", CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var summary = Assert.IsType<DocumentSummary>(created.Value);
        Assert.Equal("test.docx", summary.FileName);
        Assert.Equal(2, summary.SubjectNames.Count);
        Assert.Contains("John", summary.SubjectNames);
        Assert.Contains("Johnny", summary.SubjectNames);
    }

    // --- List Tests ---

    [Fact]
    public async Task ListDocuments_NoUserId_ReturnsUnauthorized()
    {
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

        var result = await _controller.ListDocuments(CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task ListDocuments_ReturnsOkWithDocs()
    {
        SetUserId("user-1");
        _storageMock
            .Setup(s => s.ListDocumentsAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DocumentSummary>
            {
                new() { Id = "d1", FileName = "a.docx" }
            });

        var result = await _controller.ListDocuments(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var docs = Assert.IsType<List<DocumentSummary>>(ok.Value);
        Assert.Single(docs);
    }

    // --- GetDocument Tests ---

    [Fact]
    public async Task GetDocument_NotFound_Returns404()
    {
        SetUserId("user-1");
        _storageMock
            .Setup(s => s.GetParsedDocumentAsync("no-doc", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParsedDocument?)null);

        var result = await _controller.GetDocument("no-doc", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetDocument_WrongUser_Returns404()
    {
        SetUserId("user-1");
        _storageMock
            .Setup(s => s.GetParsedDocumentAsync("doc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParsedDocument { Id = "doc-1", UploadedBy = "user-2" });

        var result = await _controller.GetDocument("doc-1", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetDocument_CorrectUser_ReturnsOk()
    {
        SetUserId("user-1");
        _storageMock
            .Setup(s => s.GetParsedDocumentAsync("doc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParsedDocument { Id = "doc-1", UploadedBy = "user-1", FileName = "test.docx" });

        var result = await _controller.GetDocument("doc-1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var doc = Assert.IsType<ParsedDocument>(ok.Value);
        Assert.Equal("test.docx", doc.FileName);
    }

    // --- Extract Tests ---

    [Fact]
    public async Task Extract_NoAiToken_ReturnsBadRequest()
    {
        SetHeaders("user-1"); // No AI token
        _storageMock
            .Setup(s => s.GetParsedDocumentAsync("doc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParsedDocument { Id = "doc-1", UploadedBy = "user-1" });

        var result = await _controller.Extract("doc-1", null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Extract_ValidRequest_ReturnsOk()
    {
        SetHeaders("user-1", "sk-test-token");

        var doc = new ParsedDocument { Id = "doc-1", UploadedBy = "user-1" };
        _storageMock
            .Setup(s => s.GetParsedDocumentAsync("doc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(doc);

        var extraction = new BiographicalExtraction
        {
            DocumentId = "doc-1",
            Summary = "Test extraction",
            Categories = new ExtractionCategories()
        };

        _extractionMock
            .Setup(e => e.ExtractAsync(doc, "sk-test-token", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(extraction);

        var result = await _controller.Extract("doc-1", null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var ext = Assert.IsType<BiographicalExtraction>(ok.Value);
        Assert.Equal("Test extraction", ext.Summary);
    }

    // --- GetExtraction Tests ---

    [Fact]
    public async Task GetExtraction_NoExtraction_Returns404()
    {
        SetUserId("user-1");
        _storageMock
            .Setup(s => s.GetParsedDocumentAsync("doc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParsedDocument { Id = "doc-1", UploadedBy = "user-1" });
        _storageMock
            .Setup(s => s.GetExtractionAsync("doc-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((BiographicalExtraction?)null);

        var result = await _controller.GetExtraction("doc-1", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // --- Helpers ---

    private static IFormFile CreateMockFile(string fileName, byte[] content)
    {
        var stream = new MemoryStream(content);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(() => new MemoryStream(content));
        return fileMock.Object;
    }
}
