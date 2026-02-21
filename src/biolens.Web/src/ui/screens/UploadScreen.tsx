import { useState, useRef, type FormEvent, type DragEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { uploadDocument } from '../../core/api';
import { isAiConfigured } from '../../ai/aiConfig';
import styles from './Screens.module.css';

export function UploadScreen() {
  const navigate = useNavigate();
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [subjectNames, setSubjectNames] = useState('');
  const [dragOver, setDragOver] = useState(false);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');

  const handleDrop = (e: DragEvent) => {
    e.preventDefault();
    setDragOver(false);
    const dropped = e.dataTransfer.files[0];
    if (dropped && dropped.name.endsWith('.docx')) {
      setFile(dropped);
      setError('');
    } else {
      setError('Only .docx files are supported.');
    }
  };

  const handleFileSelect = (files: FileList | null) => {
    if (files && files[0]) {
      if (files[0].name.endsWith('.docx')) {
        setFile(files[0]);
        setError('');
      } else {
        setError('Only .docx files are supported.');
      }
    }
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!file) { setError('Please select a .docx file.'); return; }
    if (!subjectNames.trim()) { setError('Please enter at least one subject name.'); return; }

    setError('');
    setUploading(true);

    try {
      const doc = await uploadDocument(file, subjectNames);
      navigate(`/documents/${doc.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Upload failed');
    } finally {
      setUploading(false);
    }
  };

  return (
    <div className={styles.page}>
      <div className={styles.pageHeader}>
        <h2>Upload Document</h2>
      </div>

      {!isAiConfigured() && (
        <div className={styles.warningBanner}>
          ⚠️ AI extraction requires an API token.{' '}
          <a href="/settings" className={styles.link}>Configure it in Settings</a> before extracting.
        </div>
      )}

      <form onSubmit={handleSubmit} className={styles.uploadForm}>
        {error && (
          <div className={styles.errorBanner} role="alert" aria-live="assertive">{error}</div>
        )}

        {/* Drop zone */}
        <div
          className={`${styles.dropZone} ${dragOver ? styles.dropZoneActive : ''} ${file ? styles.dropZoneHasFile : ''}`}
          onDragOver={e => { e.preventDefault(); setDragOver(true); }}
          onDragLeave={() => setDragOver(false)}
          onDrop={handleDrop}
          onClick={() => fileInputRef.current?.click()}
          role="button"
          tabIndex={0}
          onKeyDown={e => { if (e.key === 'Enter' || e.key === ' ') fileInputRef.current?.click(); }}
          aria-label="Click or drag to upload a .docx file"
        >
          <input
            ref={fileInputRef}
            type="file"
            accept=".docx"
            onChange={e => handleFileSelect(e.target.files)}
            style={{ display: 'none' }}
          />
          {file ? (
            <div>
              <p className={styles.dropZoneIcon}>📄</p>
              <p className={styles.dropZoneText}>{file.name}</p>
              <p className={styles.dropZoneHint}>{(file.size / 1024).toFixed(1)} KB — Click to change</p>
            </div>
          ) : (
            <div>
              <p className={styles.dropZoneIcon}>📁</p>
              <p className={styles.dropZoneText}>Drop a .docx file here</p>
              <p className={styles.dropZoneHint}>or click to browse</p>
            </div>
          )}
        </div>

        {/* Subject names */}
        <div className={styles.field}>
          <label htmlFor="subjectNames" className={styles.fieldLabel}>
            Subject Names / Aliases
          </label>
          <input
            id="subjectNames"
            type="text"
            className={styles.fieldInput}
            value={subjectNames}
            onChange={e => setSubjectNames(e.target.value)}
            placeholder="e.g. John Smith, Johnny, J.S."
            required
          />
          <p className={styles.fieldHint}>
            Comma-separated list of names, nicknames, and aliases for the person the document is about.
          </p>
        </div>

        <button type="submit" className={styles.primaryBtn} disabled={uploading}>
          {uploading ? 'Uploading…' : 'Upload & Parse Document'}
        </button>
      </form>
    </div>
  );
}
