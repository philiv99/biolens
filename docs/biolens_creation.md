# BioLens — Creation Plan

## App Identity

- **App Name:** BioLens
- **Repo Slug:** biolens
- **Storage Prefix:** `biolens:` (for client-side keys)
- **Display Title:** BioLens
- **Short ID:** biolens
- **Domain Type:** web-app (document analysis tool)
- **GitHub:** https://github.com/philiv99/biolens

---

## Technology Stack

| Layer | Technology | Notes |
|-------|-----------|-------|
| Frontend | React 18 + Vite + TypeScript | SPA with file upload and data explorer |
| Backend | .NET 8 C# Web API | Document parsing, AI extraction, flat-file storage |
| Auth | CopilotSdk.Api | External auth service at `localhost:5139` |
| AI/LLM | OpenAI-compatible API | User-provided token; chat completion for extraction |
| Storage | Flat JSON files | No database; `storage/` directory hierarchy |
| Doc Parsing | DocumentFormat.OpenXml | .docx → paragraph-level text with metadata |

---

## Stage 1: MVP

### Phase 1 — Scaffolding & Auth

- [x] 1.1 Initialize repo, create directory structure, .gitignore, .env.example
- [ ] 1.2 Create .NET Web API project (biolens.Api)
- [ ] 1.3 Scaffold React + Vite + TypeScript frontend (biolens.Web)
- [ ] 1.4 Implement auth module (authApi, AuthContext, ProtectedRoute, LoginScreen, RegisterScreen)
- [ ] 1.5 Configure CORS on backend, Vite proxy for dev

### Phase 2 — Document Upload & Parsing

- [ ] 2.1 Build file upload endpoint `POST /api/documents/upload` (accepts .docx + subject names)
- [ ] 2.2 Implement .docx parser service (paragraphs → text blocks with index/position)
- [ ] 2.3 Store parsed document as flat JSON (`storage/parsed/{docId}.json`)
- [ ] 2.4 Build `GET /api/documents` and `GET /api/documents/{id}` endpoints

### Phase 3 — AI Extraction Pipeline

- [ ] 3.1 Define extraction data model (BiographicalExtraction: people, events, places, conversations, thoughts)
- [ ] 3.2 Build AI extraction service (sends parsed text to LLM, returns structured data)
- [ ] 3.3 Build `POST /api/documents/{id}/extract` endpoint
- [ ] 3.4 Store extraction results as flat JSON (`storage/extractions/{docId}.json`)
- [ ] 3.5 Build `GET /api/documents/{id}/extraction` endpoint

### Phase 4 — Results UI

- [ ] 4.1 Build upload screen (file picker + name/alias input + AI token config)
- [ ] 4.2 Build document list screen
- [ ] 4.3 Build extraction results table (tabular summary grouped by category)
- [ ] 4.4 Build drill-down detail view with document source reference
- [ ] 4.5 AI settings panel for token management
- [ ] 4.6 App layout with nav, user info, logout

### Phase 5 — Testing & Quality

- [ ] 5.1 Unit tests for document parsing service
- [ ] 5.2 Unit tests for extraction models and service
- [ ] 5.3 Auth module tests (login, logout, protected route)
- [ ] 5.4 Component tests for Upload and Results screens
- [ ] 5.5 Code review
- [ ] 5.6 Security review
- [ ] 5.7 QA sign-off

---

## Stage 2: Feature Expansion (Future)

- [ ] Search/filter across extracted data
- [ ] Multi-document support per subject
- [ ] Export extracted data (CSV/JSON download)
- [ ] Role-based features (Creator manages extractions, Admin panel)
- [ ] Document source viewer with paragraph highlighting

## Stage 3: Extensibility & Polish (Future)

- [ ] PDF support
- [ ] Batch document processing
- [ ] Extraction template customization
- [ ] Performance optimization for large docs
- [ ] Full accessibility audit

---

## Data Models

### ParsedDocument (stored as `storage/parsed/{docId}.json`)
```json
{
  "id": "guid",
  "fileName": "memoir.docx",
  "subjectNames": ["John Smith", "Johnny", "J.S."],
  "uploadedBy": "userId",
  "uploadedAt": "ISO-date",
  "paragraphs": [
    { "index": 0, "text": "paragraph text...", "style": "Normal" },
    { "index": 1, "text": "...", "style": "Heading1" }
  ],
  "totalParagraphs": 150
}
```

### BiographicalExtraction (stored as `storage/extractions/{docId}.json`)
```json
{
  "documentId": "guid",
  "extractedAt": "ISO-date",
  "subjectNames": ["John Smith", "Johnny"],
  "categories": {
    "people": [
      {
        "id": "guid",
        "name": "Jane Doe",
        "relationship": "wife",
        "description": "Married in 1985...",
        "sourceRefs": [{ "paragraphIndex": 12, "snippet": "...married Jane..." }]
      }
    ],
    "events": [
      {
        "id": "guid",
        "title": "Graduated from MIT",
        "date": "1980",
        "description": "...",
        "sourceRefs": [{ "paragraphIndex": 5, "snippet": "..." }]
      }
    ],
    "places": [...],
    "conversations": [...],
    "thoughts": [...]
  },
  "summary": "Brief overall summary of document content."
}
```

---

## API Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/api/documents/upload` | Upload .docx + subject names |
| GET | `/api/documents` | List user's documents |
| GET | `/api/documents/{id}` | Get document metadata + paragraphs |
| POST | `/api/documents/{id}/extract` | Run AI extraction (requires AI token in header) |
| GET | `/api/documents/{id}/extraction` | Get extraction results |

---

## Routing Map (Frontend)

| Path | Component | Auth |
|------|-----------|------|
| `/login` | LoginScreen | Public |
| `/register` | RegisterScreen | Public |
| `/` | DocumentList (home) | Protected |
| `/upload` | UploadScreen | Protected |
| `/documents/:id` | ExtractionResults | Protected |
| `/documents/:id/detail/:category/:itemId` | DetailView | Protected |
| `/settings` | AISettingsPanel | Protected |

---

## Notes & Decisions

- **2026-02-21**: Project created. Chose flat-file JSON storage over database per user request. MySQL can be added later.
- **2026-02-21**: AI extraction uses user-provided OpenAI-compatible token. Backend proxies the LLM call to keep token handling server-side per request.
- **2026-02-21**: Document paragraphs are indexed at parse time to enable source-reference drill-down from extraction results.
