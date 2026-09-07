import { AuthContext, type AuthState } from '@fms/auth';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import App from './App';

function renderApp(initialPath: string, auth: AuthState) {
  return render(
    <AuthContext.Provider value={auth}>
      <MemoryRouter initialEntries={[initialPath]}>
        <App />
      </MemoryRouter>
    </AuthContext.Provider>,
  );
}

const anonymousAuth: AuthState = {
  isAuthenticated: false,
  user: null,
  login: () => {},
  logout: () => {},
  getAccessToken: async () => null,
};

function authedAuth(role: 'admin' | 'user'): AuthState {
  return {
    isAuthenticated: true,
    user: {
      id: 1,
      name: role === 'admin' ? 'Admin' : 'User',
      email: role === 'admin' ? 'admin@example.com' : 'user@example.com',
      role,
    },
    login: () => {},
    logout: () => {},
    getAccessToken: async () => null,
  };
}

describe('Admin App', () => {
  it('renders the admin home for an authenticated admin', () => {
    renderApp('/', authedAuth('admin'));

    expect(screen.getByRole('heading', { name: 'FMS Admin' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Create form' })).toBeInTheDocument();
    expect(screen.getByText(/\/api\/v1\/health/)).toBeInTheDocument();
  });

  it('shows the signed-in email and signs out', () => {
    const logout = vi.fn();
    renderApp('/', { ...authedAuth('admin'), logout });

    expect(screen.getByTestId('user-email')).toHaveTextContent('admin@example.com');
    screen.getByRole('button', { name: 'Sign out' }).click();
    expect(logout).toHaveBeenCalled();
  });

  it('redirects unauthenticated users to the login page', () => {
    renderApp('/', anonymousAuth);

    expect(screen.getByRole('button', { name: 'Sign in' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Create form' })).not.toBeInTheDocument();
  });

  it('keeps non-admin users away from the config route', () => {
    renderApp('/config', authedAuth('user'));

    // Redirected back to the admin home; the config page stays hidden.
    expect(screen.getByRole('heading', { name: 'FMS Admin' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Form configuration' })).not.toBeInTheDocument();
  });
});
