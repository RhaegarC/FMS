import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from './authContext';

type LoginPageProps = {
  title?: string;
};

/**
 * Public page that signs the user in via MSAL's redirect flow. Already
 * authenticated visitors are bounced back to the protected home route.
 */
export function LoginPage({ title = 'FMS' }: LoginPageProps) {
  const { isAuthenticated, login } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (isAuthenticated) navigate('/', { replace: true });
  }, [isAuthenticated, navigate]);

  return (
    <main>
      <h1>{title}</h1>
      <p>Sign in with your organizational account.</p>
      <button type="button" onClick={login}>
        Sign in
      </button>
    </main>
  );
}
