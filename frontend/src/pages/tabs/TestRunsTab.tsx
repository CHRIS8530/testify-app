import { useState, useEffect } from 'react';
import { useApi } from '../../hooks/useApi';
import '../../styles/runs.css';

interface TestRunSummary {
  id: string;
  status: string;
  createdAt: string;
  total: number;
  passed: number;
  failed: number;
  blocked: number;
  notRun: number;
  passRate: number;
}

interface CaseOption {
  id: string;
  title: string;
  priority: string;
}

function formatDateTime(value: string) {
  return new Date(value).toLocaleString('en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function TestRunsTab({ projectId }: { projectId: string }) {
  const [runs, setRuns] = useState<TestRunSummary[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [showPicker, setShowPicker] = useState(false);
  const [cases, setCases] = useState<CaseOption[]>([]);
  const [casesLoading, setCasesLoading] = useState(false);
  const [selected, setSelected] = useState<string[]>([]);
  const [isCreating, setIsCreating] = useState(false);
  const { apiCall } = useApi();

  const loadRuns = async () => {
    try {
      const data = await apiCall(`/api/v1/projects/${projectId}/test-runs`);
      setRuns(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load test runs');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadRuns();
  }, [projectId]);

  const openPicker = async () => {
    setError('');
    setShowPicker(true);
    setCasesLoading(true);
    try {
      const data = await apiCall(`/api/v1/projects/${projectId}/test-cases?limit=200`);
      const list: CaseOption[] = Array.isArray(data) ? data : [];
      setCases(list);
      setSelected(list.map((c) => c.id));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load test cases');
    } finally {
      setCasesLoading(false);
    }
  };

  const closePicker = () => {
    setShowPicker(false);
    setError('');
  };

  const toggleCase = (id: string) => {
    setSelected((current) =>
      current.includes(id) ? current.filter((x) => x !== id) : [...current, id]
    );
  };

  const toggleAll = () => {
    setSelected(selected.length === cases.length ? [] : cases.map((c) => c.id));
  };

  const createRun = async () => {
    if (selected.length === 0) {
      setError('Select at least one test case');
      return;
    }

    setError('');
    setIsCreating(true);
    try {
      const run = await apiCall(`/api/v1/projects/${projectId}/test-runs`, { method: 'POST' });
      try {
        await apiCall(`/api/v1/projects/${projectId}/test-runs/${run.id}/results`, {
          method: 'POST',
          body: JSON.stringify({ testCaseIds: selected }),
        });
      } catch (err) {
        // Do not leave an empty run behind when adding the cases failed.
        await apiCall(`/api/v1/projects/${projectId}/test-runs/${run.id}`, { method: 'DELETE' }).catch(
          () => null
        );
        throw err;
      }
      setShowPicker(false);
      await loadRuns();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create test run');
    } finally {
      setIsCreating(false);
    }
  };

  if (isLoading) return <div className="muted">Loading test runs...</div>;

  return (
    <div>
      <div className="toolbar">
        <h2>Test runs</h2>
        {!showPicker && (
          <button className="primary" onClick={openPicker}>
            + New test run
          </button>
        )}
      </div>

      {error && <div className="error-banner">{error}</div>}

      {showPicker && (
        <div className="card picker">
          <div className="picker-head">
            <h3>Choose the test cases for this run</h3>
            {cases.length > 0 && (
              <button type="button" className="secondary" onClick={toggleAll} disabled={isCreating}>
                {selected.length === cases.length ? 'Clear all' : 'Select all'}
              </button>
            )}
          </div>

          {casesLoading ? (
            <p className="muted">Loading test cases...</p>
          ) : cases.length === 0 ? (
            <p className="muted" style={{ marginBottom: '1rem' }}>
              This project has no test cases yet. Add some on the Test Cases tab first.
            </p>
          ) : (
            <div className="pick-list">
              {cases.map((c) => (
                <label key={c.id} className="pick-row">
                  <input
                    type="checkbox"
                    checked={selected.includes(c.id)}
                    onChange={() => toggleCase(c.id)}
                    disabled={isCreating}
                  />
                  <span className="pick-title">{c.title}</span>
                  <span className="pick-priority">{c.priority}</span>
                </label>
              ))}
            </div>
          )}

          <div className="form-actions" style={{ borderTop: 'none', paddingTop: 0 }}>
            <button
              className="primary"
              onClick={createRun}
              disabled={isCreating || casesLoading || cases.length === 0}
            >
              {isCreating ? 'Creating...' : `Create run (${selected.length} cases)`}
            </button>
            <button className="secondary" onClick={closePicker} disabled={isCreating}>
              Cancel
            </button>
          </div>
        </div>
      )}

      {runs.length === 0 && !showPicker ? (
        <div className="empty-state">
          <h2 style={{ color: '#0f2a43', marginBottom: '0.5rem' }}>No test runs yet</h2>
          <p className="muted" style={{ marginBottom: '1.25rem' }}>
            A run takes a set of test cases and records Pass, Fail or Blocked for each one.
          </p>
          <button className="primary" onClick={openPicker}>
            + New test run
          </button>
        </div>
      ) : (
        <div className="run-list">
          {runs.map((run) => (
            <div key={run.id} className="run-row">
              <div>
                <div className="run-head">
                  <span className="run-title">Run from {formatDateTime(run.createdAt)}</span>
                  <span
                    className={`status-badge ${
                      run.status === 'Complete' ? 'status-complete' : 'status-in-progress'
                    }`}
                  >
                    {run.status}
                  </span>
                </div>
                <div className="run-meta">
                  {run.total === 1 ? '1 test case' : `${run.total} test cases`}
                </div>
                <div className="run-counts">
                  <span className="count-chip chip-pass">{run.passed} Passed</span>
                  <span className="count-chip chip-fail">{run.failed} Failed</span>
                  <span className="count-chip chip-blocked">{run.blocked} Blocked</span>
                  <span className="count-chip chip-notrun">{run.notRun} Not run</span>
                </div>
              </div>
              <div>
                <div className="pass-rate-label">
                  <span>Pass rate</span>
                  <strong>{run.passRate}%</strong>
                </div>
                <div
                  className="progress"
                  role="progressbar"
                  aria-label="Pass rate"
                  aria-valuenow={run.passRate}
                  aria-valuemin={0}
                  aria-valuemax={100}
                >
                  <div className="progress-fill" style={{ width: `${run.passRate}%` }} />
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}