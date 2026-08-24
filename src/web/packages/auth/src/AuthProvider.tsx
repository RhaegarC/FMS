import type { Configuration } from '@azure/msal-browser';
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { AuthContext, type AuthState, type AuthUser } from './authContext';
import { createMsalClient } from './msalClient';
import { fetchMe } from './profile';
import { acquireAccessToken } from './token';

type AuthProviderProps = {
  config: Configuration;
  scopes: string[];
  children: ReactNode;
};

/**
 * Wires a portal to Microsoft Entra via MSAL's redirect flow:
 *  - processes the OAuth redirect on load (handleRedirectPromise)
 *  - keeps the user signed in across reloads (active account)
 *  - loads the profile + role from `/api/me` so admin routes can be gated
 *  - exposes login/logout/getAccessToken to the tree via `useAuth()`
 */
export function AuthProvider({ config, scopes, children }: AuthProviderProps) {
  // The MSAL client is created once; `config` is module-stable in each app's main.tsx.
  const clientRef = useRef<ReturnType<typeof createMsalClient> | null>(null);
  if (!clientRef.current) clientRef.current = createMsalClient(config);
  const client = clientRef.current;

  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [user, setUser] = useState<AuthUser | null>(null);
  // handleRedirectPromise must run exactly once (StrictMode double-invokes effects).
  const redirectHandledRef = useRef(false);

  const login = useCallback(() => {
    client.loginRedirect({ scopes });
  }, [client, scopes]);

  const logout = useCallback(() => {
    client.logoutRedirect();
  }, [client]);

  const getAccessToken = useCallback(async () => {
    return acquireAccessToken(client, scopes, () => login());
  }, [client, scopes, login]);

  useEffect(() => {
    // StrictMode double-invokes effects in dev; the ref keeps the MSAL redirect
    // handling to exactly one run (handleRedirectPromise must not be re-entered).
    if (redirectHandledRef.current) return;
    redirectHandledRef.current = true;

    (async () => {
      try {
        const result = await client.handleRedirectPromise();
        const account = result?.account ?? client.getActiveAccount();
        if (!account) return;

        client.setActiveAccount(account);
        setIsAuthenticated(true);
        try {
          const profile = await fetchMe(getAccessToken);
          setUser(profile);
        } catch (error) {
          // Profile load failure (e.g. API not up yet) leaves the user signed in
          // but role-unknown; admin routes stay locked until it resolves.
          console.error('Failed to load user profile', error);
        }
      } catch {
        // Redirect processing failed; stay signed out.
      }
    })();
  }, [client, getAccessToken]);

  const value = useMemo<AuthState>(
    () => ({ isAuthenticated, user, login, logout, getAccessToken }),
    [isAuthenticated, user, login, logout, getAccessToken],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
