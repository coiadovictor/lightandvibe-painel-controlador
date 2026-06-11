import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  RefreshCw, CheckCircle2, XCircle, AlertTriangle, AlertOctagon,
  HelpCircle, Server, ChevronDown, MessageSquareWarning, Smartphone,
} from 'lucide-react';
import clsx from 'clsx';
import { api } from '@/lib/api';
import { PageHeader } from '@/components/ui/PageHeader';
import { Card } from '@/components/ui/Card';
import type { AmbienteOverview, ContainerHealth, Incident, ContainerLogs, WhatsAppInstance } from '@/types/api';

const REFRESH_MS = 5000;

// ---------- traduções amigáveis ----------

type Tone = 'green' | 'red' | 'amber' | 'slate';

const TONE_CLASSES: Record<Tone, { badge: string; dot: string; text: string }> = {
  green: { badge: 'bg-green-50 text-green-700 border-green-200', dot: 'bg-green-500', text: 'text-green-700' },
  red:   { badge: 'bg-rose-50 text-rose-700 border-rose-200',    dot: 'bg-rose-500',  text: 'text-rose-700' },
  amber: { badge: 'bg-amber-50 text-amber-700 border-amber-200', dot: 'bg-amber-500', text: 'text-amber-700' },
  slate: { badge: 'bg-slate-100 text-slate-600 border-slate-200',dot: 'bg-slate-400', text: 'text-slate-600' },
};

function statusInfo(status: string): { label: string; tone: Tone; Icon: typeof CheckCircle2 } {
  switch (status) {
    case 'running':    return { label: 'Funcionando', tone: 'green', Icon: CheckCircle2 };
    case 'restarting': return { label: 'Reiniciando', tone: 'amber', Icon: RefreshCw };
    case 'paused':     return { label: 'Pausado',     tone: 'amber', Icon: AlertTriangle };
    case 'exited':     return { label: 'Parado',      tone: 'red',   Icon: XCircle };
    case 'dead':       return { label: 'Fora do ar',  tone: 'red',   Icon: XCircle };
    case 'created':    return { label: 'Iniciando',   tone: 'slate', Icon: HelpCircle };
    case 'not_found':  return { label: 'Não encontrado', tone: 'slate', Icon: HelpCircle };
    default:           return { label: status || 'Desconhecido', tone: 'slate', Icon: HelpCircle };
  }
}

function severityInfo(sev: Incident['severity']): { tone: Tone; Icon: typeof AlertTriangle } {
  switch (sev) {
    case 'critical': return { tone: 'red',   Icon: AlertOctagon };
    case 'error':    return { tone: 'red',   Icon: AlertTriangle };
    default:         return { tone: 'amber', Icon: AlertTriangle };
  }
}

function fmtTime(ts?: string | null) {
  if (!ts) return '—';
  return new Date(ts).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' });
}

function fmtDateTime(ts?: string | null) {
  if (!ts) return '—';
  return new Date(ts).toLocaleString('pt-BR');
}

function dayLabel(ts?: string | null) {
  if (!ts) return 'Sem data';
  const d = new Date(ts);
  const today = new Date();
  const yesterday = new Date(today);
  yesterday.setDate(today.getDate() - 1);
  const sameDay = (a: Date, b: Date) =>
    a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
  if (sameDay(d, today)) return 'Hoje';
  if (sameDay(d, yesterday)) return 'Ontem';
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'long' });
}

const WINDOW_OPTIONS = [
  { hours: 6, label: '6h' },
  { hours: 24, label: '24h' },
  { hours: 48, label: '48h' },
  { hours: 168, label: '7d' },
  { hours: 360, label: '15d' },
  { hours: 720, label: '30d' },
];

function windowLabel(hours: number) {
  if (hours <= 48) return `${hours} horas`;
  return `${Math.round(hours / 24)} dias`;
}

// ---------- cartão de saúde ----------

