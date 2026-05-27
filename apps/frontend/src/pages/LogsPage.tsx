import { useQuery } from '@tanstack/react-query';
import { MessageSquare, Users, Clock, Zap, Bot, User } from 'lucide-react';
import { api } from '../lib/api';

interface LogStats {
  totalMensagens: number;
  sessoesUnicas: number;
  mensagensHoje: number;
  mensagensUltimaHora: number;
  mensagensHumanas: number;
  mensagensIA: number;
  mediaMensagensPorSessao: number;
  ultimasSessoes: {
    sessionId: string;
    totalMensagens: number;
    ultimaMensagem: string | null;
    telefone: string | null;
    nomeFuncionario: string | null;
  }[];
}

interface LogMessage {
  id: string;
  sessionId: string;
  tipo: string;
  conteudo: string;
  criadoEm: string;
  nomeFuncionario: string | null;
}

function StatCard({
  label, value, icon: Icon, color,
}: {
  label: string;
  value: string | number;
  icon: React.ElementType;
  color: 'brand' | 'green' | 'blue' | 'yellow' | 'neutral';
}) {
  const colors = {
    brand:   { bg: 'bg-brand-50',  text: 'text-brand-600',  val: 'text-brand-700' },
    green:   { bg: 'bg-green-50',  text: 'text-green-600',  val: 'text-green-700' },
    blue:    { bg: 'bg-blue-50',   text: 'text-blue-600',   val: 'text-blue-700' },
    yellow:  { bg: 'bg-yellow-50', text: 'text-yellow-600', val: 'text-yellow-700' },
    neutral: { bg: 'bg-gray-50',   text: 'text-gray-500',   val: 'text-gray-700' },
  }[color];

  return (
    <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
      <div className="flex items-center justify-between mb-3">
        <span className="text-sm font-medium text-ink-muted">{label}</span>
        <span className={`p-2 rounded-lg ${colors.bg}`}>
          <Icon className={`h-4 w-4 ${colors.text}`} />
        </span>
      </div>
      <p className={`text-2xl font-bold ${colors.val}`}>{value}</p>
    </div>
  );
}

function typeBadge(tipo: string) {
  if (tipo === 'human')
    return <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-blue-50 text-blue-700"><User className="h-3 w-3" />Usuário</span>;
  if (tipo === 'ai' || tipo === 'assistant')
    return <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium bg-green-50 text-green-700"><Bot className="h-3 w-3" />IA</span>;
  return <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-600">{tipo}</span>;
}

function fmt(dt: string | null) {
  if (!dt) return '—';
  return new Date(dt).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: '2-digit', hour: '2-digit', minute: '2-digit' });
}

function ContactLabel({ nome, telefone, sessionId }: { nome: string | null; telefone: string | null; sessionId: string }) {
  if (nome) return <span className="font-medium text-ink">{nome}</span>;
  if (telefone) return <span className="text-ink-muted">{telefone}</span>;
  return <span className="font-mono text-ink-muted truncate max-w-[150px]" title={sessionId}>{sessionId}</span>;
}

