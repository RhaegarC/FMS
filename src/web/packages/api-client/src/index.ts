/** Shared HTTP client for the FMS backend API. */

// Types generated from the backend OpenAPI spec (feature 01).
// Regenerate with: pnpm --dir src/web --filter @fms/api-client gen
export type * from './generated';

/** API version prefix used in every request path. */
export const API_VERSION = 'v1';

/** Base path requests are made against; reverse-proxied in Docker. */
export function buildUrl(path: string): string {
  return `/api/${API_VERSION}${path.startsWith('/') ? path : `/${path}`}`;
}

/** Thin typed fetch wrapper. Throws on non-2xx responses. */
export async function requestJson<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(buildUrl(path), init);
  if (!res.ok) {
    throw new Error(`API request failed: ${res.status} ${res.statusText}`);
  }
  return res.json() as Promise<T>;
}
