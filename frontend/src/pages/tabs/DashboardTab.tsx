import { useState, useEffect } from 'react';
import { useApi } from '../../hooks/useApi';
import '../../styles/runs.css';
import '../../styles/defects.css';
import '../../styles/dashboard.css';

const SEVERITIES = ['Critical', 'High', 'Medium', 'Low'];

interface LatestRun {
  id: string;
  status: string;
  created_at: string;
  total: number;
  passed: number;
  failed: number;
  blocked: number;
  not_run: number;
  pass_rate: number;
}

interface RecentDefect {
  id: string;
  title: string;
  severity: string;
  status: string;
  created_at: string;
}

interface DashboardData {
  total_cases: number;
  total_runs: number;
  runs_completed: number;
  pass_rate: number;
  total_results: number;
  latest_run: LatestRun | null;
  open_defects: number;
  defects_by_severity: { severity: string; count: number }[];
  recent_defects: RecentDefect[];
}

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

export function DashboardTab({ projectId }: { projectId: string }) {
  const [data, setData] = useState<DashboardData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const { apiCall } = useApi();

  useEffect(() => {
    const load = async () => {
      try {
        const result = await apiCall(`/api/v1/projects/${projectId}/dashboard`);
        setData(result);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load the dashboard');
      } finally {
        setIsLoading(false);
      }
    };
    load();
  }, [projectId]);

  if (isLoading) return <div className="muted">Loading dashboard...</div>;
  if (error || !data) return <div className="error-banner">{error || 'Dashboard not available'}</div>;

  const latest = data.latest_run;
  const countFor = (severity: string) =>
    data.defects_by_severity.find((x) => x.severity === severity)?.count ?? 0;
  const biggest = Math.max(1, ...SEVERITIES.map(countFor));

  return (
    <div>
      <div className="toolbar">
        <h2>Dashboard</h2>
      </div>

      <div className="stat-grid">
        <div className="stat-card">
          <div className="stat-label">Test cases</div>
          <div className="stat-value">{data.total_cases}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Runs completed</div>
          <div className="stat-value">{data.runs_completed}</div>
          <div className="stat-sub">of {data.total_runs} {data.total_runs === 1 ? 'run' : 'runs'}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Current pass rate</div>
          <div className="stat-value">{latest ? `${latest.pass_rate}%` : '-'}</div>
          <div className="stat-sub">{latest ? 'latest run' : 'no runs yet'}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Open defects</div>
          <div className="stat-value">{data.open_defects}</div>
        </div>
      </div>

      <div className="panel-grid">
        <div className="panel">
          <h3>Latest run</h3>
          {latest ? (
            <>
              <div className="pass-rate-label">
                <span>
                  Run from {formatDate(latest.created_at)} ({latest.status})
                </span>
                <strong>{latest.pass_rate}%</strong>
              </div>
              <div
                className="progress"
                role="progressbar"
                aria-label="Pass rate of the latest run"
                aria-valuenow={latest.pass_rate}
                aria-valuemin={0}
                aria-valuemax={100}
              >
                <div className="progress-fill" style={{ width: `${latest.pass_rate}%` }} />
              </div>
              <div className="run-counts">
                <span className="count-chip chip-pass">{latest.passed} Passed</span>
                <span className="count-chip chip-fail">{latest.failed} Failed</span>
                <span className="count-chip chip-blocked">{latest.blocked} Blocked</span>
                <span className="count-chip chip-notrun">{latest.not_run} Not run</span>
              </div>
              <p className="muted" style={{ fontSize: '0.8rem', marginTop: '0.9rem' }}>
                Across all runs: {data.pass_rate}% of {data.total_results} results passed.
              </p>
            </>
          ) : (
            <p className="muted">No test runs yet. Create one on the Test Runs tab.</p>
          )}
        </div>

        <div className="panel">
          <h3>Open defects by severity</h3>
          {data.open_defects === 0 ? (
            <p className="muted">No open defects.</p>
          ) : (
            SEVERITIES.map((severity) => (
              <div key={severity} className="sev-row">
                <span className={`severity severity-${severity.toLowerCase()}`}>{severity}</span>
                <div className="sev-track">
                  <div
                    className={`sev-fill sev-fill-${severity.toLowerCase()}`}
                    style={{ width: `${(countFor(severity) / biggest) * 100}%` }}
                  />
                </div>
                <span className="sev-count">{countFor(severity)}</span>
              </div>
            ))
          )}
        </div>
      </div>

      <div className="panel">
        <h3>Recent defects</h3>
        {data.recent_defects.length === 0 ? (
          <p className="muted">No defects raised yet.</p>
        ) : (
          data.recent_defects.map((defect) => (
            <div key={defect.id} className="recent-row">
              <span className={`severity severity-${defect.severity.toLowerCase()}`}>
                {defect.severity}
              </span>
              <span className="recent-title">{defect.title}</span>
              <span className="recent-meta">
                {defect.status} &middot; {formatDate(defect.created_at)}
              </span>
            </div>
          ))
        )}
      </div>
    </div>
  );
}