function HealthCard({ c }: { c: ContainerHealth }) {
  const { label, tone, Icon } = statusInfo(c.status);
  const t = TONE_CLASSES[tone];
  return (
    <Card>
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-sm font-semibold text-ink truncate">{c.alias}</p>
          <p className="mt-0.5 text-xs text-ink-muted">
            {c.found ? `No ar desde ${fmtTime(c.startedAt)}` : 'Serviço não localizado'}
          </p>
        </div>
        <span className={clsx('inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-medium whitespace-nowrap', t.badge)}>
          <Icon className="h-3.5 w-3.5" />
          {label}
        </span>
      </div>

      {(c.restartCount > 0 || c.oomKilled) && (
        <div className="mt-3 flex flex-wrap gap-2">
          {c.oomKilled && (
            <span className="rounded-md bg-rose-50 px-2 py-0.5 text-xs font-medium text-rose-700">
              Caiu por falta de memória
            </span>
          )}
          {c.restartCount > 0 && (
            <span className="rounded-md bg-amber-50 px-2 py-0.5 text-xs font-medium text-amber-700">
              Reiniciou {c.restartCount}x
            </span>
          )}
        </div>
      )}
    </Card>
  );
}

// ---------- card de instância do WhatsApp ----------

function WhatsAppCard({ inst }: { inst: WhatsAppInstance }) {
  const connected = inst.connected;
  const tone: Tone = connected ? 'green' : 'red';
  const t = TONE_CLASSES[tone];
  return (
    <Card className={clsx(!connected && 'border-rose-300')}>
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex items-center gap-1.5">
            <Smartphone className="h-4 w-4 text-ink-muted" />
            <p className="truncate text-sm font-semibold text-ink">{inst.name}</p>
          </div>
          <p className="mt-0.5 truncate text-xs text-ink-muted">
            {inst.profileName ? `${inst.profileName}` : 'WhatsApp'}
            {inst.number ? ` · ${inst.number}` : ''}
          </p>
        </div>
        <span className={clsx('inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-medium whitespace-nowrap', t.badge)}>
          {connected ? <CheckCircle2 className="h-3.5 w-3.5" /> : <XCircle className="h-3.5 w-3.5" />}
          {connected ? 'Conectado' : 'Desconectado'}
        </span>
      </div>
      {!connected && (
        <p className="mt-3 rounded-md bg-rose-50 px-2 py-1 text-xs font-medium text-rose-700">
          Reconecte lendo o QR Code no Evolution
          {inst.disconnectedAt ? ` · caiu em ${fmtDateTime(inst.disconnectedAt)}` : ''}
        </p>
      )}
    </Card>
  );
}

// ---------- linha do tempo ----------

function TimelineItem({ inc }: { inc: Incident }) {
  const sev = severityInfo(inc.severity);
  const tone = sev.tone;
  const Icon = inc.type === 'whatsapp' ? MessageSquareWarning : sev.Icon;
  const t = TONE_CLASSES[tone];
  return (
    <div className="relative flex gap-4 pb-5 last:pb-0">
      {/* trilho vertical + bolinha */}
      <div className="relative flex flex-col items-center">
        <span className={clsx('z-10 mt-0.5 flex h-7 w-7 items-center justify-center rounded-full border-2 border-white shadow', t.dot)}>
          <Icon className="h-4 w-4 text-white" />
        </span>
        <span className="absolute top-7 h-full w-px bg-slate-200" />
      </div>
      <div className="min-w-0 flex-1 pb-1">
        <div className="flex flex-wrap items-baseline gap-x-2">
          <span className="text-sm font-medium text-ink">{fmtTime(inc.timestamp)}</span>
          <span className={clsx('rounded-md px-1.5 py-0.5 text-[11px] font-medium', t.badge)}>{inc.container}</span>
        </div>
        <p className="mt-0.5 text-sm text-ink">{inc.message}</p>
        {inc.detail && (
          <details className="mt-1">
            <summary className="cursor-pointer text-xs text-ink-muted hover:text-ink select-none">
              Ver detalhe técnico
            </summary>
            <pre className="mt-1 overflow-x-auto rounded-md bg-slate-900 px-3 py-2 text-xs text-slate-100 whitespace-pre-wrap break-all">
              {inc.detail}
            </pre>
          </details>
        )}
      </div>
    </div>
  );
}

