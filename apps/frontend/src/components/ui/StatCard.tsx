import type { LucideIcon } from 'lucide-react';
import clsx from 'clsx';
import { Card } from './Card';

type Tone = 'brand' | 'green' | 'blue' | 'neutral';

interface StatCardProps {
  label: string;
  value: number | string;
  icon: LucideIcon;
  loading?: boolean;
  hint?: string;
  tone?: Tone;
}

const toneStyles: Record<Tone, string> = {
  brand: 'bg-brand-50 text-brand-600',
  green: 'bg-accent-green-soft text-emerald-700',
  blue: 'bg-accent-blue-soft text-accent-blue',
  neutral: 'bg-slate-100 text-ink-muted',
};

export function StatCard({ label, value, icon: Icon, loading, hint, tone = 'brand' }: StatCardProps) {
  return (
    <Card>
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium text-ink-muted">{label}</span>
        <span className={clsx('flex h-9 w-9 items-center justify-center rounded-lg', toneStyles[tone])}>
          <Icon className="h-5 w-5" />
        </span>
      </div>
      <div className="mt-4 text-3xl font-semibold text-ink">
        {loading ? <span className="text-slate-300">—</span> : value}
      </div>
      {hint && <p className="mt-1 text-xs text-ink-muted">{hint}</p>}
    </Card>
  );
}
