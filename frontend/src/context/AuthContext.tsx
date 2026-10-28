import { createContext, useState, useEffect, useRef, useCallback, type ReactNode } from 'react';
import type { User, AuthResponse } from '../types/index';
import { API_BASE_URL } from '../config';

export interface AuthContextType {
  accessToken: string | null;
  currentUser: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshAccessToken: () => Promise<string | null>;
}

export const AuthContext = createContext<AuthContextType | null>(null);

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const refreshInFlight = useRef<Promise<string | null> | null>(null);

  // Asks the API for a new access token using the refresh cookie.
  // Calls made while a refresh is already running share that same refresh, because
  // every refresh retires the old cookie and a second one would fail.
  const refreshAccessToken = useCallback((): Promise<string | null> => {
    if (refreshInFlight.current) return refreshInFlight.current;

    const attempt = (async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/api/v1/auth/refresh`, {
          method: 'POST',
          credentials: 'include',
        });
        if (!response.ok) {
          setAccessToken(null);
          setCurrentUser(null);
          return null;
        }
        const data: AuthResponse = await response.json();
        setAccessToken(data.accessToken);
        setCurrentUser(data.user);
        return data.accessToken;
      } catch {
        return null;
      } finally {
        refreshInFlight.current = null;
      }
    })();

    refreshInFlight.current = attempt;
    return attempt;
  }, []);

  // Silent refresh on app load
  useEffect(() => {
    const initAuth = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/api/v1/auth/refresh`, {
          method: 'POST',
          credentials: 'include',
        });
        if (response.ok) {
          const data: AuthResponse = await response.json();
          setAccessToken(data.accessToken);
          setCurrentUser(data.user);
        }
      } catch (err) {
        console.error('Auth init failed:', err);
      } finally {
        setIsLoading(false);
      }
    };
    initAuth();
  }, []);

  const login = async (email: string, password: string) => {
    const response = await fetch(`${API_BASE_URL}/api/v1/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, password }),
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error?.message || 'Login failed');
    }
    const data: AuthResponse = await response.json();
    setAccessToken(data.accessToken);
    setCurrentUser(data.user);
  };

  const register = async (email: string, username: string, password: string) => {
    const response = await fetch(`${API_BASE_URL}/api/v1/auth/register`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, username, password }),
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error?.message || 'Registration failed');
    }
    // The register endpoint returns the new user but no token, so sign in straight away.
    await login(email, password);
  };

  const logout = async () => {
    try {
      await fetch(`${API_BASE_URL}/api/v1/auth/logout`, {
        method: 'POST',
        credentials: 'include',
      });
    } catch (err) {
      console.error('Logout failed:', err);
    }
    setAccessToken(null);
    setCurrentUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        accessToken,
        currentUser,
        isLoading,
        isAuthenticated: !!accessToken,
        login,
        register,
        logout,
        refreshAccessToken,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}