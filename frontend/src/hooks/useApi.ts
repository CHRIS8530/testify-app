import { useAuth } from './useAuth';

export function useApi() {
  const { accessToken } = useAuth();

  const apiCall = async (url: string, options: RequestInit = {}) => {
    const response = await fetch(`http://localhost:5000${url}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
        ...(accessToken && { 'Authorization': `Bearer ${accessToken}` }),
      },
      credentials: 'include',
    });

    if (response.status === 401) {
      // Try refresh
      const refreshResponse = await fetch('http://localhost:5000/api/v1/auth/refresh', {
        method: 'POST',
        credentials: 'include',
      });
      if (!refreshResponse.ok) throw new Error('Session expired');
      // Note: Would need to update token in AuthContext here, but for now just re-throw
      throw new Error('Token expired — please log in again');
    }

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: { message: response.statusText } }));
      throw new Error(error.error?.message || response.statusText);
    }

    return response.json();
  };

  return { apiCall };
}