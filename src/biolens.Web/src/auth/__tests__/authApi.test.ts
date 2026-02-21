import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import {
  getStoredUser,
  getStoredUserId,
  storeUser,
  clearAuthStorage,
} from '../authApi';
import type { User } from '../../types';

const mockUser: User = {
  id: 'user-123',
  username: 'testuser',
  email: 'test@example.com',
  displayName: 'Test User',
  role: 'Player',
  avatarType: 'Default',
  avatarData: null,
  isActive: true,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
  lastLoginAt: null,
};

describe('authApi storage', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  describe('getStoredUser', () => {
    it('returns null when no user stored', () => {
      expect(getStoredUser()).toBeNull();
    });

    it('returns stored user', () => {
      storeUser(mockUser);
      const user = getStoredUser();
      expect(user).not.toBeNull();
      expect(user!.id).toBe('user-123');
      expect(user!.username).toBe('testuser');
    });

    it('returns null on corrupted data', () => {
      localStorage.setItem('biolens:user', 'not-json');
      expect(getStoredUser()).toBeNull();
    });
  });

  describe('getStoredUserId', () => {
    it('returns null when no user stored', () => {
      expect(getStoredUserId()).toBeNull();
    });

    it('returns userId after storeUser', () => {
      storeUser(mockUser);
      expect(getStoredUserId()).toBe('user-123');
    });
  });

  describe('storeUser', () => {
    it('stores user data and userId', () => {
      storeUser(mockUser);
      expect(localStorage.getItem('biolens:user')).not.toBeNull();
      expect(localStorage.getItem('biolens:userId')).toBe('user-123');
    });
  });

  describe('clearAuthStorage', () => {
    it('clears all auth keys', () => {
      storeUser(mockUser);
      localStorage.setItem('biolens:ai-token', 'sk-test');
      localStorage.setItem('biolens:ai-model', 'gpt-4');
      localStorage.setItem('biolens:ai-baseurl', 'http://localhost');

      clearAuthStorage();

      expect(localStorage.getItem('biolens:user')).toBeNull();
      expect(localStorage.getItem('biolens:userId')).toBeNull();
      expect(localStorage.getItem('biolens:ai-token')).toBeNull();
    });
  });
});

describe('authApi login', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    localStorage.clear();
  });

  it('login stores user on success', async () => {
    const { login } = await import('../authApi');

    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: true,
        json: async () => ({ success: true, message: '', user: mockUser }),
      }),
    );

    const result = await login('testuser', 'password');
    expect(result.success).toBe(true);
    expect(result.user.id).toBe('user-123');
    expect(getStoredUserId()).toBe('user-123');
  });

  it('login throws on failure', async () => {
    const { login } = await import('../authApi');

    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue({
        ok: false,
        json: async () => ({ message: 'Invalid credentials' }),
      }),
    );

    await expect(login('bad', 'creds')).rejects.toThrow('Invalid credentials');
  });
});
