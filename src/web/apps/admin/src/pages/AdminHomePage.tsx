import { buildUrl } from '@fms/api-client';
import { useAuth } from '@fms/auth';
import { APP_NAME } from '@fms/types';
import { Button } from '@fms/ui';

/** Landing page for authenticated admins (form configuration portal). */
export function AdminHomePage() {
  const { user, logout } = useAuth();

  return (
    <main>
      <header className="app-header">
        <span data-testid="user-email">{user?.email}</span>
        <Button onClick={logout}>Sign out</Button>
      </header>
      <h1>{APP_NAME} Admin</h1>
      <p>Form configuration portal.</p>
      <p>API: {buildUrl('/health')}</p>
      <Button>Create form</Button>
    </main>
  );
}
