import React, { useState, useEffect } from 'react';
import { useApi } from '../hooks/useApi';

interface TestCaseEditorProps {
  projectId: string;
  caseId?: string;
  onSave: () => void;
  onCancel: () => void;
}

export function TestCaseEditor({ projectId, caseId, onSave, onCancel }: TestCaseEditorProps) {
  const [title, setTitle] = useState('');
  const [steps, setSteps] = useState('');
  const [expectedResult, setExpectedResult] = useState('');
  const [priority, setPriority] = useState('Medium');
  const [preconditions, setPreconditions] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const { apiCall } = useApi();

  useEffect(() => {
    if (caseId) {
      const fetchCase = async () => {
        try {
          const data = await apiCall(`/api/v1/projects/${projectId}/test-cases/${caseId}`);
          setTitle(data.title);
          setSteps(data.steps);
          setExpectedResult(data.expectedResult);
          setPriority(data.priority);
          setPreconditions(data.preconditions || '');
        } catch (err) {
          setError(err instanceof Error ? err.message : 'Failed to load test case');
        }
      };
      fetchCase();
    }
  }, [caseId, projectId]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setIsLoading(true);

    try {
      const body = { title, steps, expectedResult, priority, preconditions };
      if (caseId) {
        await apiCall(`/api/v1/projects/${projectId}/test-cases/${caseId}`, { method: 'PATCH', body: JSON.stringify(body) });
      } else {
        await apiCall(`/api/v1/projects/${projectId}/test-cases`, { method: 'POST', body: JSON.stringify(body) });
      }
      onSave();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save test case');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} style={{ maxWidth: '600px' }} className="card">
      <h2>{caseId ? 'Edit' : 'Create'} Test Case</h2>

      {error && <div style={{ backgroundColor: '#ffebee', color: '#d32f2f', padding: '0.75rem', borderRadius: '4px', marginBottom: '1rem' }}>{error}</div>}

      <div className="form-group">
        <label>Title *</label>
        <input type="text" value={title} onChange={(e) => setTitle(e.target.value)} required disabled={isLoading} />
      </div>

      <div className="form-group">
        <label>Preconditions</label>
        <textarea value={preconditions} onChange={(e) => setPreconditions(e.target.value)} disabled={isLoading} rows={3}></textarea>
      </div>

      <div className="form-group">
        <label>Steps *</label>
        <textarea value={steps} onChange={(e) => setSteps(e.target.value)} required disabled={isLoading} rows={5}></textarea>
      </div>

      <div className="form-group">
        <label>Expected Result *</label>
        <textarea value={expectedResult} onChange={(e) => setExpectedResult(e.target.value)} required disabled={isLoading} rows={5}></textarea>
      </div>

      <div className="form-group">
        <label>Priority</label>
        <select value={priority} onChange={(e) => setPriority(e.target.value)} disabled={isLoading}>
          <option>Low</option>
          <option>Medium</option>
          <option>High</option>
          <option>Critical</option>
        </select>
      </div>

      <div style={{ display: 'flex', gap: '1rem' }}>
        <button type="submit" className="primary" disabled={isLoading}>
          {isLoading ? 'Saving...' : 'Save'}
        </button>
        <button type="button" className="secondary" onClick={onCancel} disabled={isLoading}>
          Cancel
        </button>
      </div>
    </form>
  );
}