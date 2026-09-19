import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useApi } from '../hooks/useApi';
import type { Project } from '../types/index';

export function ProjectListPage() {
  const [projects, setProjects] = useState<Project[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const { apiCall } = useApi();
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

  if (isLoading) return <div style={{ textAlign: 'center', padding: '2rem' }}>Loading projects...</div>;

  return (
    <div style={{ padding: '2rem' }} className="container">
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '2rem' }}>
        <h1>Projects</h1>
        <button className="primary" onClick={() => navigate('/projects/new')}>Create Project</button>
      </div>

      {error && <div style={{ backgroundColor: '#ffebee', color: '#d32f2f', padding: '1rem', borderRadius: '4px', marginBottom: '1rem' }}>{error}</div>}

      {projects.length === 0 ? (
        <div style={{ textAlign: 'center', padding: '2rem', color: '#666' }}>
          <p>No projects yet. Create one to get started.</p>
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(300px, 1fr))', gap: '1.5rem' }}>
          {projects.map((project) => (
            <div key={project.id} className="card" style={{ cursor: 'pointer' }} onClick={() => navigate(`/projects/${project.id}`)}>
              <h3>{project.name}</h3>
              <p style={{ color: '#666', marginTop: '0.5rem' }}>{project.description}</p>
              <small style={{ color: '#999', marginTop: '1rem', display: 'block' }}>Created {new Date(project.createdAt).toLocaleDateString()}</small>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}