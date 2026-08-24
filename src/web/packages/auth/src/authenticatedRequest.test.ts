import { beforeEach, describe, expect, it, vi } from 'vitest';
import { authenticatedRequest } from './authenticatedRequest';

/** Minimal fetch-compatible response so tests don't depend on jsdom's Response. */
function jsonResponse(body: unknown, status = 200) {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 200 ? 'OK' : 'Error',
    json: async () => body,
  };
}

describe('authenticatedRequest', () => {
  beforeEach(() => {
    vi.unstubAllGlobals();
  });

  it('sends the bearer token on the request to the API path', async () => {
    const fetchMock = vi.fn().mockResolvedValue(jsonResponse({ name: 'Ada' }));
    vi.stubGlobal('fetch', fetchMock);
    const getAccessToken = vi.fn().mockResolvedValue('token-1');

    const data = await authenticatedRequest<{ name: string }>('/me', getAccessToken);

    expect(data).toEqual({ name: 'Ada' });
    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/api/v1/me');
    expect((init.headers as Headers).get('Authorization')).toBe('Bearer token-1');
  });

  it('re-acquires the token and retries once when the API returns 401', async () => {
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(jsonResponse('Unauthorized', 401))
      .mockResolvedValueOnce(jsonResponse({ name: 'Ada' }));
    vi.stubGlobal('fetch', fetchMock);
    const getAccessToken = vi.fn().mockResolvedValueOnce('token-expired').mockResolvedValueOnce('token-fresh');

    const data = await authenticatedRequest<{ name: string }>('/me', getAccessToken);

    expect(data).toEqual({ name: 'Ada' });
    expect(getAccessToken).toHaveBeenCalledTimes(2);
    const [, retryInit] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect((retryInit.headers as Headers).get('Authorization')).toBe('Bearer token-fresh');
  });

  it('throws when the API keeps returning an error status', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(jsonResponse('boom', 500)));
    const getAccessToken = vi.fn().mockResolvedValue('token-1');

    await expect(authenticatedRequest('/me', getAccessToken)).rejects.toThrow('API request failed: 500');
  });
});
