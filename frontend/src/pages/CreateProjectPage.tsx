import { useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useApi } from '../hooks/useApi';
import { AppHeader } from '../components/AppHeader';
import type { Project } from '../types/index';

export function CreateProjectPage() {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const { apiCall } = useApi();
  const navigate = useNavigate();

  // Same rules and messages as ProjectsController.CreateProject on the server.
  const validateForm = () => {
    if (!name.trim()) {
      setError('Name is required');
      return false;
    }
    if (name.trim().length > 255) {
      setError('Name must be 255 characters or less');
      return false;
    }
    return true;
  };

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    if (!validateForm()) return;

    setIsLoading(true);
    try {
      const project: Project = await apiCall('/api/v1/projects', {
        method: 'POST',
        body: JSON.stringify({ name: name.trim(), description: description.trim() }),
      });
      navigate(`/projects/${project.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create project');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <>
      <AppHeader />
      <main className="page-narrow">
        <Link to="/projects" className="back-link">
          &larr; Projects
        </Link>
        <h1 className="page-title" style={{ marginTop: '0.9rem' }}>
          Create project
        </h1>
        <p className="muted" style={{ marginBottom: '1.5rem' }}>
          A project holds your test cases, test runs and defects.
        </p>

        <form onSubmit={handleSubmit} className="card" style={{ padding: '1.75rem' }}>
          {error && <div className="error-banner">{error}</div>}

          <div className="form-group">
            <div className="field-row">
              <label htmlFor="name" style={{ marginBottom: 0 }}>
                Name
              </label>
              <span className="muted" style={{ fontSize: '0.8rem' }}>
                {name.length} / 255
              </span>
            </div>
            <input
              id="name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Checkout Regression"
              maxLength={255}
              disabled={isLoading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="description">
              Description <span className="muted" style={{ fontWeight: 400 }}>(optional)</span>
            </label>
            <textarea
              id="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="What does this project cover?"
              rows={4}
              disabled={isLoading}
            />
          </div>

          <div className="form-actions">
            <button type="submit" className="primary" disabled={isLoading}>
              {isLoading ? 'Creating...' : 'Create project'}
            </button>
            <button
              type="button"
              className="secondary"
              onClick={() => navigate('/projects')}
              disabled={isLoading}
            >
              Cancel
            </button>
          </div>
        </form>
      </main>
    </>
  );
}