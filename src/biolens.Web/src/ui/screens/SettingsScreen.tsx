import { useState, type FormEvent } from 'react';
import {
  getAiToken, setAiToken, clearAiToken,
  getAiModel, setAiModel,
  getAiBaseUrl, setAiBaseUrl,
} from '../../ai/aiConfig';
import styles from './Screens.module.css';

export function SettingsScreen() {
  const [token, setToken] = useState(getAiToken() || '');
  const [model, setModel] = useState(getAiModel());
  const [baseUrl, setBaseUrl] = useState(getAiBaseUrl());
  const [saved, setSaved] = useState(false);

  const handleSave = (e: FormEvent) => {
    e.preventDefault();
    if (token.trim()) {
      setAiToken(token.trim());
    } else {
      clearAiToken();
    }
    setAiModel(model.trim() || 'gpt-4o-mini');
    setAiBaseUrl(baseUrl.trim() || 'https://api.openai.com/v1');
    setSaved(true);
    setTimeout(() => setSaved(false), 3000);
  };

  const handleClear = () => {
    clearAiToken();
    setToken('');
    setSaved(false);
  };

  return (
    <div className={styles.page}>
      <div className={styles.pageHeader}>
        <h2>AI Settings</h2>
      </div>

      <div className={styles.settingsCard}>
        <p className={styles.settingsDesc}>
          BioLens uses an OpenAI-compatible API to extract biographical data from documents.
          Enter your API key below. Your key is stored only in your browser's local storage
          and sent per-request to the BioLens backend, which proxies the LLM call.
        </p>

        <form onSubmit={handleSave} className={styles.uploadForm}>
          <div className={styles.field}>
            <label htmlFor="ai-token" className={styles.fieldLabel}>API Token</label>
            <input
              id="ai-token"
              type="password"
              className={styles.fieldInput}
              value={token}
              onChange={e => setToken(e.target.value)}
              placeholder="sk-..."
              autoComplete="off"
            />
            <p className={styles.fieldHint}>
              Your OpenAI (or compatible) API key. Never shared except for extraction requests.
            </p>
          </div>

          <div className={styles.field}>
            <label htmlFor="ai-model" className={styles.fieldLabel}>Model</label>
            <input
              id="ai-model"
              type="text"
              className={styles.fieldInput}
              value={model}
              onChange={e => setModel(e.target.value)}
              placeholder="gpt-4o-mini"
            />
            <p className={styles.fieldHint}>
              The model name to use for extraction (e.g. gpt-4o-mini, gpt-4o, gpt-4-turbo).
            </p>
          </div>

          <div className={styles.field}>
            <label htmlFor="ai-baseurl" className={styles.fieldLabel}>API Base URL</label>
            <input
              id="ai-baseurl"
              type="url"
              className={styles.fieldInput}
              value={baseUrl}
              onChange={e => setBaseUrl(e.target.value)}
              placeholder="https://api.openai.com/v1"
            />
            <p className={styles.fieldHint}>
              Override for non-OpenAI providers (Azure, local models, etc.). Default: OpenAI.
            </p>
          </div>

          <div className={styles.buttonRow}>
            <button type="submit" className={styles.primaryBtn}>
              Save Settings
            </button>
            <button type="button" className={styles.dangerBtn} onClick={handleClear}>
              Clear Token
            </button>
          </div>

          {saved && (
            <div className={styles.successBanner}>✅ Settings saved!</div>
          )}
        </form>
      </div>
    </div>
  );
}
