import { PublicClientApplication, type Configuration } from "@azure/msal-browser";

// Microsoft Entra ID single sign-in. Values come from `src/web/.env`
// (`VITE_ENTRA_*` — see `.env.example`). The backend validates these bearer
// tokens against the SAME client id (`Entra:ClientId`) and provisions/profiles
// the user, so this SPA registration's client id doubles as the API audience.

const clientId = import.meta.env.VITE_ENTRA_CLIENT_ID as string | undefined;
const tenantId = import.meta.env.VITE_ENTRA_TENANT_ID as string | undefined;

/** True when the Entra values needed for a real sign-in are present. */
export const authConfigured = Boolean(clientId && tenantId);

function resolveAuthority(): string | undefined {
  const explicit = import.meta.env.VITE_ENTRA_AUTHORITY as string | undefined;
  if (explicit) return explicit;
  return tenantId ? `https://login.microsoftonline.com/${tenantId}` : undefined;
}

function resolveScopes(): string[] {
  const explicit = import.meta.env.VITE_ENTRA_SCOPE as string | undefined;
  // A bare ".default" (or nothing) means "the default scopes" → `<clientId>/.default`,
  // which is the backend's audience (config/.env `ENTRA_CLIENT_ID` == this client id).
  if (explicit && explicit !== ".default") {
    return explicit.split(",").map((s) => s.trim()).filter(Boolean);
  }
  return clientId ? [`${clientId}/.default`] : [];
}

export const msalConfig: Configuration = {
  auth: {
    clientId: clientId ?? "",
    authority: resolveAuthority(),
    redirectUri:
      (import.meta.env.VITE_ENTRA_REDIRECT_URI as string | undefined) ??
      window.location.origin,
  },
  cache: { cacheLocation: "localStorage" },
};

/** Scopes needed to call the backend API (e.g. `/api/me`). */
export const apiRequest = { scopes: resolveScopes() };

/** Builds the single shared MSAL instance — create once, pass to `<MsalProvider>`. */
export function createMsalInstance(): PublicClientApplication {
  return new PublicClientApplication(msalConfig);
}