function Timeline({ incidents }: { incidents: Incident[] }) {
  // agrupa por dia, preservando a ordem (já vem do mais recente pro mais antigo)
  const groups: { day: string; items: Incident[] }[] = [];
  for (const inc of incidents) {
    const day = dayLabel(inc.timestamp);
    const last = groups[groups.length - 1];
    if (last && last.day === day) last.items.push(inc);
    else groups.push({ day, items: [inc] });
  }

  return (
    <div className="space-y-6">
      {groups.map((g) => (
        <div key={g.day}>
          <p className="mb-3 text-xs font-semibold uppercase tracking-wide text-ink-muted">{g.day}</p>
          <div>
            {g.items.map((inc, i) => <TimelineItem key={i} inc={inc} />)}
          </div>
        </div>
      ))}
    </div>
  );
}

// ---------- modo técnico: tail de log ----------

function TechnicalLogs({ containers }: { containers: ContainerHealth[] }) {
  const monitored = containers.filter((c) => c.found);
  const [selected, setSelected] = useState(monitored[0]?.alias ?? '');
  const [filter, setFilter] = useState('');

  const { data, isFetching } = useQuery({
    queryKey: ['ambiente-logs', selected],
    enabled: !!selected,
    refetchInterval: REFRESH_MS,
    queryFn: async () => {
      const res = await api.get<ContainerLogs>(
        `/ambiente/logs?container=${encodeURIComponent(selected)}&tail=300`,
      );
      return res.data;
    },
  });

  const lines = (data?.lines ?? []).filter((l) =>
    filter ? l.text.toLowerCase().includes(filter.toLowerCase()) : true,
  );

  return (
    <details className="mt-8 rounded-xl border border-slate-200 bg-white shadow-sm">
      <summary className="flex cursor-pointer items-center gap-2 px-5 py-4 text-sm font-medium text-ink select-none">
        <ChevronDown className="h-4 w-4 transition-transform" />
        Modo técnico — log detalhado (para a equipe de tecnologia)
      </summary>

      <div className="border-t border-slate-100 p-5">
        <div className="mb-3 flex flex-wrap items-center gap-3">
          <select
            value={selected}
            onChange={(e) => setSelected(e.target.value)}
            className="rounded-lg border border-gray-200 bg-white px-3 py-2 text-sm text-ink focus:outline-none focus:ring-2 focus:ring-brand-300"
          >
            {monitored.map((c) => (
              <option key={c.alias} value={c.alias}>{c.alias}</option>
            ))}
          </select>
          <input
            type="text"
            placeholder="Filtrar no log…"
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            className="flex-1 min-w-[12rem] rounded-lg border border-gray-200 bg-white px-3 py-2 text-sm text-ink placeholder:text-ink-muted focus:outline-none focus:ring-2 focus:ring-brand-300"
          />
          {isFetching && <RefreshCw className="h-4 w-4 animate-spin text-ink-muted" />}
        </div>

        {data && !data.available ? (
          <p className="text-sm text-amber-700">{data.message}</p>
        ) : (
          <div className="max-h-96 overflow-auto rounded-lg bg-slate-900 p-3 font-mono text-xs leading-relaxed">
            {lines.length === 0 ? (
              <p className="text-slate-400">Nenhuma linha de log para exibir.</p>
            ) : (
              lines.map((l, i) => (
                <div key={i} className={clsx('whitespace-pre-wrap break-all', l.stream === 'stderr' ? 'text-rose-300' : 'text-slate-100')}>
                  <span className="text-slate-500">{l.timestamp ? fmtTime(l.timestamp) + ' ' : ''}</span>
                  {l.text}
                </div>
              ))
            )}
          </div>
        )}
      </div>
    </details>
  );
}

