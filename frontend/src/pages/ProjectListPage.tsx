import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useApi } from '../hooks/useApi';
import { useAuth } from '../hooks/useAuth';
import { AppHeader } from '../components/AppHeader';
import type { Project } from '../types/index';

function formatDate(value: string) {
  return new Date(value).toLocaleDateString('en-GB', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
}

export function ProjectListPage() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const { apiCall } = useApi();
  const { currentUser } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    const fetchProjects = async () => {
      try {
        setIsLoading(true);
        setError('');
        const data = await apiCall('/api/v1/projects');
        setProjects(Array.isArray(data) ? data : []);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load projects');
      } finally {
        setIsLoading(false);
      }
    };
    fetchProjects();
  }, []);

  if (isLoading) {
    return (
      <>
        <AppHeader />
        <div className="page text-center muted">Loading projects...</div>
      </>
    );
  }

  return (
    <>
      <AppHeader />
      <main className="page">
        <div className="page-title-row">
          <div>
            <h1 className="page-title">Projects</h1>
            <p className="muted">
              {projects.length === 1
                ? '1 project you own or belong to'
                : `${projects.length} projects you own or belong to`}
            </p>
          </div>
          <button className="primary" onClick={() => navigate('/projects/new')}>
            + Create project
          </button>
        </div>

        {error && <div className="error-banner">{error}</div>}

        {projects.length === 0 && !error ? (
          <div className="empty-state">
            <h2 style={{ color: '#0f2a43', marginBottom: '0.5rem' }}>No projects yet</h2>
            <p className="muted" style={{ marginBottom: '1.25rem' }}>
              A project holds your test cases, test runs and defects. Create one to get started.
            </p>
            <button className="primary" onClick={() => navigate('/projects/new')}>
              + Create project
            </button>
          </div>
        ) : (
          <div className="project-grid">
            {projects.map((project) => (
              <Link key={project.id} to={`/projects/${project.id}`} className="project-card">
                <div
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'flex-start',
                    gap: '0.75rem',
                  }}
                >
                  <h2>{project.name}</h2>
                  {project.ownerId === currentUser?.id && (
                    <span className="badge badge-owner">Owner</span>
                  )}
                </div>
                <p>{project.description}</p>
                <div className="project-card-footer">
                  <span>Created {formatDate(project.createdAt)}</span>
                  <span className="project-open">Open &rarr;</span>
                </div>
              </Link>
            ))}
          </div>
        )}
      </main>
    </>
  );
}