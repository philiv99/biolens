import type { AuthResponse, User } from '../types';

const AUTH_API_URL = import.meta.env.VITE_AUTH_API_URL || 'http://localhost:5139';

const STORAGE_USER_KEY = 'biolens:user';
const STORAGE_USERID_KEY = 'biolens:userId';

/** Get stored user data from localStorage */
export function getStoredUser(): User | null {
  try {
    const raw = localStorage.getItem(STORAGE_USER_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
}

/** Get stored userId */
export function getStoredUserId(): string | null {
  return localStorage.getItem(STORAGE_USERID_KEY);
}

/** Save user data to localStorage */
export function storeUser(user: User): void {
  localStorage.setItem(STORAGE_USER_KEY, JSON.stringify(user));
  localStorage.setItem(STORAGE_USERID_KEY, user.id);
}

/** Clear all auth data from localStorage */
export function clearAuthStorage(): void {
  localStorage.removeItem(STORAGE_USER_KEY);
  localStorage.removeItem(STORAGE_USERID_KEY);
  localStorage.removeItem('biolens:ai-token');
  localStorage.removeItem('biolens:ai-model');
  localStorage.removeItem('biolens:ai-baseurl');
}

/** Login with username and password */
export async function login(username: string, password: string): Promise<AuthResponse> {
  const res = await fetch(`${AUTH_API_URL}/api/users/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  });

  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.message || `Login failed (${res.status})`);
  }

  const data: AuthResponse = await res.json();
  if (data.success && data.user) {
    storeUser(data.user);
  }
  return data;
}

/** Register a new account */
export async function register(fields: {
  username: string;
  email: string;
  displayName: string;
  password: string;
  confirmPassword: string;
}): Promise<AuthResponse> {
  const res = await fetch(`${AUTH_API_URL}/api/users/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(fields),
  });

  if (!res.ok) {
    const data = await res.json().catch(() => ({}));
    throw new Error(data.message || `Registration failed (${res.status})`);
  }

  const data: AuthResponse = await res.json();
  if (data.success && data.user) {
    storeUser(data.user);
  }
  return data;
}

/** Validate the stored session by calling /api/users/me */
export async function validateSession(userId: string): Promise<User | null> {
  try {
    const res = await fetch(`${AUTH_API_URL}/api/users/me`, {
      headers: { 'X-User-Id': userId },
    });

    if (!res.ok) return null;

    const user: User = await res.json();
    storeUser(user);
    return user;
  } catch {
    return null;
  }
}