// ---------- página ----------

export function EnvironmentLogsPage() {
  const [autoRefresh, setAutoRefresh] = useState(true);
  const [windowHours, setWindowHours] = useState(48);

  const { data, isLoading, isError, isFetching, dataUpdatedAt, refetch } = useQuery({
    queryKey: ['ambiente-overview', windowHours],
    refetchInterval: autoRefresh ? REFRESH_MS : false,
    queryFn: async () => {
      const res = await api.get<AmbienteOverview>(`/ambiente/overview?hours=${windowHours}`);
      return res.data;
    },
  });

  // Estado AUTORITATIVO (consulta direta à Evolution) — tem prioridade.
  const waInstances = data?.whatsApp ?? [];
  const waDisconnected = waInstances.filter((i) => !i.connected);
  const waAuthoritative = !!data?.whatsAppAvailable;

  // Fallback por LOG (só quando a checagem autoritativa não está disponível).
  const whatsappAlerts = (data?.incidents ?? []).filter((i) => i.type === 'whatsapp');
  const lastWhatsappAlert = whatsappAlerts[0]; // já vem do mais recente pro mais antigo

  return (
    <div>
      <PageHeader
        title="Logs Internos do Ambiente"
        description="Acompanhe a saúde dos serviços por trás do atendimento (WhatsApp, automações e banco de dados) e veja, em linguagem clara, o que aconteceu de errado."
      />

      {/* Alerta AUTORITATIVO de WhatsApp desconectado (estado atual real) */}
      {waAuthoritative && waDisconnected.length > 0 && (
        <div className="mb-5 flex items-start gap-3 rounded-xl border-2 border-rose-400 bg-rose-50 p-4 shadow-sm">
          <MessageSquareWarning className="mt-0.5 h-6 w-6 shrink-0 text-rose-600" />
          <div className="min-w-0">
            <p className="text-sm font-bold text-rose-800">
              WhatsApp DESCONECTADO agora — ação necessária!
            </p>
            <p className="mt-1 text-sm text-rose-700">
              {waDisconnected.map((i) => i.name + (i.number ? ` (${i.number})` : '')).join(', ')}
              {' '}está fora do ar.{' '}
              <span className="font-medium">Acesse o Evolution API e reconecte lendo o QR Code.</span>
            </p>
          </div>
        </div>
      )}

      {/* Fallback por log: só quando a checagem autoritativa não está disponível */}
      {!waAuthoritative && lastWhatsappAlert && (
        <div className="mb-5 flex items-start gap-3 rounded-xl border-2 border-rose-300 bg-rose-50 p-4 shadow-sm">
          <MessageSquareWarning className="mt-0.5 h-6 w-6 shrink-0 text-rose-600" />
          <div className="min-w-0">
            <p className="text-sm font-bold text-rose-800">WhatsApp pode estar desconectado!</p>
            <p className="mt-1 text-sm text-rose-700">
              Detectamos sinal de queda/desconexão do WhatsApp no Evolution
              {lastWhatsappAlert.timestamp ? ` por volta das ${fmtTime(lastWhatsappAlert.timestamp)}` : ''}.
              {' '}<span className="font-medium">Acesse o Evolution API e reconecte lendo o QR Code novamente.</span>
            </p>
            <p className="mt-1 text-xs text-rose-600">
              {whatsappAlerts.length} sinal(is) na janela selecionada. Se já reconectou, este alerta some quando a janela não tiver mais o evento.
            </p>
          </div>
        </div>
      )}

      <div className="mb-5 flex flex-wrap items-center justify-between gap-3">
        <label className="flex cursor-pointer items-center gap-2 text-sm text-ink">
          <input
            type="checkbox"
            checked={autoRefresh}
            onChange={(e) => setAutoRefresh(e.target.checked)}
            className="h-4 w-4 rounded border-gray-300 text-brand-600 focus:ring-brand-300"
          />
          Atualizar automaticamente
        </label>
        <div className="flex items-center gap-3 text-xs text-ink-muted">
          {dataUpdatedAt > 0 && <span>Atualizado às {fmtTime(new Date(dataUpdatedAt).toISOString())}</span>}
          <button
            type="button"
            onClick={() => refetch()}
            className="inline-flex items-center gap-1.5 rounded-lg border border-slate-200 bg-white px-3 py-1.5 font-medium text-ink hover:bg-slate-50"
          >
            <RefreshCw className={clsx('h-3.5 w-3.5', isFetching && 'animate-spin')} />
            Atualizar agora
          </button>
        </div>
      </div>

      {isError && (
        <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
          Não foi possível carregar as informações do ambiente.
        </div>
      )}

      {data && !data.available && (
        <div className="mb-6 flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-800">
          <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0" />
          <div>
            <p className="font-medium">Monitoramento ainda não está ativo.</p>
            <p className="mt-0.5">{data.message}</p>
          </div>
        </div>
      )}

      {/* Status do WhatsApp (estado autoritativo) */}
      {waAuthoritative && waInstances.length > 0 && (
        <div className="mb-6">
          <div className="mb-3 flex items-center gap-2">
            <Smartphone className="h-4 w-4 text-ink-muted" />
            <h2 className="text-sm font-semibold uppercase tracking-wide text-ink-muted">
              WhatsApp — conexão das instâncias
            </h2>
          </div>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {waInstances.map((inst) => <WhatsAppCard key={inst.name} inst={inst} />)}
          </div>
        </div>
      )}

      {waAuthoritative === false && data?.whatsAppMessage && (
        <div className="mb-6 flex items-start gap-3 rounded-xl border border-slate-200 bg-slate-50 p-3 text-xs text-ink-muted">
          <Smartphone className="mt-0.5 h-4 w-4 shrink-0" />
          <span>Status do WhatsApp em tempo real indisponível: {data.whatsAppMessage}</span>
        </div>
      )}

      {/* Cartões de saúde dos serviços */}
      <div className="mb-3 flex items-center gap-2">
        <Server className="h-4 w-4 text-ink-muted" />
        <h2 className="text-sm font-semibold uppercase tracking-wide text-ink-muted">
          Saúde dos serviços
        </h2>
      </div>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {isLoading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <Card key={i}><div className="h-12 animate-pulse rounded bg-slate-100" /></Card>
          ))
        ) : (
          (data?.containers ?? []).map((c) => <HealthCard key={c.alias} c={c} />)
        )}
      </div>

      {/* Linha do tempo */}
      <div className="mt-8">
        <div className="mb-3 flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <Server className="h-4 w-4 text-ink-muted" />
            <h2 className="text-sm font-semibold uppercase tracking-wide text-ink-muted">
              Linha do tempo — o que aconteceu nas últimas {windowLabel(windowHours)}
            </h2>
          </div>
          <div className="inline-flex rounded-lg border border-slate-200 bg-white p-0.5">
            {WINDOW_OPTIONS.map((w) => (
              <button
                key={w.hours}
                type="button"
                onClick={() => setWindowHours(w.hours)}
                className={clsx(
                  'rounded-md px-3 py-1 text-xs font-medium transition',
                  windowHours === w.hours ? 'bg-brand-600 text-white' : 'text-ink-muted hover:bg-slate-100',
                )}
              >
                {w.label}
              </button>
            ))}
          </div>
        </div>
        <Card>
          {data && data.incidents.length === 0 ? (
            <div className="flex items-center gap-2 text-sm text-green-700">
              <CheckCircle2 className="h-5 w-5" />
              Nenhum problema detectado nas últimas {windowLabel(windowHours)}. Tudo funcionando normalmente.
            </div>
          ) : (
            <Timeline incidents={data?.incidents ?? []} />
          )}
        </Card>
      </div>

      {/* Modo técnico */}
      {data?.available && data.containers.some((c) => c.found) && (
        <TechnicalLogs containers={data.containers} />
      )}
    </div>
  );
}
