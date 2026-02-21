namespace biolens.Api.Services;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using biolens.Api.Models;

/// <summary>
/// Extracts structured biographical data from parsed document text using an
/// OpenAI-compatible chat completion API. The user's AI token is passed per-request.
/// </summary>
public interface IExtractionService
{
    Task<BiographicalExtraction> ExtractAsync(
        ParsedDocument document,
        string aiToken,
        string? model = null,
        string? apiBaseUrl = null,
        CancellationToken ct = default);
}

public class AiExtractionService : IExtractionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiExtractionService> _logger;

    private const string DefaultModel = "gpt-4o-mini";
    private const string DefaultApiBase = "https://api.openai.com/v1";
    private const int MaxParagraphsPerChunk = 40;

    public AiExtractionService(IHttpClientFactory httpClientFactory, ILogger<AiExtractionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<BiographicalExtraction> ExtractAsync(
        ParsedDocument document,
        string aiToken,
        string? model = null,
        string? apiBaseUrl = null,
        CancellationToken ct = default)
    {
        model ??= DefaultModel;
        apiBaseUrl ??= DefaultApiBase;

        var allCategories = new ExtractionCategories();
        var summaryParts = new List<string>();

        // Process in chunks to handle large documents
        var chunks = ChunkParagraphs(document.Paragraphs, MaxParagraphsPerChunk);

        _logger.LogInformation(
            "Extracting from document {DocId}: {ChunkCount} chunks, model={Model}",
            document.Id, chunks.Count, model);

        for (int i = 0; i < chunks.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            _logger.LogInformation("Processing chunk {Chunk}/{Total}", i + 1, chunks.Count);

            var chunkResult = await ExtractChunkAsync(
                document.SubjectNames, chunks[i], aiToken, model, apiBaseUrl, ct);

            if (chunkResult != null)
            {
                MergeCategories(allCategories, chunkResult.Categories);
                if (!string.IsNullOrWhiteSpace(chunkResult.Summary))
                    summaryParts.Add(chunkResult.Summary);
            }
        }

        // If multiple chunks, generate an overall summary
        var finalSummary = summaryParts.Count switch
        {
            0 => "No biographical data could be extracted from this document.",
            1 => summaryParts[0],
            _ => string.Join(" ", summaryParts)
        };

        return new BiographicalExtraction
        {
            DocumentId = document.Id,
            ExtractedAt = DateTime.UtcNow,
            SubjectNames = document.SubjectNames,
            Categories = allCategories,
            Summary = finalSummary
        };
    }

    private async Task<BiographicalExtraction?> ExtractChunkAsync(
        List<string> subjectNames,
        List<DocumentParagraph> paragraphs,
        string aiToken,
        string model,
        string apiBaseUrl,
        CancellationToken ct)
    {
        var systemPrompt = BuildSystemPrompt(subjectNames);
        var userContent = BuildUserContent(paragraphs);

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            },
            temperature = 0.2,
            response_format = new { type = "json_object" }
        };

        var client = _httpClientFactory.CreateClient();
        var url = $"{apiBaseUrl.TrimEnd('/')}/chat/completions";

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", aiToken);

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call AI API at {Url}", url);
            throw new InvalidOperationException($"Failed to connect to AI API: {ex.Message}", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            // Sanitize error body — truncate and avoid logging potential token data
            var safeError = errorBody.Length > 200 ? errorBody[..200] + "..." : errorBody;
            _logger.LogError("AI API returned {Status}: {Body}", response.StatusCode, safeError);
            throw new InvalidOperationException(
                $"AI API returned {(int)response.StatusCode}. Check your API token and settings.");
        }

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        return ParseAiResponse(responseJson);
    }

    private BiographicalExtraction? ParseAiResponse(string responseJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseJson);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
                return null;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<BiographicalExtraction>(content, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI response");
            return null;
        }
    }

    private static string BuildSystemPrompt(List<string> subjectNames)
    {
        var names = string.Join(", ", subjectNames);
        return $@"You are a biographical data extraction assistant. The document is about or written by a person known by these names/aliases: {names}.

Extract structured biographical information and return ONLY valid JSON matching this exact schema:

{{
  ""documentId"": """",
  ""extractedAt"": """",
  ""subjectNames"": [],
  ""categories"": {{
    ""people"": [
      {{
        ""id"": ""unique-id"",
        ""name"": ""person name"",
        ""relationship"": ""relationship to subject"",
        ""description"": ""brief description of this person's role/relevance"",
        ""sourceRefs"": [{{ ""paragraphIndex"": 0, ""snippet"": ""relevant quote from text"" }}]
      }}
    ],
    ""events"": [
      {{
        ""id"": ""unique-id"",
        ""title"": ""event title"",
        ""date"": ""date or time period if mentioned"",
        ""description"": ""what happened"",
        ""sourceRefs"": [{{ ""paragraphIndex"": 0, ""snippet"": ""relevant quote"" }}]
      }}
    ],
    ""places"": [
      {{
        ""id"": ""unique-id"",
        ""name"": ""place name"",
        ""context"": ""why this place is mentioned"",
        ""description"": ""details about the place"",
        ""sourceRefs"": [{{ ""paragraphIndex"": 0, ""snippet"": ""relevant quote"" }}]
      }}
    ],
    ""conversations"": [
      {{
        ""id"": ""unique-id"",
        ""participants"": ""who was involved"",
        ""topic"": ""what was discussed"",
        ""summary"": ""brief summary of the conversation"",
        ""sourceRefs"": [{{ ""paragraphIndex"": 0, ""snippet"": ""relevant quote"" }}]
      }}
    ],
    ""thoughts"": [
      {{
        ""id"": ""unique-id"",
        ""topic"": ""what the thought is about"",
        ""content"": ""the thought, opinion, or reflection"",
        ""attribution"": ""who expressed this thought"",
        ""sourceRefs"": [{{ ""paragraphIndex"": 0, ""snippet"": ""relevant quote"" }}]
      }}
    ]
  }},
  ""summary"": ""A 2-3 sentence summary of the biographical content in this section.""
}}

Rules:
- Extract ALL people, events, places, conversations, and thoughts you can identify.
- The paragraphIndex in sourceRefs MUST match the paragraph index numbers provided in the input.
- The snippet should be a short direct quote (max 100 chars) from the source paragraph.
- Generate unique IDs (use simple format like ""person-1"", ""event-1"", etc.).
- If a category has no items, return an empty array for it.
- Return ONLY the JSON object. No markdown, no explanation.";
    }

    private static string BuildUserContent(List<DocumentParagraph> paragraphs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Extract biographical data from the following document paragraphs:");
        sb.AppendLine();

        foreach (var p in paragraphs)
        {
            sb.AppendLine($"[Paragraph {p.Index}] ({p.Style}): {p.Text}");
        }

        return sb.ToString();
    }

    private static List<List<DocumentParagraph>> ChunkParagraphs(List<DocumentParagraph> paragraphs, int chunkSize)
    {
        var chunks = new List<List<DocumentParagraph>>();
        for (int i = 0; i < paragraphs.Count; i += chunkSize)
        {
            chunks.Add(paragraphs.GetRange(i, Math.Min(chunkSize, paragraphs.Count - i)));
        }
        return chunks;
    }

    private static void MergeCategories(ExtractionCategories target, ExtractionCategories source)
    {
        if (source == null) return;
        target.People.AddRange(source.People ?? new());
        target.Events.AddRange(source.Events ?? new());
        target.Places.AddRange(source.Places ?? new());
        target.Conversations.AddRange(source.Conversations ?? new());
        target.Thoughts.AddRange(source.Thoughts ?? new());
    }
}
