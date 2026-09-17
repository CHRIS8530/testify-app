import React from 'react';

interface TabNavProps {
  tabs: { id: string; label: string }[];
  activeTab: string;
  onChange: (tabId: string) => void;
}

export function TabNav({ tabs, activeTab, onChange }: TabNavProps) {
  return (
    <div style={{ display: 'flex', borderBottom: '1px solid #ddd', marginBottom: '1.5rem' }}>
      {tabs.map((tab) => (
        <button
          key={tab.id}
          onClick={() => onChange(tab.id)}
          style={{
            padding: '1rem',
            border: 'none',
            background: activeTab === tab.id ? '#0066cc' : 'transparent',
            color: activeTab === tab.id ? 'white' : '#333',
            cursor: 'pointer',
            fontSize: '1rem',
          }}
        >
          {tab.label}
        </button>
      ))}
    </div>
  );
}