import { authenticatedRequest, type AccessTokenProvider } from './authenticatedRequest';
import type { AuthUser } from './authContext';

/**
 * Fetches the authenticated user's profile (id, email, role) from the protected
 * `/api/me` endpoint. The role gates admin-only routes in the portals.
 */
export function fetchMe(getAccessToken: AccessTokenProvider): Promise<AuthUser> {
  return authenticatedRequest<AuthUser>('/me', getAccessToken);
}
