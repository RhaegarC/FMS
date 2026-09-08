import type { AccountInfo, IPublicClientApplication } from "@azure/msal-browser";
import { apiRequest } from "./authConfig";

/** Shape of `GET /api/me` (Fms.Api `MeController`). */
export interface Me {
  id: string;
  name: string;
  email: string;
  role: "admin" | "user";
}

// API base. Defaults to the app's own origin — in dev the Vite server proxies
// `/api` to the backend (see `server.proxy` in vite.config.ts), so no CORS is
// involved. Point `VITE_API_BASE` at an absolute backend origin to bypass the
// proxy (the backend then needs that origin in its CORS allow-list).
const apiBase: string = (import.meta.env.VITE_API_BASE as string | undefined) ?? "";

/**
 * Calls `GET /api/me` with a bearer token for the current account, resolving the
 * caller's Fms identity + role (provisioned by the backend on first login).
 */
export async function fetchMe(
  instance: IPublicClientApplication,
  account: AccountInfo,
): Promise<Me> {
  const result = await instance.acquireTokenSilent({
    scopes: apiRequest.scopes,
    account,
  });

  const res = await fetch(`${apiBase}/api/me`, {
    headers: { Authorization: `Bearer ${result.accessToken}` },
  });
  if (!res.ok) {
    throw new Error(
      `The API rejected the session (HTTP ${res.status}). Is the backend running and configured for this tenant?`,
    );
  }
  return (await res.json()) as Me;
}
