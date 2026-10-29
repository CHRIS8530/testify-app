import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export function AppHeader() {
  const { currentUser, logout } = useAuth();
  const navigate = useNavigate();

  const handleSignOut = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <header className="app-header">
      <Link to="/projects" className="app-brand">
        <svg
          aria-hidden="true"
          width="26"
          height="26"
          viewBox="0 0 24 24"
          fill="none"
          stroke="#6fb4ff"
          strokeWidth="2.2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <rect x="3" y="3" width="18" height="18" rx="4" />
          <path d="M8 12.5l3 3 5-6" />
        </svg>
        Testify
      </Link>
      <div className="app-user">
        {currentUser && (
          <span className="app-user-name">
            <span className="app-avatar" aria-hidden="true">
              {currentUser.username.charAt(0).toUpperCase()}
            </span>
            {currentUser.username}
          </span>
        )}
        <button type="button" className="signout" onClick={handleSignOut}>
          Sign out
        </button>
      </div>
    </header>
  );
}