import { NavLink, Outlet } from 'react-router-dom';
import { useMsal } from '@azure/msal-react';
import {
  LayoutDashboard, PlayCircle, AlertTriangle, FileCheck,
  Brain, Plug, FileText, BookOpen, LogOut, ClipboardList, Upload, Database
} from 'lucide-react';

const DEV_MODE = !import.meta.env.VITE_AZURE_CLIENT_ID;

const navItems = [
  { to: '/dashboard',     label: 'Dashboard',       icon: <LayoutDashboard size={18} /> },
  { to: '/simulaciones',  label: 'Simulaciones',     icon: <PlayCircle      size={18} /> },
  { to: '/hallazgos',     label: 'Hallazgos',        icon: <AlertTriangle   size={18} /> },
  { to: '/planes-accion', label: 'Planes de acción', icon: <ClipboardList   size={18} /> },
  { to: '/evidencias',    label: 'Evidencias',       icon: <FileCheck       size={18} /> },
  { to: '/ia',            label: 'Agente IA',        icon: <Brain           size={18} /> },
  { to: '/conectores',    label: 'Conectores',       icon: <Plug            size={18} /> },
  { to: '/politicas',     label: 'Políticas',        icon: <FileText        size={18} /> },
  { to: '/bitacora',      label: 'Bitácora',         icon: <BookOpen        size={18} /> },
  { to: '/cargas',            label: 'Cargas',             icon: <Upload    size={18} /> },
  { to: '/base-conocimiento', label: 'Base Conocimiento',  icon: <Database  size={18} /> },
];

// Sub-componente que usa useMsal — solo se monta dentro de MsalProvider
function MsalUserFooter() {
  const { instance, accounts } = useMsal();
  const user = accounts[0];
  return (
    <div className="px-4 py-4 border-t border-gray-700">
      <p className="text-xs text-gray-400 truncate">{user?.username}</p>
      <p className="text-[10px] text-gray-500 truncate">ILG Logistics</p>
      <button
        onClick={() => instance.logoutRedirect()}
        className="mt-2 flex items-center gap-2 text-xs text-gray-400 hover:text-white transition-colors"
      >
        <LogOut size={14} />
        Cerrar sesión
      </button>
    </div>
  );
}

function DevUserFooter() {
  return (
    <div className="px-4 py-4 border-t border-gray-700">
      <p className="text-xs text-gray-400 truncate">demo@ilglogistics.com</p>
      <p className="text-[10px] text-gray-500 truncate">ILG Logistics — Modo Demo</p>
    </div>
  );
}

export function AppLayout() {
  return (
    <div className="flex h-screen bg-gray-100">
      {/* Sidebar */}
      <aside className="w-56 bg-gray-900 text-white flex flex-col shadow-xl">
        <div className="px-4 py-5 border-b border-gray-700">
          <div className="flex items-center gap-2">
            <div className="w-7 h-7 bg-red-600 rounded-lg flex items-center justify-center shrink-0">
              <span className="text-white font-black text-sm">A</span>
            </div>
            <div>
              <h1 className="text-sm font-bold text-white leading-tight">AuditorPRO TI</h1>
              <p className="text-[10px] text-gray-400 leading-tight">Auditoría Preventiva</p>
            </div>
          </div>
        </div>

        <nav className="flex-1 px-2 py-4 space-y-0.5 overflow-y-auto">
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                `flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm transition-colors ${
                  isActive
                    ? 'bg-red-600 text-white'
                    : 'text-gray-300 hover:bg-gray-800 hover:text-white'
                }`
              }
            >
              {item.icon}
              {item.label}
            </NavLink>
          ))}
        </nav>

        {DEV_MODE ? <DevUserFooter /> : <MsalUserFooter />}
      </aside>

      {/* Main content */}
      <main className="flex-1 overflow-auto">
        <Outlet />
      </main>
    </div>
  );
}
