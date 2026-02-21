const TOKEN_KEY = 'biolens:ai-token';
const MODEL_KEY = 'biolens:ai-model';
const BASEURL_KEY = 'biolens:ai-baseurl';

const DEFAULT_MODEL = 'gpt-4o-mini';
const DEFAULT_BASE_URL = 'https://api.openai.com/v1';

export function getAiToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setAiToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearAiToken(): void {
  localStorage.removeItem(TOKEN_KEY);
}

export function getAiModel(): string {
  return localStorage.getItem(MODEL_KEY) || DEFAULT_MODEL;
}

export function setAiModel(model: string): void {
  localStorage.setItem(MODEL_KEY, model);
}

export function getAiBaseUrl(): string {
  return localStorage.getItem(BASEURL_KEY) || DEFAULT_BASE_URL;
}

export function setAiBaseUrl(url: string): void {
  localStorage.setItem(BASEURL_KEY, url);
}

export function isAiConfigured(): boolean {
  return !!getAiToken();
}
