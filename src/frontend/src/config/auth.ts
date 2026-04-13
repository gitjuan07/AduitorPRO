import { type Configuration, type PopupRequest, LogLevel } from '@azure/msal-browser';

export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_AZURE_CLIENT_ID || '',
    authority: `https://login.microsoftonline.com/${import.meta.env.VITE_AZURE_TENANT_ID || ''}`,
    redirectUri: import.meta.env.VITE_REDIRECT_URI || window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        if (level === LogLevel.Error) console.error('[MSAL]', message);
      },
    },
  },
};

// Login: solo openid + profile — sin scopes de API custom para evitar bloqueo por admin consent
export const loginRequest: PopupRequest = {
  scopes: ['openid', 'profile', 'email', 'User.Read'],
};

// Token para llamadas a la API — scope explícito con URI completo para que
// Azure AD presente la pantalla de consentimiento al usuario (no requiere admin consent)
const _clientId = import.meta.env.VITE_AZURE_CLIENT_ID || 'd33ab28c-95f5-4c60-acc4-b8acf32c1eac';
export const apiScopes = [
  `api://${_clientId}/Simulaciones.Read`,
];
