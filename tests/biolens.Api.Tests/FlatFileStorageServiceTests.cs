using biolens.Api.Services;
using biolens.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace biolens.Api.Tests;

/// <summary>
/// Tests for FlatFileStorageService — verifies JSON persistence and retrieval.
/// Uses a temp directory that is cleaned up after each test.
/// </summary>
public class FlatFileStorageServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly FlatFileStorageService _storage;

    public FlatFileStorageServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "biolens-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempRoot);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Storage:RootPath", _tempRoot }
            })
            .Build();

        var logger = new Mock<ILogger<FlatFileStorageService>>();
        _storage = new FlatFileStorageService(config, logger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }

    [Fact]
    public async Task SaveAndGetParsedDocument_RoundTrips()
    {
        var doc = new ParsedDocument
        {
            Id = "test-doc-1",
            FileName = "test.docx",
            SubjectNames = new List<string> { "John", "Johnny" },
            UploadedBy = "user-1",
            UploadedAt = DateTime.UtcNow,
            Paragraphs = new List<DocumentParagraph>
            {
                new(0, "First paragraph", "Normal"),
                new(1, "Second paragraph", "Heading1"),
            },
            TotalParagraphs = 2
        };

        await _storage.SaveParsedDocumentAsync(doc);
        var retrieved = await _storage.GetParsedDocumentAsync("test-doc-1");

        Assert.NotNull(retrieved);
        Assert.Equal("test-doc-1", retrieved!.Id);
        Assert.Equal("test.docx", retrieved.FileName);
        Assert.Equal(2, retrieved.SubjectNames.Count);
        Assert.Equal(2, retrieved.Paragraphs.Count);
        Assert.Equal("First paragraph", retrieved.Paragraphs[0].Text);
    }

    [Fact]
    public async Task GetParsedDocument_NotFound_ReturnsNull()
    {
        var result = await _storage.GetParsedDocumentAsync("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public async Task ListDocuments_FiltersById()
    {
        var doc1 = new ParsedDocument { Id = "doc-a", FileName = "a.docx", UploadedBy = "user-1" };
        var doc2 = new ParsedDocument { Id = "doc-b", FileName = "b.docx", UploadedBy = "user-2" };

        await _storage.SaveParsedDocumentAsync(doc1);
        await _storage.SaveParsedDocumentAsync(doc2);

        var user1Docs = await _storage.ListDocumentsAsync("user-1");
        var user2Docs = await _storage.ListDocumentsAsync("user-2");

        Assert.Single(user1Docs);
        Assert.Equal("doc-a", user1Docs[0].Id);
        Assert.Single(user2Docs);
        Assert.Equal("doc-b", user2Docs[0].Id);
    }

    [Fact]
    public async Task SaveAndGetExtraction_RoundTrips()
    {
        var extraction = new BiographicalExtraction
        {
            DocumentId = "doc-ext-1",
            ExtractedAt = DateTime.UtcNow,
            SubjectNames = new List<string> { "Jane" },
            Summary = "A test summary.",
            Categories = new ExtractionCategories
            {
                People = new List<ExtractedPerson>
                {
                    new()
                    {
                        Id = "p1",
                        Name = "Bob",
                        Relationship = "friend",
                        Description = "A good friend",
                        SourceRefs = new List<SourceReference> { new(0, "Bob was a friend") }
                    }
                },
                Events = new List<ExtractedEvent>(),
                Places = new List<ExtractedPlace>(),
                Conversations = new List<ExtractedConversation>(),
                Thoughts = new List<ExtractedThought>()
            }
        };

        await _storage.SaveExtractionAsync(extraction);
        var retrieved = await _storage.GetExtractionAsync("doc-ext-1");

        Assert.NotNull(retrieved);
        Assert.Equal("doc-ext-1", retrieved!.DocumentId);
        Assert.Equal("A test summary.", retrieved.Summary);
        Assert.Single(retrieved.Categories.People);
        Assert.Equal("Bob", retrieved.Categories.People[0].Name);
    }

    [Fact]
    public async Task GetExtraction_NotFound_ReturnsNull()
    {
        var result = await _storage.GetExtractionAsync("nonexistent");
        Assert.Null(result);
    }

    [Fact]
    public void ExtractionExists_ReturnsFalseWhenMissing()
    {
        Assert.False(_storage.ExtractionExists("no-such-doc"));
    }

    [Fact]
    public async Task ExtractionExists_ReturnsTrueAfterSave()
    {
        var extraction = new BiographicalExtraction
        {
            DocumentId = "exists-test",
            Categories = new ExtractionCategories()
        };

        await _storage.SaveExtractionAsync(extraction);
        Assert.True(_storage.ExtractionExists("exists-test"));
    }

    [Fact]
    public async Task ListDocuments_IncludesExtractionStatus()
    {
        var doc = new ParsedDocument { Id = "doc-status", FileName = "s.docx", UploadedBy = "user-x" };
        await _storage.SaveParsedDocumentAsync(doc);

        var beforeExtraction = await _storage.ListDocumentsAsync("user-x");
        Assert.False(beforeExtraction[0].HasExtraction);

        await _storage.SaveExtractionAsync(new BiographicalExtraction
        {
            DocumentId = "doc-status",
            Categories = new ExtractionCategories()
        });

        var afterExtraction = await _storage.ListDocumentsAsync("user-x");
        Assert.True(afterExtraction[0].HasExtraction);
    }
}
