import { useState, useEffect } from 'react';
import { useApi } from '../../hooks/useApi';
import '../../styles/defects.css';

const STATUSES = ['Open', 'In Progress', 'Resolved', 'Closed'];

interface Defect {
  id: string;
  title: string;
  description: string;
  severity: string;
  status: string;
  testCaseId: string;
  createdAt: string;
}

interface CaseInfo {
  id: string;
  title: string;
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

export function DefectsTab({ projectId }: { projectId: string }) {
  const [defects, setDefects] = useState<Defect[]>([]);
  const [caseTitles, setCaseTitles] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState('All');
  const [updatingId, setUpdatingId] = useState<string | null>(null);
  const { apiCall } = useApi();

  useEffect(() => {
    const load = async () => {
      try {
        const [defectData, caseData] = await Promise.all([
          apiCall(`/api/v1/projects/${projectId}/defects?limit=200`),
          apiCall(`/api/v1/projects/${projectId}/test-cases?limit=200`),
        ]);
        setDefects(Array.isArray(defectData) ? defectData : []);
        const titles: Record<string, string> = {};
        (Array.isArray(caseData) ? caseData : []).forEach((c: CaseInfo) => {
          titles[c.id] = c.title;
        });
        setCaseTitles(titles);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load defects');
      } finally {
        setIsLoading(false);
      }
    };
    load();
  }, [projectId]);

  const changeStatus = async (defect: Defect, status: string) => {
    setUpdatingId(defect.id);
    setError('');
    try {
      const updated = await apiCall(`/api/v1/projects/${projectId}/defects/${defect.id}`, {
        method: 'PUT',
        body: JSON.stringify({ status }),
      });
      setDefects((current) =>
        current.map((d) => (d.id === defect.id ? { ...d, status: updated.status } : d))
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update the defect');
    } finally {
      setUpdatingId(null);
    }
  };

  if (isLoading) return <div className="muted">Loading defects...</div>;

  const visible = filter === 'All' ? defects : defects.filter((d) => d.status === filter);
  const openCount = defects.filter((d) => d.status === 'Open').length;

  return (
    <div>
      <div className="toolbar">
        <div>
          <h2>Defects</h2>
          <p className="muted" style={{ fontSize: '0.9rem' }}>
            {defects.length === 1 ? '1 defect' : `${defects.length} defects`}, {openCount} open
          </p>
        </div>
        {defects.length > 0 && (
          <div className="toolbar-right">
            <label htmlFor="status-filter" className="muted" style={{ fontSize: '0.85rem' }}>
              Status
            </label>
            <select
              id="status-filter"
              className="filter-select"
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
            >
              <option value="All">All</option>
              {STATUSES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </div>
        )}
      </div>

      {error && <div className="error-banner">{error}</div>}

      {defects.length === 0 ? (
        <div className="empty-state">
          <h2 style={{ color: '#0f2a43', marginBottom: '0.5rem' }}>No defects yet</h2>
          <p className="muted">
            Defects are raised from a failed result on a test run. Open a run and click Raise defect
            next to a failed test case.
          </p>
        </div>
      ) : visible.length === 0 ? (
        <div className="empty-state">
          <h2 style={{ color: '#0f2a43', marginBottom: '0.5rem' }}>No defects with this status</h2>
          <p className="muted">Choose another status, or All, to see the rest.</p>
        </div>
      ) : (
        <div className="defect-list">
          {visible.map((defect) => (
            <div key={defect.id} className="defect-row">
              <div>
                <div className="defect-title">{defect.title}</div>
                <div className="defect-meta">
                  From test case: {caseTitles[defect.testCaseId] ?? 'Unknown test case'} &middot;
                  Raised {formatDate(defect.createdAt)}
                </div>
                {defect.description && <div className="defect-desc">{defect.description}</div>}
              </div>
              <div className="defect-side">
                <span className={`severity severity-${defect.severity.toLowerCase()}`}>
                  {defect.severity}
                </span>
                <select
                  className="status-select"
                  aria-label={`Status of ${defect.title}`}
                  value={defect.status}
                  disabled={updatingId === defect.id}
                  onChange={(e) => changeStatus(defect, e.target.value)}
                >
                  {STATUSES.map((s) => (
                    <option key={s} value={s}>
                      {s}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}