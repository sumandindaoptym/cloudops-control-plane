/**
 * API Configuration for CloudOps Control Plane
 * 
 * In Replit:
 * - Frontend runs on port 5000 (user-facing)
 * - API runs on port 5056 (backend)
 * - Both are accessible via the Replit domain
 */

export function getApiUrl(): string {
  // Check if we're in the browser
  if (typeof window === 'undefined') {
    // Server-side: use localhost backend directly
    return 'http://localhost:5056';
  }

  // Client-side: always use Next.js API proxy
  // The Next.js API route at /api/[...proxy] will forward requests to the backend
  return '/api';
}

export const API_URL = getApiUrl();

/**
 * Fetch wrapper with proper API URL
 */
export async function apiFetch(endpoint: string, options?: RequestInit) {
  const url = `${API_URL}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;
  const response = await fetch(url, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  });
  
  if (!response.ok) {
    throw new Error(`API error: ${response.status} ${response.statusText}`);
  }
  
  return response.json();
}
