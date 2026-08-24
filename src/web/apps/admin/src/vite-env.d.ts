/// <reference types="vite/client" />

declare global {
  interface ImportMetaEnv {
    readonly VITE_ENTRA_CLIENT_ID?: string;
    readonly VITE_ENTRA_TENANT_ID?: string;
    readonly VITE_ENTRA_REDIRECT_URI?: string;
    readonly VITE_ENTRA_SCOPE?: string;
  }
}

export {};
