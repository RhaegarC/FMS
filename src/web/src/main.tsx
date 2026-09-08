import React from 'react'
import ReactDOM from 'react-dom/client'
import { MsalProvider } from '@azure/msal-react'
import App from './App'
import { authConfigured, createMsalInstance } from './auth/authConfig'
import './index.css'

/**
 * Bootstrap: build the single MSAL instance, finish any in-flight redirect
 * sign-in (the browser returning from login.microsoftonline.com), and only then
 * render — so the app never flashes an unauthenticated frame mid-login.
 */
async function bootstrap() {
  const container = document.getElementById('root')!

  if (!authConfigured) {
    ReactDOM.createRoot(container).render(
      <React.StrictMode>
        <ConfigMissing />
      </React.StrictMode>,
    )
    return
  }

  const msalInstance = createMsalInstance()
  try {
    await msalInstance.initialize()
    await msalInstance.handleRedirectPromise()
    if (!msalInstance.getActiveAccount() && msalInstance.getAllAccounts().length > 0) {
      msalInstance.setActiveAccount(msalInstance.getAllAccounts()[0])
    }
    msalInstance.enableAccountStorageEvents()
  } catch (err) {
    // A cancelled/failed redirect login. The app still renders (signed out) and
    // the user can sign in again from the home page.
    console.error('MSAL startup failed:', err)
  }

  ReactDOM.createRoot(container).render(
    <React.StrictMode>
      <MsalProvider instance={msalInstance}>
        <App />
      </MsalProvider>
    </React.StrictMode>,
  )
}

function ConfigMissing() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-background text-foreground p-6">
      <div className="max-w-sm bg-card border border-border rounded-lg p-6 mx-4">
        <h1 className="text-base font-semibold mb-2">Entra ID is not configured</h1>
        <p className="text-xs text-muted-foreground leading-relaxed">
          Set <code className="font-mono">VITE_ENTRA_TENANT_ID</code> and{' '}
          <code className="font-mono">VITE_ENTRA_CLIENT_ID</code> in{' '}
          <code className="font-mono">src/web/.env</code> (see{' '}
          <code className="font-mono">src/web/.env.example</code>) to sign in with your work
          account.
        </p>
      </div>
    </div>
  )
}

bootstrap()
