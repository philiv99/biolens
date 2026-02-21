namespace biolens.Api.Models;

/// <summary>A reference back to a source paragraph in the original document.</summary>
public record SourceReference(
    int ParagraphIndex,
    string Snippet
);

/// <summary>A person mentioned in the document.</summary>
public class ExtractedPerson
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SourceReference> SourceRefs { get; set; } = new();
}

/// <summary>An event described in the document.</summary>
public class ExtractedEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SourceReference> SourceRefs { get; set; } = new();
}

/// <summary>A place mentioned in the document.</summary>
public class ExtractedPlace
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<SourceReference> SourceRefs { get; set; } = new();
}

/// <summary>A conversation or dialogue described in the document.</summary>
public class ExtractedConversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Participants { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<SourceReference> SourceRefs { get; set; } = new();
}

/// <summary>A thought, opinion, or reflection expressed in the document.</summary>
public class ExtractedThought
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Topic { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Attribution { get; set; } = string.Empty;
    public List<SourceReference> SourceRefs { get; set; } = new();
}

/// <summary>Container for all extraction categories.</summary>
public class ExtractionCategories
{
    public List<ExtractedPerson> People { get; set; } = new();
    public List<ExtractedEvent> Events { get; set; } = new();
    public List<ExtractedPlace> Places { get; set; } = new();
    public List<ExtractedConversation> Conversations { get; set; } = new();
    public List<ExtractedThought> Thoughts { get; set; } = new();
}

/// <summary>Full extraction result stored as flat JSON.</summary>
public class BiographicalExtraction
{
    public string DocumentId { get; set; } = string.Empty;
    public DateTime ExtractedAt { get; set; } = DateTime.UtcNow;
    public List<string> SubjectNames { get; set; } = new();
    public ExtractionCategories Categories { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

/// <summary>Request to trigger extraction. AI token passed in header.</summary>
public class ExtractRequest
{
    public string? Model { get; set; }
    public string? ApiBaseUrl { get; set; }
}
