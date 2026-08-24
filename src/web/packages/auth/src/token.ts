import type { AccountInfo, AuthenticationResult, SilentRequest } from '@azure/msal-browser';

/** The MSAL surface the token helper needs (structural, so tests can mock it). */
export type MsalTokenClient = {
  getActiveAccount: () => AccountInfo | null;
  acquireTokenSilent: (request: SilentRequest) => Promise<AuthenticationResult>;
};

/** Error name MSAL uses when a silent token refresh needs a full redirect. */
const INTERACTION_REQUIRED = 'InteractionRequiredAuthError';

/** Matches the error MSAL throws when user interaction (re-login) is required. */
export function isInteractionRequiredError(error: unknown): boolean {
  return (error as Error | undefined)?.name === INTERACTION_REQUIRED;
}

/**
 * Acquires an access token silently for the API. Falls back to a redirect login
 * when MSAL needs interaction (first login, expired refresh token, tenant policy),
 * satisfying the "expired tokens refresh or re-auth" acceptance criterion.
 */
export async function acquireAccessToken(
  client: MsalTokenClient,
  scopes: string[],
  onRedirect: () => void,
): Promise<string | null> {
  const account = client.getActiveAccount();
  if (!account) {
    onRedirect();
    return null;
  }

  try {
    const response = await client.acquireTokenSilent({ scopes, account });
    return response.accessToken;
  } catch (error) {
    if (isInteractionRequiredError(error)) {
      onRedirect();
      return null;
    }
    throw error;
  }
}
