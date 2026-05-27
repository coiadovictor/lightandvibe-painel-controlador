import { Menu } from 'lucide-react';

interface TopbarProps {
  onMenuClick: () => void;
}

export function Topbar({ onMenuClick }: TopbarProps) {
  return (
    <header className="flex h-16 items-center gap-3 border-b border-slate-200 bg-white px-4 md:px-8">
      <button
        type="button"
        className="md:hidden text-ink-muted"
        aria-label="Abrir menu"
        onClick={onMenuClick}
      >
        <Menu className="h-5 w-5" />
      </button>
      <div className="flex-1" />
      <div className="text-xs font-medium text-ink-muted">
        <span className="text-ink">Light</span>
        <span className="mx-0.5 text-accent-green">&amp;</span>
        <span className="text-accent-blue">Vibe</span>
        <span className="ml-1.5 text-[10px] uppercase tracking-wider text-ink-muted">
          AI Automation
        </span>
      </div>
    </header>
  );
}
