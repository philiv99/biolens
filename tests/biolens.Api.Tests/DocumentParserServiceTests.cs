using biolens.Api.Services;
using biolens.Api.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace biolens.Api.Tests;

/// <summary>
/// Tests for the DocumentParserService which parses .docx files into structured paragraphs.
/// Uses a real .docx created in memory via DocumentFormat.OpenXml.
/// </summary>
public class DocumentParserServiceTests
{
    private readonly DocumentParserService _parser;

    public DocumentParserServiceTests()
    {
        var logger = new Mock<ILogger<DocumentParserService>>();
        _parser = new DocumentParserService(logger.Object);
    }

    [Fact]
    public async Task ParseDocxAsync_ValidDocument_ReturnsParagraphs()
    {
        // Arrange — create a .docx in memory
        using var stream = CreateDocx("Hello World", "This is a test paragraph.", "Third paragraph here.");

        // Act
        var paragraphs = await _parser.ParseDocxAsync(stream);

        // Assert
        Assert.Equal(3, paragraphs.Count);
        Assert.Equal("Hello World", paragraphs[0].Text);
        Assert.Equal(0, paragraphs[0].Index);
        Assert.Equal("This is a test paragraph.", paragraphs[1].Text);
        Assert.Equal(1, paragraphs[1].Index);
        Assert.Equal("Third paragraph here.", paragraphs[2].Text);
        Assert.Equal(2, paragraphs[2].Index);
    }

    [Fact]
    public async Task ParseDocxAsync_EmptyDocument_ReturnsEmptyList()
    {
        using var stream = CreateDocx(); // No paragraphs with text

        var paragraphs = await _parser.ParseDocxAsync(stream);

        Assert.Empty(paragraphs);
    }

    [Fact]
    public async Task ParseDocxAsync_SkipsEmptyParagraphs()
    {
        // Arrange — first paragraph has text, second is whitespace, third has text
        using var stream = CreateDocxWithBlanks("Real content", "", "   ", "More content");

        var paragraphs = await _parser.ParseDocxAsync(stream);

        Assert.Equal(2, paragraphs.Count);
        Assert.Equal("Real content", paragraphs[0].Text);
        Assert.Equal("More content", paragraphs[1].Text);
    }

    [Fact]
    public async Task ParseDocxAsync_AssignsSequentialIndexes()
    {
        using var stream = CreateDocx("A", "B", "C", "D");

        var paragraphs = await _parser.ParseDocxAsync(stream);

        for (int i = 0; i < paragraphs.Count; i++)
        {
            Assert.Equal(i, paragraphs[i].Index);
        }
    }

    [Fact]
    public async Task ParseDocxAsync_InvalidStream_ThrowsInvalidOperationException()
    {
        using var stream = new MemoryStream(new byte[] { 0x00, 0x01, 0x02 }); // Not a valid docx

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _parser.ParseDocxAsync(stream));
    }

    [Fact]
    public async Task ParseDocxAsync_CancellationRespected()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        using var stream = CreateDocx("Test");

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _parser.ParseDocxAsync(stream, cts.Token));
    }

    // ---------- Helpers ----------

    private static MemoryStream CreateDocx(params string[] texts)
    {
        var ms = new MemoryStream();
        using (var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            var body = mainPart.Document.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Body());

            foreach (var text in texts)
            {
                var para = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                var run = para.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
            }
        }

        ms.Position = 0;
        return ms;
    }

    private static MemoryStream CreateDocxWithBlanks(params string[] texts)
    {
        var ms = new MemoryStream();
        using (var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Create(ms, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new DocumentFormat.OpenXml.Wordprocessing.Document();
            var body = mainPart.Document.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Body());

            foreach (var text in texts)
            {
                var para = body.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Paragraph());
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var run = para.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Run());
                    run.AppendChild(new DocumentFormat.OpenXml.Wordprocessing.Text(text));
                }
            }
        }

        ms.Position = 0;
        return ms;
    }
}
