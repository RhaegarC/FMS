import { PublicClientApplication, type Configuration } from '@azure/msal-browser';

/**
 * Factory kept behind a single call so AuthProvider stays testable without a
 * real MSAL client (tests mock this module).
 */
export function createMsalClient(config: Configuration): PublicClientApplication {
  return new PublicClientApplication(config);
}
