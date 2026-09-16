import React, { createContext, useState, useEffect, ReactNode } from 'react';
import { User, AuthResponse } from '../types';

export interface AuthContextType {
  accessToken: string | null;
  currentUser: User | null;
  isLoading: boolean;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextType | null>(null);

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [accessToken, setAccessToken] = useState<string | null>(null);
  const [currentUser, setCurrentUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Silent refresh on app load
  useEffect(() => {
    const initAuth = async () => {
      try {
        const response = await fetch('http://localhost:5000/api/v1/auth/refresh', {
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
    const response = await fetch('http://localhost:5000/api/v1/auth/login', {
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
    const response = await fetch('http://localhost:5000/api/v1/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email, username, password }),
      credentials: 'include',
    });
    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.error?.message || 'Registration failed');
    }
    const data: AuthResponse = await response.json();
    setAccessToken(data.accessToken);
    setCurrentUser(data.user);
  };

  const logout = async () => {
    try {
      await fetch('http://localhost:5000/api/v1/auth/logout', {
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
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}