import { createContext, useContext } from 'react';

/** Role stored on the `users` row (feature 03 backend provisioning). */
export type UserRole = 'admin' | 'user';

/** The authenticated user's profile, loaded from the protected `/api/me` endpoint. */
export type AuthUser = {
  id: number;
  name: string;
  email: string;
  role: UserRole;
};

/** Auth state exposed to the tree via `useAuth()`. */
export type AuthState = {
  isAuthenticated: boolean;
  user: AuthUser | null;
  login: () => void;
  logout: () => void;
  getAccessToken: () => Promise<string | null>;
};

export const AuthContext = createContext<AuthState | null>(null);

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return ctx;
}
