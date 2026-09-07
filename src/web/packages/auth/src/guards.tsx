import type { ReactNode } from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth, type UserRole } from './authContext';

/**
 * Layout route guard: redirects unauthenticated users to /login and renders the
 * nested protected routes for everyone else.
 */
export function RequireAuth() {
  const { isAuthenticated } = useAuth();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  return <Outlet />;
}

type RequireRoleProps = {
  role: UserRole;
  children: ReactNode;
};

/**
 * Element-level role gate for admin-only routes. Waits for the profile (role) to
 * load, then redirects non-admins back to the home route.
 */
export function RequireRole({ role, children }: RequireRoleProps) {
  const { isAuthenticated, user } = useAuth();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (!user) return <p>Loading account…</p>;
  if (user.role !== role) return <Navigate to="/" replace />;
  return <>{children}</>;
}
