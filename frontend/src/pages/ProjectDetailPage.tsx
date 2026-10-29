import { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useApi } from '../hooks/useApi';
import { AppHeader } from '../components/AppHeader';
import type { Project } from '../types/index';
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

  if (isLoading) {
    return (
      <>
        <AppHeader />
        <div className="page text-center muted">Loading project...</div>
      </>
    );
  }

  if (error || !project) {
    return (
      <>
        <AppHeader />
        <main className="page">
          <Link to="/projects" className="back-link">
            &larr; Projects
          </Link>
          <div className="error-banner" style={{ marginTop: '1rem' }}>
            {error || 'Project not found'}
          </div>
        </main>
      </>
    );
  }

  const tabs = [
    { id: 'cases', label: 'Test Cases' },
    { id: 'runs', label: 'Test Runs' },
    { id: 'defects', label: 'Defects' },
    { id: 'dashboard', label: 'Dashboard' },
  ];

  return (
    <>
      <AppHeader />
      <main className="page">
        <Link to="/projects" className="back-link">
          &larr; Projects
        </Link>
        <h1 className="page-title" style={{ marginTop: '0.9rem' }}>
          {project.name}
        </h1>
        <p className="muted" style={{ marginBottom: '2rem' }}>
          {project.description}
        </p>

        <TabNav tabs={tabs} activeTab={activeTab} onChange={setActiveTab} />

        {activeTab === 'cases' && <TestCasesTab projectId={project.id} />}
        {activeTab === 'runs' && <TestRunsTab projectId={project.id} />}
        {activeTab === 'defects' && <DefectsTab projectId={project.id} />}
        {activeTab === 'dashboard' && <DashboardTab projectId={project.id} />}
      </main>
    </>
  );
}