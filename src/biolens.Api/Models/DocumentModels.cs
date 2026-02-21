namespace biolens.Api.Models;

using System.Text.Json.Serialization;

/// <summary>Represents a paragraph from a parsed .docx document.</summary>
public record DocumentParagraph(
    int Index,
    string Text,
    string Style
);

/// <summary>Request body for uploading a document. The file comes via multipart form; these are the metadata fields.</summary>
public class UploadDocumentRequest
{
    /// <summary>Comma-separated list of names/aliases for the document subject.</summary>
    public string SubjectNames { get; set; } = string.Empty;
}

/// <summary>A parsed document stored as a flat JSON file.</summary>
public class ParsedDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FileName { get; set; } = string.Empty;
    public List<string> SubjectNames { get; set; } = new();
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public List<DocumentParagraph> Paragraphs { get; set; } = new();
    public int TotalParagraphs { get; set; }
}

/// <summary>Summary view of a document (without full paragraph content).</summary>
public class DocumentSummary
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public List<string> SubjectNames { get; set; } = new();
    public DateTime UploadedAt { get; set; }
    public int TotalParagraphs { get; set; }
    public bool HasExtraction { get; set; }
}
