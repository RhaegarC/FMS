import { AuthProvider, buildMsalConfig } from '@fms/auth';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import './index.css';

// Entra settings are supplied per app via Vite env (see .env.example). Fails fast
// if they're missing — the portal requires login.
const { msalConfig, scopes } = buildMsalConfig(import.meta.env);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider config={msalConfig} scopes={scopes}>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </AuthProvider>
  </StrictMode>,
);
