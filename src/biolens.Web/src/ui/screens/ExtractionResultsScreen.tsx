import { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { getDocument, getExtraction, extractDocument } from '../../core/api';
import { isAiConfigured } from '../../ai/aiConfig';
import type {
  ParsedDocument,
  BiographicalExtraction,
  CategoryKey,
  ExtractedItem,
  DocumentParagraph,
} from '../../types';
import styles from './Screens.module.css';

const CATEGORY_LABELS: Record<CategoryKey, string> = {
  people: '👤 People',
  events: '📅 Events',
  places: '📍 Places',
  conversations: '💬 Conversations',
  thoughts: '💭 Thoughts',
};

const CATEGORY_COLUMNS: Record<CategoryKey, string[]> = {
  people: ['Name', 'Relationship', 'Description'],
  events: ['Title', 'Date', 'Description'],
  places: ['Name', 'Context', 'Description'],
  conversations: ['Participants', 'Topic', 'Summary'],
  thoughts: ['Topic', 'Content', 'Attribution'],
};

function getItemFields(category: CategoryKey, item: ExtractedItem): string[] {
  switch (category) {
    case 'people': {
      const p = item as any;
      return [p.name, p.relationship, p.description];
    }
    case 'events': {
      const e = item as any;
      return [e.title, e.date, e.description];
    }
    case 'places': {
      const pl = item as any;
      return [pl.name, pl.context, pl.description];
    }
    case 'conversations': {
      const c = item as any;
      return [c.participants, c.topic, c.summary];
    }
    case 'thoughts': {
      const t = item as any;
      return [t.topic, t.content, t.attribution];
    }
  }
}

export function ExtractionResultsScreen() {
  const { id } = useParams<{ id: string }>();
  const [doc, setDoc] = useState<ParsedDocument | null>(null);
  const [extraction, setExtraction] = useState<BiographicalExtraction | null>(null);
  const [loading, setLoading] = useState(true);
  const [extracting, setExtracting] = useState(false);
  const [error, setError] = useState('');
  const [activeCategory, setActiveCategory] = useState<CategoryKey>('people');
  const [selectedItem, setSelectedItem] = useState<ExtractedItem | null>(null);

  const loadData = useCallback(async () => {
    if (!id) return;
    try {
      const docData = await getDocument(id);
      setDoc(docData);

      try {
        const ext = await getExtraction(id);
        setExtraction(ext);
      } catch {
        // No extraction yet — that's fine
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load document');
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => { loadData(); }, [loadData]);

  const handleExtract = async () => {
    if (!id) return;
    if (!isAiConfigured()) {
      setError('AI token not configured. Go to Settings to add your API key.');
      return;
    }

    setExtracting(true);
    setError('');

    try {
      const result = await extractDocument(id);
      setExtraction(result);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Extraction failed');
    } finally {
      setExtracting(false);
    }
  };

  if (loading) return <div className={styles.center}><p>Loading…</p></div>;
  if (error && !doc) return <div className={styles.center}><p className={styles.errorText}>{error}</p></div>;
  if (!doc) return <div className={styles.center}><p>Document not found.</p></div>;

  const items = extraction ? extraction.categories[activeCategory] : [];

  return (
    <div className={styles.page}>
      {/* Header */}
      <div className={styles.pageHeader}>
        <div>
          <Link to="/" className={styles.backLink}>← Documents</Link>
          <h2>{doc.fileName}</h2>
          <p className={styles.meta}>
            Subject: <strong>{doc.subjectNames.join(', ')}</strong> · {doc.totalParagraphs} paragraphs
          </p>
        </div>

        {!extraction && (
          <button
            className={styles.primaryBtn}
            onClick={handleExtract}
            disabled={extracting}
          >
            {extracting ? '🔄 Extracting…' : '🤖 Run AI Extraction'}
          </button>
        )}
      </div>

      {error && <div className={styles.errorBanner} role="alert">{error}</div>}

      {extracting && (
        <div className={styles.extractingBanner}>
          <p>⏳ AI extraction in progress… This may take a minute for large documents.</p>
        </div>
      )}

      {extraction && (
        <>
          {/* Summary */}
          <div className={styles.summaryCard}>
            <h3>Summary</h3>
            <p>{extraction.summary}</p>
          </div>

          {/* Category tabs */}
          <div className={styles.tabBar}>
            {(Object.keys(CATEGORY_LABELS) as CategoryKey[]).map(cat => {
              const count = extraction.categories[cat]?.length || 0;
              return (
                <button
                  key={cat}
                  className={`${styles.tab} ${activeCategory === cat ? styles.tabActive : ''}`}
                  onClick={() => { setActiveCategory(cat); setSelectedItem(null); }}
                >
                  {CATEGORY_LABELS[cat]} ({count})
                </button>
              );
            })}
          </div>

          {/* Results table */}
          {items.length === 0 ? (
            <p className={styles.emptyCategory}>No {activeCategory} found in this document.</p>
          ) : (
            <div className={styles.tableWrap}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    {CATEGORY_COLUMNS[activeCategory].map(col => (
                      <th key={col}>{col}</th>
                    ))}
                    <th>Sources</th>
                  </tr>
                </thead>
                <tbody>
                  {items.map((item: ExtractedItem) => {
                    const fields = getItemFields(activeCategory, item);
                    const isSelected = selectedItem?.id === item.id;
                    return (
                      <tr
                        key={item.id}
                        className={isSelected ? styles.rowSelected : styles.rowClickable}
                        onClick={() => setSelectedItem(isSelected ? null : item)}
                      >
                        {fields.map((f, i) => (
                          <td key={i} className={i === 0 ? styles.cellBold : ''}>
                            {f || '—'}
                          </td>
                        ))}
                        <td>{item.sourceRefs?.length || 0} ref(s)</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}

          {/* Detail panel (drill-down) */}
          {selectedItem && (
            <DetailPanel item={selectedItem} category={activeCategory} paragraphs={doc.paragraphs} />
          )}
        </>
      )}
    </div>
  );
}

/** Drill-down panel showing the selected item's details and source paragraphs */
function DetailPanel({
  item,
  category,
  paragraphs,
}: {
  item: ExtractedItem;
  category: CategoryKey;
  paragraphs: DocumentParagraph[];
}) {
  const fields = getItemFields(category, item);
  const columns = CATEGORY_COLUMNS[category];

  return (
    <div className={styles.detailPanel}>
      <h3 className={styles.detailTitle}>
        {CATEGORY_LABELS[category]} Detail
      </h3>

      <dl className={styles.detailFields}>
        {columns.map((col, i) => (
          <div key={col} className={styles.detailField}>
            <dt>{col}</dt>
            <dd>{fields[i] || '—'}</dd>
          </div>
        ))}
      </dl>

      <h4 className={styles.detailSourcesTitle}>
        Source References ({item.sourceRefs?.length || 0})
      </h4>

      {item.sourceRefs && item.sourceRefs.length > 0 ? (
        <div className={styles.sourceRefs}>
          {item.sourceRefs.map((ref, i) => {
            const para = paragraphs.find(p => p.index === ref.paragraphIndex);
            return (
              <div key={i} className={styles.sourceRef}>
                <div className={styles.sourceRefHeader}>
                  <span className={styles.sourceRefIndex}>¶ {ref.paragraphIndex}</span>
                  <span className={styles.sourceRefSnippet}>"{ref.snippet}"</span>
                </div>
                {para && (
                  <div className={styles.sourceRefFull}>
                    <p className={styles.sourceRefParagraph}>{para.text}</p>
                    <span className={styles.sourceRefStyle}>{para.style}</span>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      ) : (
        <p className={styles.emptyCategory}>No source references available.</p>
      )}
    </div>
  );
}
