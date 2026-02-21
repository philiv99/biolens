namespace biolens.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using biolens.Api.Models;
using biolens.Api.Services;

/// <summary>
/// Handles document upload, listing, retrieval, and AI extraction.
/// </summary>
[ApiController]
[Route("api/documents")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentParserService _parser;
    private readonly IStorageService _storage;
    private readonly IExtractionService _extraction;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IDocumentParserService parser,
        IStorageService storage,
        IExtractionService extraction,
        ILogger<DocumentsController> logger)
    {
        _parser = parser;
        _storage = storage;
        _extraction = extraction;
        _logger = logger;
    }

    /// <summary>Upload a .docx file with subject names.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] string subjectNames,
        CancellationToken ct)
    {
        // Validate user
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { error = "X-User-Id header is required." });

        // Validate file
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file was uploaded." });

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        if (ext != ".docx")
            return BadRequest(new { error = "Only .docx files are supported." });

        // Validate subject names
        if (string.IsNullOrWhiteSpace(subjectNames))
            return BadRequest(new { error = "At least one subject name is required." });

        var names = subjectNames
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (names.Count == 0)
            return BadRequest(new { error = "At least one valid subject name is required." });

        try
        {
            // Buffer the file to a MemoryStream so we can read it twice (parse + save)
            using var buffer = new MemoryStream();
            await file.CopyToAsync(buffer, ct);

            // Parse the document
            buffer.Position = 0;
            var paragraphs = await _parser.ParseDocxAsync(buffer, ct);

            if (paragraphs.Count == 0)
                return BadRequest(new { error = "The document appears to be empty or could not be parsed." });

            // Create parsed document record
            var doc = new ParsedDocument
            {
                Id = Guid.NewGuid().ToString(),
                FileName = file.FileName,
                SubjectNames = names,
                UploadedBy = userId,
                UploadedAt = DateTime.UtcNow,
                Paragraphs = paragraphs,
                TotalParagraphs = paragraphs.Count
            };

            // Save the original file
            buffer.Position = 0;
            await _storage.SaveUploadedFileAsync(doc.Id, buffer, file.FileName, ct);

            // Save parsed document
            await _storage.SaveParsedDocumentAsync(doc, ct);

            _logger.LogInformation(
                "Document uploaded: {DocId}, {FileName}, {ParagraphCount} paragraphs",
                doc.Id, file.FileName, paragraphs.Count);

            return CreatedAtAction(nameof(GetDocument), new { id = doc.Id }, new DocumentSummary
            {
                Id = doc.Id,
                FileName = doc.FileName,
                SubjectNames = doc.SubjectNames,
                UploadedAt = doc.UploadedAt,
                TotalParagraphs = doc.TotalParagraphs,
                HasExtraction = false
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Document parsing failed");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>List all documents for the current user.</summary>
    [HttpGet]
    public async Task<IActionResult> ListDocuments(CancellationToken ct)
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { error = "X-User-Id header is required." });

        var docs = await _storage.ListDocumentsAsync(userId, ct);
        return Ok(docs);
    }

    /// <summary>Get a specific document with its paragraphs.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDocument(string id, CancellationToken ct)
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { error = "X-User-Id header is required." });

        var doc = await _storage.GetParsedDocumentAsync(id, ct);
        if (doc == null)
            return NotFound(new { error = "Document not found." });

        if (doc.UploadedBy != userId)
            return NotFound(new { error = "Document not found." });

        return Ok(doc);
    }

    /// <summary>Run AI extraction on a document.</summary>
    [HttpPost("{id}/extract")]
    public async Task<IActionResult> Extract(
        string id,
        [FromBody] ExtractRequest? request,
        CancellationToken ct)
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { error = "X-User-Id header is required." });

        // AI token comes in a separate header
        var aiToken = Request.Headers["X-AI-Token"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(aiToken))
            return BadRequest(new { error = "X-AI-Token header is required for extraction." });

        var doc = await _storage.GetParsedDocumentAsync(id, ct);
        if (doc == null)
            return NotFound(new { error = "Document not found." });

        if (doc.UploadedBy != userId)
            return NotFound(new { error = "Document not found." });

        try
        {
            var extraction = await _extraction.ExtractAsync(
                doc,
                aiToken,
                request?.Model,
                request?.ApiBaseUrl,
                ct);

            await _storage.SaveExtractionAsync(extraction, ct);

            _logger.LogInformation("Extraction complete for document {DocId}", id);
            return Ok(extraction);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Extraction failed for document {DocId}", id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Get extraction results for a document.</summary>
    [HttpGet("{id}/extraction")]
    public async Task<IActionResult> GetExtraction(string id, CancellationToken ct)
    {
        var userId = Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { error = "X-User-Id header is required." });

        // Verify ownership
        var doc = await _storage.GetParsedDocumentAsync(id, ct);
        if (doc == null || doc.UploadedBy != userId)
            return NotFound(new { error = "Document not found." });

        var extraction = await _storage.GetExtractionAsync(id, ct);
        if (extraction == null)
            return NotFound(new { error = "No extraction found for this document." });

        return Ok(extraction);
    }
}
