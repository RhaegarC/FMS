import { describe, expect, it, vi } from 'vitest';
import { acquireAccessToken, type MsalTokenClient } from './token';

function makeClient(): MsalTokenClient & { getActiveAccount: ReturnType<typeof vi.fn>; acquireTokenSilent: ReturnType<typeof vi.fn> } {
  return {
    getActiveAccount: vi.fn(),
    acquireTokenSilent: vi.fn(),
  };
}

describe('acquireAccessToken', () => {
  it('returns the access token from a silent acquisition', async () => {
    const client = makeClient();
    client.getActiveAccount.mockReturnValue({ homeAccountId: 'home-1' });
    client.acquireTokenSilent.mockResolvedValue({ accessToken: 'token-1' });
    const onRedirect = vi.fn();

    await expect(acquireAccessToken(client, ['scope'], onRedirect)).resolves.toBe('token-1');
    expect(client.acquireTokenSilent).toHaveBeenCalledWith({
      scopes: ['scope'],
      account: { homeAccountId: 'home-1' },
    });
    expect(onRedirect).not.toHaveBeenCalled();
  });

  it('redirects to login when there is no active account', async () => {
    const client = makeClient();
    client.getActiveAccount.mockReturnValue(null);
    const onRedirect = vi.fn();

    await expect(acquireAccessToken(client, ['scope'], onRedirect)).resolves.toBeNull();
    expect(onRedirect).toHaveBeenCalledTimes(1);
  });

  it('re-authenticates via redirect when a silent refresh needs interaction', async () => {
    const client = makeClient();
    client.getActiveAccount.mockReturnValue({ homeAccountId: 'home-1' });
    client.acquireTokenSilent.mockRejectedValue(
      Object.assign(new Error('interaction_required'), { name: 'InteractionRequiredAuthError' }),
    );
    const onRedirect = vi.fn();

    await expect(acquireAccessToken(client, ['scope'], onRedirect)).resolves.toBeNull();
    expect(onRedirect).toHaveBeenCalledTimes(1);
  });

  it('rethrows unexpected token acquisition errors', async () => {
    const client = makeClient();
    client.getActiveAccount.mockReturnValue({ homeAccountId: 'home-1' });
    client.acquireTokenSilent.mockRejectedValue(new Error('network down'));
    const onRedirect = vi.fn();

    await expect(acquireAccessToken(client, ['scope'], onRedirect)).rejects.toThrow('network down');
    expect(onRedirect).not.toHaveBeenCalled();
  });
});
