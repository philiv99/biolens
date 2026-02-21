import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { listDocuments } from '../../core/api';
import type { DocumentSummary } from '../../types';
import styles from './Screens.module.css';

export function DocumentListScreen() {
  const navigate = useNavigate();
  const [docs, setDocs] = useState<DocumentSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    listDocuments()
      .then(setDocs)
      .catch(err => setError(err.message))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className={styles.center}><p>Loading documents…</p></div>;
  if (error) return <div className={styles.center}><p className={styles.errorText}>{error}</p></div>;

  return (
    <div className={styles.page}>
      <div className={styles.pageHeader}>
        <h2>Documents</h2>
        <button className={styles.primaryBtn} onClick={() => navigate('/upload')}>
          + Upload Document
        </button>
      </div>

      {docs.length === 0 ? (
        <div className={styles.emptyState}>
          <p>No documents yet.</p>
          <p>Upload a .docx file to get started with biographical extraction.</p>
          <Link to="/upload" className={styles.primaryBtn} style={{ display: 'inline-block', marginTop: '1rem' }}>
            Upload Your First Document
          </Link>
        </div>
      ) : (
        <div className={styles.tableWrap}>
          <table className={styles.table}>
            <thead>
              <tr>
                <th>File Name</th>
                <th>Subject Names</th>
                <th>Paragraphs</th>
                <th>Uploaded</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {docs.map(doc => (
                <tr key={doc.id}>
                  <td className={styles.fileName}>{doc.fileName}</td>
                  <td>{doc.subjectNames.join(', ')}</td>
                  <td>{doc.totalParagraphs}</td>
                  <td>{new Date(doc.uploadedAt).toLocaleDateString()}</td>
                  <td>
                    {doc.hasExtraction ? (
                      <span className={styles.badge + ' ' + styles.badgeSuccess}>Extracted</span>
                    ) : (
                      <span className={styles.badge + ' ' + styles.badgePending}>Pending</span>
                    )}
                  </td>
                  <td>
                    <Link to={`/documents/${doc.id}`} className={styles.linkBtn}>
                      View →
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
