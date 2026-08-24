import type { Configuration } from '@azure/msal-browser';
import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider } from './AuthProvider';
import { useAuth } from './authContext';

const mocks = vi.hoisted(() => ({
  client: {
    handleRedirectPromise: vi.fn(),
    getActiveAccount: vi.fn(),
    setActiveAccount: vi.fn(),
    acquireTokenSilent: vi.fn(),
    loginRedirect: vi.fn(),
    logoutRedirect: vi.fn(),
  },
}));

vi.mock('./msalClient', () => ({
  createMsalClient: () => mocks.client,
}));

const CONFIG: Configuration = {
  auth: {
    clientId: 'client-1',
    authority: 'https://login.microsoftonline.com/tenant-1/v2.0',
  },
};

function Probe() {
  const auth = useAuth();
  return (
    <div>
      <span data-testid="authenticated">{String(auth.isAuthenticated)}</span>
      <span data-testid="role">{auth.user?.role ?? 'none'}</span>
      <button onClick={auth.login}>login</button>
      <button onClick={auth.logout}>logout</button>
    </div>
  );
}

function jsonResponse(body: unknown, status = 200) {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText: status === 200 ? 'OK' : 'Error',
    json: async () => body,
  };
}

describe('AuthProvider', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.unstubAllGlobals();
    mocks.client.handleRedirectPromise.mockResolvedValue(null);
    mocks.client.getActiveAccount.mockReturnValue(null);
  });

  it('signs in after a successful redirect and loads the profile from /api/me', async () => {
    mocks.client.handleRedirectPromise.mockResolvedValue({ account: { homeAccountId: 'home-1' } });
    mocks.client.getActiveAccount.mockReturnValue({ homeAccountId: 'home-1' });
    mocks.client.acquireTokenSilent.mockResolvedValue({ accessToken: 'token-1' });
    const fetchMock = vi
      .fn()
      .mockResolvedValue(jsonResponse({ id: 1, name: 'Ada', email: 'ada@example.com', role: 'admin' }));
    vi.stubGlobal('fetch', fetchMock);

    render(
      <AuthProvider config={CONFIG} scopes={['client-1/.default']}>
        <Probe />
      </AuthProvider>,
    );

    await waitFor(() => expect(screen.getByTestId('authenticated')).toHaveTextContent('true'));
    expect(screen.getByTestId('role')).toHaveTextContent('admin');

    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('/api/v1/me');
    expect((init.headers as Headers).get('Authorization')).toBe('Bearer token-1');
    expect(mocks.client.setActiveAccount).toHaveBeenCalledWith({ homeAccountId: 'home-1' });
  });

  it('stays signed out when the redirect carries no account', async () => {
    render(
      <AuthProvider config={CONFIG} scopes={['client-1/.default']}>
        <Probe />
      </AuthProvider>,
    );

    await waitFor(() => expect(screen.getByTestId('authenticated')).toHaveTextContent('false'));
    expect(screen.getByTestId('role')).toHaveTextContent('none');
    expect(mocks.client.setActiveAccount).not.toHaveBeenCalled();
  });

  it('triggers an MSAL redirect on login()', async () => {
    render(
      <AuthProvider config={CONFIG} scopes={['client-1/.default']}>
        <Probe />
      </AuthProvider>,
    );

    await waitFor(() => expect(screen.getByTestId('authenticated')).toHaveTextContent('false'));
    screen.getByText('login').click();
    expect(mocks.client.loginRedirect).toHaveBeenCalledWith({ scopes: ['client-1/.default'] });
  });

  it('triggers an MSAL redirect on logout()', async () => {
    render(
      <AuthProvider config={CONFIG} scopes={['client-1/.default']}>
        <Probe />
      </AuthProvider>,
    );

    await waitFor(() => expect(screen.getByTestId('authenticated')).toHaveTextContent('false'));
    screen.getByText('logout').click();
    expect(mocks.client.logoutRedirect).toHaveBeenCalled();
  });
});
