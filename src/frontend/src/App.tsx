import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { MsalProvider, AuthenticatedTemplate, UnauthenticatedTemplate } from '@azure/msal-react';
import { Toaster } from 'sonner';
import { msalInstance } from './store/authStore';
import { AppLayout } from './components/layout/AppLayout';
import { Dashboard } from './pages/Dashboard';
import { Simulaciones } from './pages/Simulaciones';
import { Hallazgos } from './pages/Hallazgos';
import { Evidencias } from './pages/Evidencias';
import { PlanesAccion } from './pages/PlanesAccion';
import { AgenteIA } from './pages/AgenteIA';
import { Conectores } from './pages/Conectores';
import { Politicas } from './pages/Politicas';
import { Bitacora } from './pages/Bitacora';
import { Cargas } from './pages/Cargas';
import { SimulacionDetalle } from './pages/SimulacionDetalle';
import { BaseConocimiento } from './pages/BaseConocimiento';
import { Sociedades } from './pages/Sociedades';
import { loginRequest } from './config/auth';

// Modo demo: sin Azure AD configurado, muestra el app directamente
const DEV_MODE = !import.meta.env.VITE_AZURE_CLIENT_ID;

function AppRoutes() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<AppLayout />}>
          <Route index element={<Navigate to="/dashboard" replace />} />
          <Route path="dashboard"        element={<Dashboard />} />
          <Route path="simulaciones"     element={<Simulaciones />} />
          <Route path="simulaciones/:id" element={<SimulacionDetalle />} />
          <Route path="hallazgos"        element={<Hallazgos />} />
          <Route path="evidencias"       element={<Evidencias />} />
          <Route path="planes-accion"    element={<PlanesAccion />} />
          <Route path="ia"               element={<AgenteIA />} />
          <Route path="conectores"       element={<Conectores />} />
          <Route path="politicas"        element={<Politicas />} />
          <Route path="bitacora"         element={<Bitacora />} />
          <Route path="cargas"           element={<Cargas />} />
              <Route path="base-conocimiento" element={<BaseConocimiento />} />
          <Route path="sociedades"        element={<Sociedades />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

function LoginScreen() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-gray-900 to-red-900 flex items-center justify-center">
      <div className="bg-white rounded-2xl shadow-2xl p-8 max-w-sm w-full text-center">
        <div className="mb-6">
          <div className="w-12 h-12 bg-red-600 rounded-xl flex items-center justify-center mx-auto mb-3">
            <span className="text-white font-black text-lg">A</span>
          </div>
          <h1 className="text-2xl font-bold text-gray-900">AuditorPRO TI</h1>
          <p className="text-sm text-gray-500 mt-1">Plataforma de Auditoría Preventiva Inteligente</p>
        </div>
        <p className="text-sm text-gray-600 mb-6">
          Autenticación corporativa con Microsoft Entra ID
        </p>
        <button
          onClick={() => msalInstance.loginRedirect(loginRequest)}
          className="w-full bg-red-600 text-white py-2.5 rounded-xl font-medium hover:bg-red-700 transition"
        >
          Iniciar sesión con Microsoft
        </button>
        <p className="text-xs text-gray-400 mt-4">ILG Logistics — Uso interno confidencial</p>
      </div>
    </div>
  );
}

export default function App() {
  // Sin Azure AD configurado: modo demo directo
  if (DEV_MODE) {
    return (
      <>
        <Toaster position="top-right" richColors />
        <AppRoutes />
      </>
    );
  }

  return (
    <MsalProvider instance={msalInstance}>
      <Toaster position="top-right" richColors />
      <AuthenticatedTemplate>
        <AppRoutes />
      </AuthenticatedTemplate>
      <UnauthenticatedTemplate>
        <LoginScreen />
      </UnauthenticatedTemplate>
    </MsalProvider>
  );
}
