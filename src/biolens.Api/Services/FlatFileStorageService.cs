namespace biolens.Api.Services;

using System.Text.Json;
using biolens.Api.Models;

/// <summary>
/// Flat-file JSON storage for parsed documents and extraction results.
/// All data is stored under the configured storage root directory.
/// </summary>
public interface IStorageService
{
    Task<ParsedDocument> SaveParsedDocumentAsync(ParsedDocument doc, CancellationToken ct = default);
    Task<ParsedDocument?> GetParsedDocumentAsync(string docId, CancellationToken ct = default);
    Task<List<DocumentSummary>> ListDocumentsAsync(string userId, CancellationToken ct = default);
    Task SaveUploadedFileAsync(string docId, Stream fileStream, string fileName, CancellationToken ct = default);

    Task SaveExtractionAsync(BiographicalExtraction extraction, CancellationToken ct = default);
    Task<BiographicalExtraction?> GetExtractionAsync(string docId, CancellationToken ct = default);
    bool ExtractionExists(string docId);
}

public class FlatFileStorageService : IStorageService
{
    private readonly string _storageRoot;
    private readonly ILogger<FlatFileStorageService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FlatFileStorageService(IConfiguration config, ILogger<FlatFileStorageService> logger)
    {
        _storageRoot = config.GetValue<string>("Storage:RootPath")
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "storage");
        _storageRoot = Path.GetFullPath(_storageRoot);
        _logger = logger;

        // Ensure directories exist
        Directory.CreateDirectory(Path.Combine(_storageRoot, "uploads"));
        Directory.CreateDirectory(Path.Combine(_storageRoot, "parsed"));
        Directory.CreateDirectory(Path.Combine(_storageRoot, "extractions"));

        _logger.LogInformation("Storage root: {Root}", _storageRoot);
    }

    public async Task SaveUploadedFileAsync(string docId, Stream fileStream, string fileName, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        var filePath = Path.Combine(_storageRoot, "uploads", $"{docId}{ext}");

        await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(fs, ct);

        _logger.LogInformation("Saved uploaded file: {Path}", filePath);
    }

    public async Task<ParsedDocument> SaveParsedDocumentAsync(ParsedDocument doc, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_storageRoot, "parsed", $"{doc.Id}.json");
        var json = JsonSerializer.Serialize(doc, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, ct);

        _logger.LogInformation("Saved parsed document: {Id}", doc.Id);
        return doc;
    }

    public async Task<ParsedDocument?> GetParsedDocumentAsync(string docId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_storageRoot, "parsed", $"{docId}.json");
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath, ct);
        return JsonSerializer.Deserialize<ParsedDocument>(json, ReadOptions);
    }

    public async Task<List<DocumentSummary>> ListDocumentsAsync(string userId, CancellationToken ct = default)
    {
        var parsedDir = Path.Combine(_storageRoot, "parsed");
        var summaries = new List<DocumentSummary>();

        if (!Directory.Exists(parsedDir))
            return summaries;

        foreach (var file in Directory.GetFiles(parsedDir, "*.json"))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var json = await File.ReadAllTextAsync(file, ct);
                var doc = JsonSerializer.Deserialize<ParsedDocument>(json, ReadOptions);

                if (doc == null) continue;

                // Filter by user
                if (!string.IsNullOrEmpty(userId) && doc.UploadedBy != userId)
                    continue;

                var docId = Path.GetFileNameWithoutExtension(file);
                summaries.Add(new DocumentSummary
                {
                    Id = doc.Id,
                    FileName = doc.FileName,
                    SubjectNames = doc.SubjectNames,
                    UploadedAt = doc.UploadedAt,
                    TotalParagraphs = doc.TotalParagraphs,
                    HasExtraction = ExtractionExists(docId)
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read parsed document: {File}", file);
            }
        }

        return summaries.OrderByDescending(s => s.UploadedAt).ToList();
    }

    public async Task SaveExtractionAsync(BiographicalExtraction extraction, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_storageRoot, "extractions", $"{extraction.DocumentId}.json");
        var json = JsonSerializer.Serialize(extraction, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, ct);

        _logger.LogInformation("Saved extraction for document: {DocId}", extraction.DocumentId);
    }

    public async Task<BiographicalExtraction?> GetExtractionAsync(string docId, CancellationToken ct = default)
    {
        var filePath = Path.Combine(_storageRoot, "extractions", $"{docId}.json");
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath, ct);
        return JsonSerializer.Deserialize<BiographicalExtraction>(json, ReadOptions);
    }

    public bool ExtractionExists(string docId)
    {
        var filePath = Path.Combine(_storageRoot, "extractions", $"{docId}.json");
        return File.Exists(filePath);
    }
}
