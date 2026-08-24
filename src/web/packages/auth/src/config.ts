import type { Configuration } from '@azure/msal-browser';

/** Entra configuration supplied by each app via Vite env vars (see .env.example). */
export type AuthEnv = {
  VITE_ENTRA_CLIENT_ID?: string;
  VITE_ENTRA_TENANT_ID?: string;
  VITE_ENTRA_REDIRECT_URI?: string;
  VITE_ENTRA_SCOPE?: string;
};

/** Everything an app needs to wire up MSAL. */
export type MsalSetup = {
  msalConfig: Configuration;
  scopes: string[];
};

const ENTRA_AUTHORITY = 'https://login.microsoftonline.com';

/**
 * Builds the MSAL configuration + token scopes for an app from its Vite env.
 * Fails fast when the Entra identity fields are missing — both portals require login.
 */
export function buildMsalConfig(env: AuthEnv, fallbackOrigin = window.location.origin): MsalSetup {
  const clientId = env.VITE_ENTRA_CLIENT_ID;
  const tenantId = env.VITE_ENTRA_TENANT_ID;
  if (!clientId) throw new Error('VITE_ENTRA_CLIENT_ID is required for Entra login');
  if (!tenantId) throw new Error('VITE_ENTRA_TENANT_ID is required for Entra login');

  const msalConfig: Configuration = {
    auth: {
      clientId,
      authority: `${ENTRA_AUTHORITY}/${tenantId}/v2.0`,
      redirectUri: env.VITE_ENTRA_REDIRECT_URI || fallbackOrigin,
    },
  };

  return { msalConfig, scopes: getTokenScopes(env) };
}

/**
 * Token scope(s) the API accepts. Defaults to the app registration's `/.default`
 * scope, which resolves to every API scope the backend exposes under that client id.
 */
export function getTokenScopes(env: AuthEnv): string[] {
  const clientId = env.VITE_ENTRA_CLIENT_ID;
  if (!clientId) throw new Error('VITE_ENTRA_CLIENT_ID is required to derive token scopes');
  return [env.VITE_ENTRA_SCOPE ?? `${clientId}/.default`];
}
