import { describe, it, expect, beforeEach } from 'vitest';
import {
  getAiToken,
  setAiToken,
  clearAiToken,
  getAiModel,
  setAiModel,
  getAiBaseUrl,
  setAiBaseUrl,
  isAiConfigured,
} from '../aiConfig';

describe('aiConfig', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  describe('AI Token', () => {
    it('returns null when no token is stored', () => {
      expect(getAiToken()).toBeNull();
    });

    it('stores and retrieves a token', () => {
      setAiToken('sk-test-123');
      expect(getAiToken()).toBe('sk-test-123');
    });

    it('clears the token', () => {
      setAiToken('sk-test');
      clearAiToken();
      expect(getAiToken()).toBeNull();
    });
  });

  describe('AI Model', () => {
    it('returns default model when none is stored', () => {
      expect(getAiModel()).toBe('gpt-4o-mini');
    });

    it('stores and retrieves a custom model', () => {
      setAiModel('gpt-4o');
      expect(getAiModel()).toBe('gpt-4o');
    });
  });

  describe('AI Base URL', () => {
    it('returns default URL when none is stored', () => {
      expect(getAiBaseUrl()).toBe('https://api.openai.com/v1');
    });

    it('stores and retrieves a custom URL', () => {
      setAiBaseUrl('http://localhost:11434/v1');
      expect(getAiBaseUrl()).toBe('http://localhost:11434/v1');
    });
  });

  describe('isAiConfigured', () => {
    it('returns false when no token', () => {
      expect(isAiConfigured()).toBe(false);
    });

    it('returns true when token is set', () => {
      setAiToken('sk-abc');
      expect(isAiConfigured()).toBe(true);
    });
  });
});
