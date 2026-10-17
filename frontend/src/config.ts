const url: string | undefined = import.meta.env.VITE_API_URL;

if (!url) {
  throw new Error(
    'VITE_API_URL is not set. For local development it comes from frontend/.env.development. Anywhere else, set it in the environment.'
  );
}

// Origin of the API, with no trailing slash and no /api/v1, for example http://localhost:5137
export const API_BASE_URL: string = url.replace(/\/+$/, '');
