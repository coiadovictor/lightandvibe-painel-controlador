import { NavLink, useNavigate } from 'react-router-dom';
import { LayoutDashboard, ScrollText, Users, Tags, FileText, GitMerge, X, LogOut, UserCog } from 'lucide-react';
import clsx from 'clsx';
import { Logo } from './Logo';
import { useAuth } from '../../contexts/AuthContext';

const items = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/hollerith', label: 'Holerite', icon: FileText, end: false },
  { to: '/logs', label: 'Logs', icon: ScrollText, end: false },
  { to: '/employees', label: 'Funcionário', icon: Users, end: false },
  { to: '/information-types', label: 'Tipo de Informação', icon: Tags, end: false },
  { to: '/integracoes', label: 'Integrações', icon: GitMerge, end: false },
];

interface SidebarProps {
  mobileOpen: boolean;
  onClose: () => void;
}

export function Sidebar({ mobileOpen, onClose }: SidebarProps) {
  const { logout, user } = useAuth();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    navigate('/login', { replace: true });
  }

  return (
    <>
      {mobileOpen && (
        <div
          className="fixed inset-0 z-30 bg-ink/50 md:hidden"
          onClick={onClose}
          aria-hidden
        />
      )}
      <aside
        className={clsx(
          'fixed inset-y-0 left-0 z-40 w-64 transform border-r border-slate-200 bg-white transition-transform md:static md:translate-x-0 flex flex-col',
          mobileOpen ? 'translate-x-0' : '-translate-x-full',
        )}
      >
        <div className="flex h-16 items-center justify-between px-5 border-b border-slate-100">
          <Logo />
          <button
            type="button"
            className="md:hidden text-ink-muted"
            aria-label="Fechar menu"
            onClick={onClose}
          >
            <X className="h-5 w-5" />
          </button>
        </div>
        <nav className="px-3 py-3 flex-1">
          <p className="px-3 pb-2 text-[10px] font-semibold uppercase tracking-wider text-ink-muted">
            Painel Controlador
          </p>
          <ul className="space-y-1">
            {items.map(({ to, label, icon: Icon, end }) => (
              <li key={to}>
                <NavLink
                  to={to}
                  end={end}
                  onClick={onClose}
                  className={({ isActive }) =>
                    clsx(
                      'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition',
                      isActive
                        ? 'bg-brand-50 text-brand-700'
                        : 'text-ink-muted hover:bg-slate-100 hover:text-ink',
                    )
                  }
                >
                  <Icon className="h-4 w-4" />
                  {label}
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>

        <div className="border-t border-slate-100 px-3 py-3 space-y-1">
          <NavLink
            to="/profile"
            onClick={onClose}
            className={({ isActive }) =>
              `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition ${
                isActive
                  ? 'bg-brand-50 text-brand-700'
                  : 'text-ink-muted hover:bg-slate-100 hover:text-ink'
              }`
            }
          >
            <UserCog className="h-4 w-4 shrink-0" />
            <span className="truncate">{user?.username}</span>
          </NavLink>
          <button
            type="button"
            onClick={handleLogout}
            className="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-ink-muted hover:bg-red-50 hover:text-red-600 transition"
          >
            <LogOut className="h-4 w-4" />
            Sair
          </button>
        </div>
      </aside>
    </>
  );
}
