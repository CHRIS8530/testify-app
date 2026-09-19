import { useState, useEffect } from 'react';
import { useApi } from '../../hooks/useApi';
import { TestCaseEditor } from '../../components/TestCaseEditor';

interface TestCase {
  id: string;
  title: string;
  priority: string;
  createdAt: string;
}

export function TestCasesTab({ projectId }: { projectId: string }) {
  const [cases, setCases] = useState<TestCase[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [editingId, setEditingId] = useState<string | null>(null);
  const [showEditor, setShowEditor] = useState(false);
  const { apiCall } = useApi();

  useEffect(() => {
    fetchCases();
  }, [projectId]);

  const fetchCases = async () => {
    try {
      const data = await apiCall(`/api/v1/projects/${projectId}/test-cases`);
      setCases(Array.isArray(data) ? data : []);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load cases');
    } finally {
      setIsLoading(false);
    }
  };

  const handleDelete = async (caseId: string) => {
    if (!confirm('Delete this test case?')) return;
    try {
      await apiCall(`/api/v1/projects/${projectId}/test-cases/${caseId}`, { method: 'DELETE' });
      fetchCases();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to delete');
    }
  };

  if (isLoading) return <div>Loading cases...</div>;

  return (
    <div>
      {error && <div style={{ color: '#d32f2f', marginBottom: '1rem' }}>{error}</div>}

      {showEditor && (
        <div style={{ marginBottom: '2rem', padding: '1rem', border: '1px solid #ddd', borderRadius: '4px' }}>
          <TestCaseEditor
            projectId={projectId}
            caseId={editingId || undefined}
            onSave={() => { setShowEditor(false); setEditingId(null); fetchCases(); }}
            onCancel={() => { setShowEditor(false); setEditingId(null); }}
          />
        </div>
      )}

      {!showEditor && (
        <button className="primary" onClick={() => { setShowEditor(true); setEditingId(null); }} style={{ marginBottom: '1rem' }}>
          Add Test Case
        </button>
      )}

      {cases.length === 0 ? (
        <p>No test cases yet.</p>
      ) : (
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ borderBottom: '2px solid #ddd' }}>
              <th style={{ textAlign: 'left', padding: '0.5rem' }}>Title</th>
              <th style={{ textAlign: 'left', padding: '0.5rem' }}>Priority</th>
              <th style={{ textAlign: 'left', padding: '0.5rem' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {cases.map((c) => (
              <tr key={c.id} style={{ borderBottom: '1px solid #eee' }}>
                <td style={{ padding: '0.5rem' }}>{c.title}</td>
                <td style={{ padding: '0.5rem' }}>{c.priority}</td>
                <td style={{ padding: '0.5rem' }}>
                  <button onClick={() => { setEditingId(c.id); setShowEditor(true); }} style={{ marginRight: '0.5rem', padding: '0.25rem 0.5rem', fontSize: '0.85rem' }}>Edit</button>
                  <button onClick={() => handleDelete(c.id)} style={{ padding: '0.25rem 0.5rem', fontSize: '0.85rem', background: '#ffcdd2', color: '#d32f2f' }}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}