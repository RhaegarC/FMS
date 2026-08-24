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

function authedAuth(): AuthState {
  return {
    isAuthenticated: true,
    user: { id: 1, name: 'Ada', email: 'ada@example.com', role: 'user' },
    login: () => {},
    logout: () => {},
    getAccessToken: async () => null,
  };
}

describe('User App', () => {
  it('renders the user home for an authenticated user', () => {
    renderApp('/', authedAuth());

    expect(screen.getByRole('heading', { name: 'FMS' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Feedback form' })).toBeInTheDocument();
    expect(screen.getByText(/\/api\/v1\/health/)).toBeInTheDocument();
  });

  it('shows the signed-in email and signs out', () => {
    const logout = vi.fn();
    renderApp('/', { ...authedAuth(), logout });

    expect(screen.getByTestId('user-email')).toHaveTextContent('ada@example.com');
    screen.getByRole('button', { name: 'Sign out' }).click();
    expect(logout).toHaveBeenCalled();
  });

  it('redirects unauthenticated users to the login page', () => {
    renderApp('/', anonymousAuth);

    expect(screen.getByRole('button', { name: 'Sign in' })).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Feedback form' })).not.toBeInTheDocument();
  });
});
