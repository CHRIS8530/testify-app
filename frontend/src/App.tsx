import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, AuthContext } from './context/AuthContext';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { ProjectListPage } from './pages/ProjectListPage';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const auth = React.useContext(AuthContext);
  if (!auth) return <div>Loading...</div>;
  if (auth.isLoading) return <div style={{ textAlign: 'center', padding: '2rem' }}>Loading...</div>;
  if (!auth.isAuthenticated) return <Navigate to="/login" />;
  return <>{children}</>;
}

function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route path="/projects" element={<ProtectedRoute><ProjectListPage /></ProtectedRoute>} />
      <Route path="/projects/:id" element={<ProtectedRoute><div style={{ padding: '2rem', textAlign: 'center' }}>Project detail page coming soon</div></ProtectedRoute>} />
      <Route path="*" element={<Navigate to="/login" />} />
    </Routes>
  );
}

export function App() {
  return (
    <AuthProvider>
      <Router>
        <AppRoutes />
      </Router>
    </AuthProvider>
  );
}

export default App;