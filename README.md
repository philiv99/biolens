# BioLens

**Extract structured biographical data from Word documents using AI.**

BioLens lets you upload `.docx` files containing biographical content, specify the subject's names and aliases, and extract structured information — people, events, conversations, places, and thoughts — presented in a searchable table with drill-down to the original document source.

## Features

- 📄 Upload `.docx` Word documents
- 👤 Specify subject names/aliases (comma-separated)
- 🤖 AI-powered biographical data extraction (user-provided LLM API key)
- 📊 Tabular results grouped by category (people, events, places, conversations, thoughts)
- 🔍 Drill-down from any extracted item to its source paragraph in the document
- 🔐 User authentication via CopilotSdk.Api

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [CopilotSdk.Api](http://localhost:5139) running for authentication
- An OpenAI-compatible API key for AI extraction features

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/philiv99/biolens.git
cd biolens
```

### 2. Configure Environment

```bash
cp .env.example .env
# Edit .env with your values
```

### 3. Run the Backend

```bash
cd src/biolens.Api
dotnet run
# API starts at http://localhost:5180
```

### 4. Run the Frontend

```bash
cd src/biolens.Web
npm install
npm run dev
# App starts at http://localhost:5173
```

### 5. CORS Configuration

Ensure CopilotSdk.Api allows `http://localhost:5173` in its CORS policy.
The BioLens API allows the frontend origin by default in development.

## Authentication

BioLens requires authentication. Users must log in before accessing any features.
Authentication is handled by CopilotSdk.Api running at the URL configured in `VITE_AUTH_API_URL`.

### User Roles

| Role | Access |
|------|--------|
| Player | Upload documents, run extractions, view results |
| Creator | All Player access + manage documents |
| Admin | Full access including all documents |

## AI Integration

BioLens uses an OpenAI-compatible LLM API to extract biographical data from documents.
Users must provide their own API token via the Settings screen. Tokens are stored locally
and sent per-request to the backend, which proxies the LLM call.

**Supported providers:** Any OpenAI-compatible chat completion API (OpenAI, Azure OpenAI, local models via LM Studio, Ollama with OpenAI compatibility, etc.)

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | React 18 + Vite + TypeScript |
| Backend | .NET 8 C# Web API |
| Auth | CopilotSdk.Api |
| AI | OpenAI-compatible chat completion |
| Storage | Flat JSON files |
| Doc Parsing | DocumentFormat.OpenXml |

## Project Structure

```
biolens/
├── docs/                          # Documentation
├── src/
│   ├── biolens.Api/               # .NET Web API backend
│   │   ├── Controllers/           # API endpoints
│   │   ├── Services/              # Business logic
│   │   ├── Models/                # Data models
│   │   └── Storage/               # Flat-file persistence
│   └── biolens.Web/               # React frontend
│       └── src/
│           ├── auth/              # Authentication module
│           ├── ai/                # AI token management
│           ├── ui/                # Components & screens
│           └── app/               # Routing & layout
├── storage/                       # Runtime data (gitignored)
│   ├── uploads/                   # Original .docx files
│   ├── parsed/                    # Parsed document JSON
│   └── extractions/               # Extraction result JSON
└── tests/                         # Test projects
```

## Testing

```bash
# Backend tests
cd tests/biolens.Api.Tests
dotnet test

# Frontend tests
cd src/biolens.Web
npm test
```

## App Identity

- **App Name:** BioLens
- **Repo Slug:** biolens
- **Storage Prefix:** `biolens:`
