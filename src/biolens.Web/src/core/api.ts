import type {
  DocumentSummary,
  ParsedDocument,
  BiographicalExtraction,
} from '../types';
import { getStoredUserId } from '../auth/authApi';
import { getAiToken, getAiModel, getAiBaseUrl } from '../ai/aiConfig';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5180';

function authHeaders(): Record<string, string> {
  const userId = getStoredUserId();
  if (!userId) throw new Error('Not authenticated');
  return { 'X-User-Id': userId };
}

/** Upload a .docx file with subject names. */
export async function uploadDocument(
  file: File,
  subjectNames: string,
): Promise<DocumentSummary> {
  const form = new FormData();
  form.append('file', file);
  form.append('subjectNames', subjectNames);

  const res = await fetch(`${API_URL}/api/documents/upload`, {
    method: 'POST',
    headers: authHeaders(),
    body: form,
  });

  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.error || `Upload failed (${res.status})`);
  }

  return res.json();
}

/** List all documents for the current user. */
export async function listDocuments(): Promise<DocumentSummary[]> {
  const res = await fetch(`${API_URL}/api/documents`, {
    headers: authHeaders(),
  });

  if (!res.ok) throw new Error(`Failed to list documents (${res.status})`);
  return res.json();
}

/** Get a specific document with its paragraphs. */
export async function getDocument(id: string): Promise<ParsedDocument> {
  const res = await fetch(`${API_URL}/api/documents/${id}`, {
    headers: authHeaders(),
  });

  if (!res.ok) throw new Error(`Failed to load document (${res.status})`);
  return res.json();
}

/** Trigger AI extraction on a document. */
export async function extractDocument(id: string): Promise<BiographicalExtraction> {
  const aiToken = getAiToken();
  if (!aiToken) throw new Error('AI token is not configured. Go to Settings to add your API key.');

  const res = await fetch(`${API_URL}/api/documents/${id}/extract`, {
    method: 'POST',
    headers: {
      ...authHeaders(),
      'X-AI-Token': aiToken,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({
      model: getAiModel(),
      apiBaseUrl: getAiBaseUrl(),
    }),
  });

  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.error || `Extraction failed (${res.status})`);
  }

  return res.json();
}

/** Get existing extraction results for a document. */
export async function getExtraction(id: string): Promise<BiographicalExtraction> {
  const res = await fetch(`${API_URL}/api/documents/${id}/extraction`, {
    headers: authHeaders(),
  });

  if (!res.ok) throw new Error(`No extraction found (${res.status})`);
  return res.json();
}
