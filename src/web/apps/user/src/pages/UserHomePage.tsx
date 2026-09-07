import { buildUrl } from '@fms/api-client';
import { useAuth } from '@fms/auth';
import { FormRenderer } from '@fms/form-renderer';
import { APP_NAME } from '@fms/types';

/** Landing page for authenticated users (fill & submit forms). */
export function UserHomePage() {
  const { user, logout } = useAuth();

  return (
    <main>
      <header className="app-header">
        <span data-testid="user-email">{user?.email}</span>
        <button type="button" onClick={logout}>
          Sign out
        </button>
      </header>
      <h1>{APP_NAME}</h1>
      <p>API: {buildUrl('/health')}</p>
      <FormRenderer schema={{ title: 'Feedback form', properties: {} }} />
    </main>
  );
}
