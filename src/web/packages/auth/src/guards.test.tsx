import { render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import { AuthContext, type AuthState } from './authContext';
import { RequireAuth, RequireRole } from './guards';

function renderWithAuth(
  routes: ReactNode,
  auth: Partial<AuthState>,
  initialEntries: string[] = ['/'],
) {
  const state: AuthState = {
    isAuthenticated: auth.isAuthenticated ?? false,
    user: auth.user ?? null,
    login: () => {},
    logout: () => {},
    getAccessToken: async () => null,
  };

  return render(
    <MemoryRouter initialEntries={initialEntries}>
      <AuthContext.Provider value={state}>{routes}</AuthContext.Provider>
    </MemoryRouter>,
  );
}

const adminUser = { id: 1, name: 'Ada', email: 'ada@example.com', role: 'admin' as const };
const regularUser = { id: 2, name: 'Bob', email: 'bob@example.com', role: 'user' as const };

describe('RequireAuth', () => {
  it('redirects unauthenticated users to /login', () => {
    renderWithAuth(
      <Routes>
        <Route path="/login" element={<div>login page</div>} />
        <Route element={<RequireAuth />}>
          <Route path="/" element={<div>protected content</div>} />
        </Route>
      </Routes>,
      { isAuthenticated: false },
    );

    expect(screen.getByText('login page')).toBeInTheDocument();
    expect(screen.queryByText('protected content')).not.toBeInTheDocument();
  });

  it('renders protected content for authenticated users', () => {
    renderWithAuth(
      <Routes>
        <Route path="/login" element={<div>login page</div>} />
        <Route element={<RequireAuth />}>
          <Route path="/" element={<div>protected content</div>} />
        </Route>
      </Routes>,
      { isAuthenticated: true, user: adminUser },
    );

    expect(screen.getByText('protected content')).toBeInTheDocument();
    expect(screen.queryByText('login page')).not.toBeInTheDocument();
  });
});

describe('RequireRole', () => {
  it('renders children when the user has the required role', () => {
    renderWithAuth(
      <Routes>
        <Route path="/" element={<RequireRole role="admin"><div>admin only</div></RequireRole>} />
      </Routes>,
      { isAuthenticated: true, user: adminUser },
    );

    expect(screen.getByText('admin only')).toBeInTheDocument();
  });

  it('redirects users without the required role to the home route', () => {
    renderWithAuth(
      <Routes>
        <Route path="/" element={<div>home</div>} />
        <Route path="/config" element={<RequireRole role="admin"><div>admin only</div></RequireRole>} />
      </Routes>,
      { isAuthenticated: true, user: regularUser },
      ['/config'],
    );

    expect(screen.getByText('home')).toBeInTheDocument();
    expect(screen.queryByText('admin only')).not.toBeInTheDocument();
  });

  it('redirects unauthenticated users to /login from a role-protected route', () => {
    renderWithAuth(
      <Routes>
        <Route path="/login" element={<div>login page</div>} />
        <Route path="/config" element={<RequireRole role="admin"><div>admin only</div></RequireRole>} />
      </Routes>,
      { isAuthenticated: false },
      ['/config'],
    );

    expect(screen.getByText('login page')).toBeInTheDocument();
  });

  it('shows a loading state while the profile is still loading', () => {
    renderWithAuth(
      <Routes>
        <Route path="/config" element={<RequireRole role="admin"><div>admin only</div></RequireRole>} />
      </Routes>,
      { isAuthenticated: true, user: null },
      ['/config'],
    );

    expect(screen.getByText(/loading/i)).toBeInTheDocument();
    expect(screen.queryByText('admin only')).not.toBeInTheDocument();
  });
});
