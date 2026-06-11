import { useState } from 'react';
import { NavLink, useNavigate, useLocation } from 'react-router-dom';
import {
  LayoutDashboard, ScrollText, Users, Tags, FileText, GitMerge, Activity,
  ChevronDown, X, LogOut, UserCog,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import clsx from 'clsx';
import { Logo } from './Logo';
import { useAuth } from '../../contexts/AuthContext';

interface LeafItem {
  to: string;
  label: string;
  icon: LucideIcon;
  end?: boolean;
}

interface GroupItem {
  label: string;
  icon: LucideIcon;
  children: LeafItem[];
}

type NavItem = LeafItem | GroupItem;

const items: NavItem[] = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/hollerith', label: 'Holerite', icon: FileText },
  {
    label: 'Log',
    icon: ScrollText,
    children: [
      { to: '/logs', label: 'Utilizações', icon: ScrollText },
      { to: '/ambiente-logs', label: 'Monitoramento Interno', icon: Activity },
    ],
  },
  { to: '/employees', label: 'Funcionário', icon: Users },
  { to: '/information-types', label: 'Tipo de Informação', icon: Tags },
  { to: '/integracoes', label: 'Integrações', icon: GitMerge },
];

const leafClasses = (isActive: boolean) =>
  clsx(
    'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition',
    isActive ? 'bg-brand-50 text-brand-700' : 'text-ink-muted hover:bg-slate-100 hover:text-ink',
  );

interface SidebarProps {
  mobileOpen: boolean;
  onClose: () => void;
}

function NavGroup({ group, onClose }: { group: GroupItem; onClose: () => void }) {
  const location = useLocation();
  const hasActiveChild = group.children.some((c) => location.pathname === c.to);
  const [open, setOpen] = useState(hasActiveChild);
  const { icon: Icon } = group;

  return (
    <li>
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        className={clsx(
          'flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition',
          hasActiveChild ? 'text-ink' : 'text-ink-muted hover:bg-slate-100 hover:text-ink',
        )}
      >
        <Icon className="h-4 w-4" />
        <span className="flex-1 text-left">{group.label}</span>
        <ChevronDown className={clsx('h-4 w-4 transition-transform', open ? 'rotate-180' : '')} />
      </button>
      {open && (
        <ul className="mt-1 space-y-1 pl-4">
          {group.children.map(({ to, label, icon: ChildIcon, end }) => (
            <li key={to}>
              <NavLink to={to} end={end} onClick={onClose} className={({ isActive }) => leafClasses(isActive)}>
                <ChildIcon className="h-4 w-4" />
                {label}
              </NavLink>
            </li>
          ))}
        </ul>
      )}
    </li>
  );
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
            {items.map((item) =>
              'children' in item ? (
                <NavGroup key={item.label} group={item} onClose={onClose} />
              ) : (
                <li key={item.to}>
                  <NavLink
                    to={item.to}
                    end={item.end}
                    onClick={onClose}
                    className={({ isActive }) => leafClasses(isActive)}
                  >
                    <item.icon className="h-4 w-4" />
                    {item.label}
                  </NavLink>
                </li>
              ),
            )}
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
