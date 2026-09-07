import { buildUrl } from '@fms/api-client';

export type AccessTokenProvider = () => Promise<string | null>;

async function send(
  path: string,
  getAccessToken: AccessTokenProvider,
  init?: RequestInit,
): Promise<Response> {
  const token = await getAccessToken();
  const headers = new Headers(init?.headers);
  if (token) headers.set('Authorization', `Bearer ${token}`);
  return fetch(buildUrl(path), { ...init, headers });
}

/**
 * Typed fetch that attaches the current bearer token to every API call.
 * On a 401 the token is re-acquired (MSAL silently refreshes, or redirects for
 * re-auth) and the request is retried once before surfacing an error.
 */
export async function authenticatedRequest<T>(
  path: string,
  getAccessToken: AccessTokenProvider,
  init?: RequestInit,
): Promise<T> {
  let response = await send(path, getAccessToken, init);
  if (response.status === 401) {
    response = await send(path, getAccessToken, init);
  }
  if (!response.ok) {
    throw new Error(`API request failed: ${response.status} ${response.statusText}`);
  }
  return response.json() as Promise<T>;
}
