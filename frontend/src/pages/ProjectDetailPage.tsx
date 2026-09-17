import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useApi } from '../hooks/useApi';
import { Project } from '../types';
import { TabNav } from '../components/TabNav';
import { TestCasesTab } from './tabs/TestCasesTab';
import { TestRunsTab } from './tabs/TestRunsTab';
import { DefectsTab } from './tabs/DefectsTab';
import { DashboardTab } from './tabs/DashboardTab';

export function ProjectDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [project, setProject] = useState<Project | null>(null);
  const [activeTab, setActiveTab] = useState('cases');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const { apiCall } = useApi();
  const navigate = useNavigate();

  useEffect(() => {
    const fetchProject = async () => {
      try {
        if (!id) throw new Error('Project ID not found');
        const data = await apiCall(`/api/v1/projects/${id}`);
        setProject(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load project');
      } finally {
        setIsLoading(false);
      }
    };
    fetchProject();
  }, [id]);

  if (isLoading) return <div style={{ textAlign: 'center', padding: '2rem' }}>Loading project...</div>;
  if (error) return <div style={{ padding: '2rem', color: '#d32f2f' }}>Error: {error}</div>;
  if (!project) return <div style={{ padding: '2rem', color: '#d32f2f' }}>Project not found</div>;

  const tabs = [
    { id: 'cases', label: 'Test Cases' },
    { id: 'runs', label: 'Test Runs' },
    { id: 'defects', label: 'Defects' },
    { id: 'dashboard', label: 'Dashboard' },
  ];

  return (
    <div style={{ padding: '2rem' }} className="container">
      <button onClick={() => navigate('/projects')} style={{ marginBottom: '1rem', background: '#e0e0e0', color: '#333', padding: '0.5rem 1rem', border: 'none', borderRadius: '4px', cursor: 'pointer' }}>
        ← Back to Projects
      </button>
      <h1>{project.name}</h1>
      <p style={{ color: '#666', marginBottom: '2rem' }}>{project.description}</p>

      <TabNav tabs={tabs} activeTab={activeTab} onChange={setActiveTab} />

      {activeTab === 'cases' && <TestCasesTab projectId={project.id} />}
      {activeTab === 'runs' && <TestRunsTab projectId={project.id} />}
      {activeTab === 'defects' && <DefectsTab projectId={project.id} />}
      {activeTab === 'dashboard' && <DashboardTab projectId={project.id} />}
    </div>
  );
}