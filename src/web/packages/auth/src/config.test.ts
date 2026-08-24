import { describe, expect, it } from 'vitest';
import { buildMsalConfig, getTokenScopes } from './config';

describe('buildMsalConfig', () => {
  it('throws when the Entra client id is missing', () => {
    expect(() => buildMsalConfig({})).toThrow(/VITE_ENTRA_CLIENT_ID/);
  });

  it('throws when the Entra tenant id is missing', () => {
    expect(() => buildMsalConfig({ VITE_ENTRA_CLIENT_ID: 'client-1' })).toThrow(/VITE_ENTRA_TENANT_ID/);
  });

  it('builds an MSAL config from the Entra env vars', () => {
    const { msalConfig } = buildMsalConfig(
      { VITE_ENTRA_CLIENT_ID: 'client-1', VITE_ENTRA_TENANT_ID: 'tenant-1' },
      'http://localhost:5173',
    );

    expect(msalConfig.auth?.clientId).toBe('client-1');
    expect(msalConfig.auth?.authority).toBe('https://login.microsoftonline.com/tenant-1/v2.0');
    expect(msalConfig.auth?.redirectUri).toBe('http://localhost:5173');
  });

  it('uses the configured redirect URI when provided', () => {
    const { msalConfig } = buildMsalConfig(
      {
        VITE_ENTRA_CLIENT_ID: 'client-1',
        VITE_ENTRA_TENANT_ID: 'tenant-1',
        VITE_ENTRA_REDIRECT_URI: 'http://localhost:4173',
      },
      'http://localhost:5173',
    );

    expect(msalConfig.auth?.redirectUri).toBe('http://localhost:4173');
  });
});

describe('getTokenScopes', () => {
  it('defaults to the client id /.default scope', () => {
    expect(getTokenScopes({ VITE_ENTRA_CLIENT_ID: 'client-1' })).toEqual(['client-1/.default']);
  });

  it('uses the configured scope when provided', () => {
    expect(
      getTokenScopes({
        VITE_ENTRA_CLIENT_ID: 'client-1',
        VITE_ENTRA_SCOPE: 'api://api-1/access_as_user',
      }),
    ).toEqual(['api://api-1/access_as_user']);
  });
});
