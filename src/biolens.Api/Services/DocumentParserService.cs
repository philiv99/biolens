namespace biolens.Api.Services;

using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using biolens.Api.Models;

/// <summary>
/// Parses .docx files into structured paragraph data with positional metadata.
/// </summary>
public interface IDocumentParserService
{
    /// <summary>Parse a .docx stream into a list of paragraphs with index and style.</summary>
    Task<List<DocumentParagraph>> ParseDocxAsync(Stream fileStream, CancellationToken ct = default);
}

public class DocumentParserService : IDocumentParserService
{
    private readonly ILogger<DocumentParserService> _logger;

    public DocumentParserService(ILogger<DocumentParserService> logger)
    {
        _logger = logger;
    }

    public Task<List<DocumentParagraph>> ParseDocxAsync(Stream fileStream, CancellationToken ct = default)
    {
        var paragraphs = new List<DocumentParagraph>();

        try
        {
            using var wordDoc = WordprocessingDocument.Open(fileStream, false);
            var body = wordDoc.MainDocumentPart?.Document?.Body;

            if (body == null)
            {
                _logger.LogWarning("Document body is null — empty or malformed .docx");
                return Task.FromResult(paragraphs);
            }

            int index = 0;
            foreach (var element in body.Elements<Paragraph>())
            {
                ct.ThrowIfCancellationRequested();

                var text = element.InnerText?.Trim() ?? string.Empty;

                // Skip empty paragraphs
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                var style = element.ParagraphProperties?.ParagraphStyleId?.Val?.Value ?? "Normal";

                paragraphs.Add(new DocumentParagraph(index, text, style));
                index++;
            }

            _logger.LogInformation("Parsed {Count} non-empty paragraphs from .docx", paragraphs.Count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse .docx file");
            throw new InvalidOperationException("The uploaded file could not be parsed as a valid .docx document.", ex);
        }

        return Task.FromResult(paragraphs);
    }
}
