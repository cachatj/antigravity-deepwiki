/**
 * Server-only utility for reading API_PROXY_URL from environment.
 *
 * NOTE: This module is NOT imported anywhere — the API route handlers
 * (app/api/[...path]/route.ts, app/oauth/[...path]/route.ts) define
 * their own inline getApiProxyUrl(). Kept here for reference only.
 *
 * Using dynamic imports so it won't break client-side bundling.
 */

// Cache environment variables and last load time
let cachedApiUrl: string | null = null;
let lastLoadTime = 0;
const CACHE_TTL = 5000; // 5-second cache for hot reloading

/**
 * Get API proxy address (server-side only)
 */
export function getApiProxyUrl(): string {

  // Prefer system environment variables (from Docker/K8s)
  if (process.env.API_PROXY_URL) {
    return process.env.API_PROXY_URL;
  }

  const now = Date.now();
  // Use cache
  if (cachedApiUrl !== null && (now - lastLoadTime) < CACHE_TTL) {
    return cachedApiUrl;
  }

  // This module is not used at runtime — see note above.
  // If you need it, import it only from a server component or API route.
  cachedApiUrl = '';
  lastLoadTime = now;
  return '';
}
