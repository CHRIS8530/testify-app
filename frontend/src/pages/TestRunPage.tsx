import { useState, useEffect } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useApi } from '../hooks/useApi';
import { AppHeader } from '../components/AppHeader';
import '../styles/runs.css';
import '../styles/run-page.css';

type Outcome = 'Pass' | 'Fail' | 'Blocked' | 'Not Run';

const OUTCOMES: Outcome[] = ['Pass', 'Fail', 'Blocked', 'Not Run'];

const OUTCOME_CLASS: Record<Outcome, string> = {
  Pass: 'outcome-pass',
  Fail: 'outcome-fail',
  Blocked: 'outcome-blocked',
  'Not Run': 'outcome-notrun',
};

interface RunInfo {
  id: string;
  status: string;
  createdAt: string;
}

interface ResultRow {
  id: string;
  testCaseId: string;
  testCaseTitle: string;
  result: Outcome;
  notes: string | null;
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

export function TestRunPage() {
  const { projectId, runId } = useParams<{ projectId: string; runId: string }>();
  const [run, setRun] = useState<RunInfo | null>(null);
  const [results, setResults] = useState<ResultRow[]>([]);
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [savingId, setSavingId] = useState<string | null>(null);
  const [isChangingStatus, setIsChangingStatus] = useState(false);
  const { apiCall } = useApi();

  useEffect(() => {
    const load = async () => {
      try {
        if (!projectId || !runId) throw new Error('Test run not found');
        const [runData, resultData] = await Promise.all([
          apiCall(`/api/v1/projects/${projectId}/test-runs/${runId}`),
          apiCall(`/api/v1/projects/${projectId}/test-runs/${runId}/results`),
        ]);
        setRun(runData);
        setResults(Array.isArray(resultData) ? resultData : []);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load the test run');
      } finally {
        setIsLoading(false);
      }
    };
    load();
  }, [projectId, runId]);

  const backLink = (
    <Link to={`/projects/${projectId}?tab=runs`} className="back-link">
      &larr; Test runs
    </Link>
  );

  if (isLoading) {
    return (
      <>
        <AppHeader />
        <div className="page text-center muted">Loading test run...</div>
      </>
    );
  }

  if (!run) {
    return (
      <>
        <AppHeader />
        <main className="page">
          {backLink}
          <div className="error-banner" style={{ marginTop: '1rem' }}>
            {error || 'Test run not found'}
          </div>
        </main>
      </>
    );
  }

  const isComplete = run.status === 'Complete';
  const total = results.length;
  const countOf = (outcome: Outcome) => results.filter((r) => r.result === outcome).length;
  const passed = countOf('Pass');
  const notRun = countOf('Not Run');
  // Same rule as the server: whole percentage, halves round up.
  const passRate = total === 0 ? 0 : Math.round((100 * passed) / total);

  const noteFor = (row: ResultRow) => drafts[row.id] ?? row.notes ?? '';

  const saveResult = async (row: ResultRow, outcome: Outcome, notes: string) => {
    setSavingId(row.id);
    setError('');
    try {
      const updated = await apiCall(
        `/api/v1/projects/${projectId}/test-runs/${runId}/results/${row.id}`,
        { method: 'PATCH', body: JSON.stringify({ result: outcome, notes }) }
      );
      setResults((current) =>
        current.map((r) =>
          r.id === row.id ? { ...r, result: updated.result, notes: updated.notes } : r
        )
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save the result');
    } finally {
      setSavingId(null);
    }
  };

  const handleNotesBlur = (row: ResultRow) => {
    const draft = noteFor(row);
    if (draft !== (row.notes ?? '')) {
      saveResult(row, row.result, draft);
    }
  };

  const changeStatus = async (status: 'Complete' | 'In Progress') => {
    setIsChangingStatus(true);
    setError('');
    try {
      const updated = await apiCall(`/api/v1/projects/${projectId}/test-runs/${runId}`, {
        method: 'PUT',
        body: JSON.stringify({ status }),
      });
      setRun((current) => (current ? { ...current, status: updated.status } : current));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to update the test run');
    } finally {
      setIsChangingStatus(false);
    }
  };

  return (
    <>
      <AppHeader />
      <main className="page">
        {backLink}

        <div className="page-title-row" style={{ marginTop: '0.9rem' }}>
          <div>
            <h1 className="page-title">Test run</h1>
            <p className="muted">Started {formatDateTime(run.createdAt)}</p>
          </div>
          <div className="run-actions">
            <span
              className={`status-badge ${isComplete ? 'status-complete' : 'status-in-progress'}`}
            >
              {run.status}
            </span>
            {isComplete ? (
              <button
                className="secondary"
                onClick={() => changeStatus('In Progress')}
                disabled={isChangingStatus}
              >
                {isChangingStatus ? 'Reopening...' : 'Reopen run'}
              </button>
            ) : (
              <button
                className="primary"
                onClick={() => changeStatus('Complete')}
                disabled={isChangingStatus}
              >
                {isChangingStatus ? 'Completing...' : 'Complete run'}
              </button>
            )}
          </div>
        </div>

        {error && <div className="error-banner">{error}</div>}

        <div className="card run-summary">
          <div className="pass-rate-label">
            <span>Pass rate</span>
            <strong>{passRate}%</strong>
          </div>
          <div
            className="progress"
            role="progressbar"
            aria-label="Pass rate"
            aria-valuenow={passRate}
            aria-valuemin={0}
            aria-valuemax={100}
          >
            <div className="progress-fill" style={{ width: `${passRate}%` }} />
          </div>
          <div className="run-counts">
            <span className="count-chip chip-pass">{passed} Passed</span>
            <span className="count-chip chip-fail">{countOf('Fail')} Failed</span>
            <span className="count-chip chip-blocked">{countOf('Blocked')} Blocked</span>
            <span className="count-chip chip-notrun">{notRun} Not run</span>
          </div>
        </div>

        {!isComplete && notRun > 0 && (
          <p className="muted run-hint">
            {notRun === 1 ? '1 test case is' : `${notRun} test cases are`} still not run.
          </p>
        )}

        {isComplete && (
          <p className="muted run-hint">This run is complete. Reopen it to change a result.</p>
        )}

        {total === 0 ? (
          <div className="empty-state">
            <h2 style={{ color: '#0f2a43', marginBottom: '0.5rem' }}>No test cases in this run</h2>
            <p className="muted">Go back and create a new run with some test cases.</p>
          </div>
        ) : (
          <div className="result-list">
            {results.map((row) => {
              const locked = isComplete || savingId === row.id;
              return (
                <div key={row.id} className="result-card">
                  <div className="result-top">
                    <span className="result-title">{row.testCaseTitle}</span>
                    <div className="outcome-group" role="group" aria-label="Result">
                      {OUTCOMES.map((outcome) => (
                        <button
                          key={outcome}
                          type="button"
                          className={`outcome ${OUTCOME_CLASS[outcome]}`}
                          aria-pressed={row.result === outcome}
                          disabled={locked}
                          onClick={() => saveResult(row, outcome, noteFor(row))}
                        >
                          {outcome === 'Not Run' ? 'Not run' : outcome}
                        </button>
                      ))}
                    </div>
                  </div>
                  <div className="result-notes">
                    <label htmlFor={`notes-${row.id}`} className="muted" style={{ fontSize: '0.8rem' }}>
                      Notes
                    </label>
                    <textarea
                      id={`notes-${row.id}`}
                      rows={2}
                      value={noteFor(row)}
                      placeholder="What happened? (saved when you click away)"
                      disabled={locked}
                      onChange={(e) => setDrafts((current) => ({ ...current, [row.id]: e.target.value }))}
                      onBlur={() => handleNotesBlur(row)}
                    />
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </main>
    </>
  );
}