export function LogsPage() {
  const { data: stats, isLoading: loadingStats } = useQuery<LogStats>({
    queryKey: ['logs-stats'],
    queryFn: () => api.get<LogStats>('/logs/stats').then(r => r.data),
    refetchInterval: 60_000,
  });

  const { data: messages, isLoading: loadingMessages } = useQuery<LogMessage[]>({
    queryKey: ['logs-messages'],
    queryFn: () => api.get<LogMessage[]>('/logs/messages?limit=100').then(r => r.data),
    refetchInterval: 60_000,
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-ink">Logs de Conversas</h1>
        <p className="text-sm text-ink-muted mt-1">Métricas e histórico do banco n8n.</p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
        <StatCard label="Total Mensagens"   value={loadingStats ? '—' : (stats?.totalMensagens ?? 0).toLocaleString('pt-BR')}      icon={MessageSquare} color="brand"   />
        <StatCard label="Sessões Únicas"    value={loadingStats ? '—' : (stats?.sessoesUnicas ?? 0).toLocaleString('pt-BR')}       icon={Users}         color="blue"    />
        <StatCard label="Mensagens Hoje"    value={loadingStats ? '—' : (stats?.mensagensHoje ?? 0).toLocaleString('pt-BR')}       icon={Clock}         color="green"   />
        <StatCard label="Última 1h"         value={loadingStats ? '—' : (stats?.mensagensUltimaHora ?? 0).toLocaleString('pt-BR')} icon={Zap}           color="yellow"  />
        <StatCard label="Msgs Usuário"      value={loadingStats ? '—' : (stats?.mensagensHumanas ?? 0).toLocaleString('pt-BR')}    icon={User}          color="neutral" />
        <StatCard label="Respostas IA"      value={loadingStats ? '—' : (stats?.mensagensIA ?? 0).toLocaleString('pt-BR')}         icon={Bot}           color="neutral" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Últimas Sessões */}
        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5">
          <h2 className="text-sm font-semibold text-ink mb-1">Últimas Sessões</h2>
          <p className="text-xs text-ink-muted mb-4">
            Média: <span className="font-medium text-ink">{stats?.mediaMensagensPorSessao ?? 0} msg/sessão</span>
          </p>
          <div className="space-y-2">
            {loadingStats && <p className="text-xs text-ink-muted">Carregando...</p>}
            {stats?.ultimasSessoes.map(s => (
              <div key={s.sessionId} className="flex items-center justify-between py-1.5 border-b border-gray-50 last:border-0">
                <div className="min-w-0">
                  <div className="text-xs truncate max-w-[160px]">
                    <ContactLabel nome={s.nomeFuncionario} telefone={s.telefone} sessionId={s.sessionId} />
                  </div>
                  <p className="text-[10px] text-ink-muted">{fmt(s.ultimaMensagem)}</p>
                </div>
                <span className="ml-2 shrink-0 text-xs font-semibold text-brand-600 bg-brand-50 px-2 py-0.5 rounded-full">
                  {s.totalMensagens} msg
                </span>
              </div>
            ))}
            {!loadingStats && !stats?.ultimasSessoes.length && (
              <p className="text-xs text-ink-muted">Nenhuma sessão encontrada.</p>
            )}
          </div>
        </div>

        {/* Mensagens Recentes */}
        <div className="lg:col-span-2 bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="px-5 py-4 border-b border-gray-100">
            <h2 className="text-sm font-semibold text-ink">Mensagens Recentes</h2>
            <p className="text-xs text-ink-muted">Últimas 100 mensagens do histórico de chat.</p>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-50 border-b border-gray-100">
                  <th className="px-4 py-2.5 text-left text-xs font-semibold text-ink-muted uppercase tracking-wide">Tipo</th>
                  <th className="px-4 py-2.5 text-left text-xs font-semibold text-ink-muted uppercase tracking-wide">Contato</th>
                  <th className="px-4 py-2.5 text-left text-xs font-semibold text-ink-muted uppercase tracking-wide">Conteúdo</th>
                  <th className="px-4 py-2.5 text-left text-xs font-semibold text-ink-muted uppercase tracking-wide whitespace-nowrap">Data/Hora</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {loadingMessages && (
                  <tr><td colSpan={4} className="px-4 py-8 text-center text-ink-muted text-xs">Carregando...</td></tr>
                )}
                {messages?.map(m => (
                  <tr key={m.id} className="hover:bg-gray-50 transition-colors">
                    <td className="px-4 py-2.5">{typeBadge(m.tipo)}</td>
                    <td className="px-4 py-2.5 text-xs max-w-[130px] truncate" title={m.sessionId}>
                      {m.nomeFuncionario
                        ? <span className="font-medium text-ink">{m.nomeFuncionario}</span>
                        : <span className="font-mono text-ink-muted">{m.sessionId}</span>}
                    </td>
                    <td className="px-4 py-2.5 text-xs text-ink max-w-xs truncate" title={m.conteudo}>
                      {m.conteudo || <span className="text-ink-muted italic">sem conteúdo</span>}
                    </td>
                    <td className="px-4 py-2.5 text-xs text-ink-muted whitespace-nowrap">{fmt(m.criadoEm)}</td>
                  </tr>
                ))}
                {!loadingMessages && !messages?.length && (
                  <tr><td colSpan={4} className="px-4 py-8 text-center text-ink-muted text-xs">Nenhuma mensagem encontrada.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
}
