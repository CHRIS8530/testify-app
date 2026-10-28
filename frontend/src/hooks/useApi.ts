import { useAuth } from './useAuth';
import { API_BASE_URL } from '../config';

export function useApi() {
  const { accessToken, refreshAccessToken } = useAuth();

  const send = (url: string, options: RequestInit, token: string | null) =>
    fetch(`${API_BASE_URL}${url}`, {
      ...options,
      headers: {
        'Content-Type': 'application/json',
        ...options.headers,
        ...(token && { Authorization: `Bearer ${token}` }),
      },
      credentials: 'include',
    });

  const apiCall = async (url: string, options: RequestInit = {}) => {
    let response = await send(url, options, accessToken);

    // The access token only lasts 15 minutes. Get a new one and try again, once.
    if (response.status === 401) {
      const newToken = await refreshAccessToken();
      if (!newToken) throw new Error('Session expired, please sign in again');
      response = await send(url, options, newToken);
    }

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: { message: response.statusText } }));
      throw new Error(error.error?.message || response.statusText);
    }

    // A successful DELETE answers 204 with no body, so there is nothing to parse.
    if (response.status === 204) return null;

    return response.json();
  };

  return { apiCall };
}