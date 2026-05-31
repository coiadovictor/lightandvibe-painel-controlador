import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { MessageSquare, Users, Clock, Zap, Bot, User, X, ChevronDown, ChevronRight } from 'lucide-react';
import { api } from '../lib/api';

interface SessaoResumo {
  sessionId: string;
  totalMensagens: number;
  ultimaMensagem: string | null;
  telefone: string | null;
  nomeFuncionario: string | null;
  setor: string | null;
}

interface LogStats {
  totalMensagens: number;
  sessoesUnicas: number;
  mensagensHoje: number;
  mensagensUltimaHora: number;
  mensagensHumanas: number;
  mensagensIA: number;
  mediaMensagensPorSessao: number;
  sessoesPorSetor: { setor: string; sessoes: SessaoResumo[] }[];
}

interface LogMessage {
  id: string;
  sessionId: string;
  tipo: string;
  conteudo: string;
  criadoEm: string | null;
  nomeFuncionario: string | null;
}

interface SelectedSession {
  sessionId: string;
  label: string; // nome ou telefone
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

function getSectorEmoji(setor: string): string {
  const s = setor.toUpperCase().trim();

  if (s.startsWith('VEND/') || s.startsWith('VEND /') || s.startsWith('VEND.')) return '🛒';
  if (s.startsWith('V./') || s.startsWith('V/') || s.startsWith('V.DIVISAO/')) return '🛒';
  if (s.startsWith('DIR/VEND') || s.startsWith('VEND./')) return '🛒';

  const map: Record<string, string> = {
    'ADMINIST-SERVICOS': '🏢',
    'ADMINIST.SERVICOS': '🏢',
    'AMBULATORIO MÉDICO': '🩺',
    'AMBULATORIO MEDICO': '🩺',
    'CONTAS A RECEBER': '💰',
    'FINANCEIRO': '💰',
    'COM./FATURAMENTO': '💰',
    'HONORARIOS': '💰',
    'MATRIZ DIV./CAIXA': '💰',
    'RECURSOS HUMANOS': '👥',
    'SEGURANCA TRABALHO': '🦺',
    'TECNOLOGIA DA INFORMAÇÃO': '💻',
    'TECNOLOGIA DA INFORMACAO': '💻',
    'SUPORTE TECNICO': '🖥️',
    'SEM SETOR': '⚠️',
    'MANUTENCAO GERAL': '🔧',
    'MANUTENCAO ELETRICA': '⚡',
    'MANUTENCAO MAQUINAS': '🔧',
    'MANUT./MAQUINAS': '🔧',
    'OFICINA/MANUTENCAO': '🔧',
    'ADMINISTR. E MANUT.': '🔧',
    'MARKETING': '📢',
    'RELACOES PUBLICAS': '📣',
    'CONTABILIDADE': '📊',
    'CONTROLADORIA': '📊',
    'CPM - CONTR.PROD.MAT.': '📊',
    'MANUT./CONSTRUCOES': '🏗️',
    'MANUTENCAO CONSTRUC.': '🏗️',
    'CAM.LAGES/CONSTRUCAO': '🏗️',
    'PROJETOS': '🏗️',
    'PROJETOS ESPECIAIS': '🏗️',
    'ADMINISTRAÇÃO LOGISTICA': '🚚',
    'ADMINISTRACAO LOGISTICA': '🚚',
    'DIVISAO TRANSPORTES': '🚚',
    'DIVISAO/TRANSPORTE': '🚚',
    'V.DIVISAO/TRANSPORTE': '🚚',
    'CONTROLE LOGISTICO DE PEDIDOS': '🚚',
    'CONTROLE LOGIST PEDIDOS': '🚚',
    'CENTRO DE DISTRIB.40': '🚚',
    'CONSELHO DE ADMINIST': '👔',
    'DIRETORIA COMERCIAL': '👔',
    'DIRETORIA': '👔',
    'GERENTES/COORDENAD.': '👔',
    'TAFFMAN E': '👔',
    'TAFFMAN-E': '👔',
    'ADM.GERAL/STA ISABEL': '🏢',
    'ADMINISTRACAO GERAL': '🏢',
    'ADMINISTRAÇÃO GERAL': '🏢',
    'ADMINISTRACAO': '🏢',
    'ADMINISTRAÇÃO CA': '🏢',
    'ADMINISTRAÇÃO AG/COM': '🏢',
    'CENT/INS/ESCRITORIO': '🏢',
    'ESCRIT.ADM.ORIENTAL': '🏢',
    'ADMINIST./FRUTICASA': '🏢',
    'ADM.GERAL/FRUTICASA': '🏢',
    'ADM.GERAL-FABRICA': '🏢',
    'ADM.GERAL - LORENA': '🏢',
    'C.I.A.': '🏢',
    'CENTRO GERAL-LAGOINHA': '🏢',
    'REFEITORIO': '🍽️',
    'REFEITORIO/FRUTICASA': '🍽️',
    'AGROPECUARIA': '🌱',
    'ADM.GERAL FAZENDA': '🌱',
    'AGROPEC.FUNRURAL': '🌱',
    'MANUT.REFRIGERACAO': '❄️',
    'CAMARA-LAGES': '❄️',
    'LABORATORIO ANALISE': '🧪',
    'LABORAT./FRUTICASA': '🧪',
    'LAB.PESQ/DESENVOLV.': '🧪',
    'LAB.INS.ART.FUNRURAL': '🧪',
    'GARANTIA DE QUALIDADE': '🧪',
    'CONTROLE QUALIDADE': '🧪',
    'CIENCIAS E PESQUISA': '🧪',
    'CIDV/MICRO': '🔬',
    'INJETORA': '🏭',
    'PRODUCAO/ESSENCIAS': '🏭',
    'PRODUCAO-SUCOS': '🏭',
    'PRODUCAO/SUCOS': '🏭',
    'PRODUCAO DE SUCO': '🏭',
    'FABRICA/LORENA': '🏭',
    'ENVASE-SOFYL': '🏭',
    'ENVASE SOFYL': '🏭',
    'ADIDOS FABRICA': '🏭',
    'ADIDOS/LORENA': '🏭',
    'PRODUCAO FABR 3': '🏭',
    'ANDES DO SUL': '🌎',
    'IMPORT./EXPORTACAO': '🌎',
    'DIVISAO/FARMACEUTICA': '💊',
    'DIVISÃO FARMACEUTICA': '💊',
    'SUPLEMENTO': '💊',
    'CADASTRO ASSIST.R.A.': '📋',
    'LYNX': '💻',
    'SIST. GESTÃO INTEGRADA': '💻',
    'SIST.GESTÃO INTEGRADA': '💻',
    'SIST. GESTAO INTEGRADA': '💻',
    'SIST.GESTAO INTEGRADA': '💻',
    'JABOTICABAL': '📍',
    'CACY': '📍',
    'ARARAQUARA': '📍',
    'SAO CARLOS-C.A': '📍',
    'CEARA-CA': '📍',
    'AUTÔNOMOS': '👷',
    'AUTONOMOS': '👷',
    'ULTRA SONOGRAFIA': '🩺',
    'TREINAMENTO E DIVULGAÇÃO': '🎓',
    'TREINAMENTO E DIVULGACAO': '🎓',
    'FERMENTACAO': '🧫',
    'PRODUCAO VAPOR': '♨️',
    'SEDE-COLONIA DE FERIAS': '🏖️',
    'PROCESSO-SOFYL': '⚙️',
    'PROCESSO SOFYL': '⚙️',
    'ADM.VEICULOS': '🚗',
    'LEGALIZACAO/VEICULOS': '🚗',
    'PRESIDENCIA': '👑',
    'PRESIDENCIA DE HONRA': '👑',
    'AUDITORIA': '🔎',
    'EVERESTE': '🏔️',
    'PLAN.PESQ.ESPECIAIS': '📈',
    'PLANEJAMENTO': '📈',
    'DIVISAO/COSMETICOS': '💄',
    'COSMETICOS-TECNICO': '💄',
    'DEPTO JURIDICO': '⚖️',
    'DEPTO FISCAL': '⚖️',
    'SEMEN PARANA': '🐄',
    'SEMEN B.H.': '🐄',
    'WAGYU': '🐂',
    'VILA MARIANA/ESTOQUE': '📦',
    'ARMAZENAG./FRUTICASA': '📦',
    'SUPRIMENTOS': '📦',
    'ADM/COMPRAS/ALMOXARIFADO': '📦',
    'ENGARRAFAMENTO I': '🍾',
    'ENGARRAFAMENTO II': '🍾',
    'ENGARRAFAMENTO': '🍾',
    'SISTEMA TRATAM.AGUA': '💧',
    'LATICINIOS': '🥛',
    'ADIDOS': '🤝',
    'ASSIST ADMINIST CREDITO': '💳',
    'COMPRAS': '🛒',
    'CAMARA/COMPRAS': '🛒',
  };

  if (map[s]) return map[s];

  if (s.includes('MANUTENCAO') || s.includes('MANUT.') || s.includes('OFICINA')) return '🔧';
  if (s.includes('PRODUCAO') || s.includes('FABRICA') || s.includes('ENVASE') || s.includes('ADIDOS')) return '🏭';
  if (s.includes('REFEITORIO')) return '🍽️';
  if (s.includes('LABORAT') || s.includes('LAB.') || s.includes('QUALIDADE') || s.includes('PESQUISA')) return '🧪';
  if (s.includes('TRANSPORT') || s.includes('LOGIST') || s.includes('DISTRIB')) return '🚚';
  if (s.includes('FINANC') || s.includes('FATURAMENTO') || s.includes('CAIXA')) return '💰';
  if (s.includes('CONTAB') || s.includes('CONTROLA') || s.includes('CPM')) return '📊';
  if (s.includes('INFORM') || s.includes('SIST.') || s.includes('SISTEMA') || s.includes('TECNOL')) return '💻';
  if (s.includes('DIRETORIA') || s.includes('GERENTE') || s.includes('COORDENAD')) return '👔';
  if (s.includes('AGROPEC') || s.includes('FAZENDA') || s.includes('FUNRURAL')) return '🌱';
  if (s.includes('ADM') || s.includes('ADMINIST')) return '🏢';

  return '📍';
}

function fmt(dt: string | null) {
  if (!dt) return '—';
  return new Date(dt).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', year: '2-digit', hour: '2-digit', minute: '2-digit' });
}

export function LogsPage() {
  const [selected, setSelected] = useState<SelectedSession | null>(null);
  const [openSectors, setOpenSectors] = useState<Set<string>>(new Set());

  function toggleSetor(setor: string) {
    setOpenSectors(prev => {
      const next = new Set(prev);
      next.has(setor) ? next.delete(setor) : next.add(setor);
      return next;
    });
  }

  const { data: stats, isLoading: loadingStats } = useQuery<LogStats>({
    queryKey: ['logs-stats'],
    queryFn: () => api.get<LogStats>('/logs/stats').then(r => r.data),
    refetchInterval: 60_000,
  });

  const { data: messages, isLoading: loadingMessages } = useQuery<LogMessage[]>({
    queryKey: ['logs-messages', selected?.sessionId],
    queryFn: () => {
      const params = new URLSearchParams({ limit: '200' });
      if (selected?.sessionId) params.set('sessionId', selected.sessionId);
      return api.get<LogMessage[]>(`/logs/messages?${params}`).then(r => r.data);
    },
    enabled: !!selected,
    refetchInterval: selected ? 30_000 : false,
  });

  function handleSelectSession(s: SessaoResumo) {
    const label = s.nomeFuncionario ?? s.telefone ?? s.sessionId;
    if (selected?.sessionId === s.sessionId) {
      setSelected(null);
    } else {
      setSelected({ sessionId: s.sessionId, label });
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-ink">Logs de Conversas</h1>
        <p className="text-sm text-ink-muted mt-1">Métricas e histórico do banco n8n.</p>
      </div>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-4">
        <StatCard label="Total Mensagens"   value={loadingStats ? '—' : (stats?.totalMensagens ?? 0).toLocaleString('pt-BR')}      icon={MessageSquare} color="brand"   />
        <StatCard label="Funcionários que já utilizaram" value={loadingStats ? '—' : (stats?.sessoesUnicas ?? 0).toLocaleString('pt-BR')} icon={Users} color="blue" />
        <StatCard label="Mensagens Hoje"    value={loadingStats ? '—' : (stats?.mensagensHoje ?? 0).toLocaleString('pt-BR')}       icon={Clock}         color="green"   />
        <StatCard label="Última 1h"         value={loadingStats ? '—' : (stats?.mensagensUltimaHora ?? 0).toLocaleString('pt-BR')} icon={Zap}           color="yellow"  />
        <StatCard label="Msgs Usuário"      value={loadingStats ? '—' : (stats?.mensagensHumanas ?? 0).toLocaleString('pt-BR')}    icon={User}          color="neutral" />
        <StatCard label="Respostas IA"      value={loadingStats ? '—' : (stats?.mensagensIA ?? 0).toLocaleString('pt-BR')}         icon={Bot}           color="neutral" />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Sessões por Setor */}
        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 overflow-y-auto max-h-[600px]">
          <h2 className="text-sm font-semibold text-ink mb-1">Sessões por Setor</h2>
          <p className="text-xs text-ink-muted mb-4">
            Média: <span className="font-medium text-ink">{stats?.mediaMensagensPorSessao ?? 0} msg/sessão</span>
          </p>
          <div className="space-y-2">
            {loadingStats && <p className="text-xs text-ink-muted">Carregando...</p>}
            {stats?.sessoesPorSetor.map(grupo => {
              const isOpen = openSectors.has(grupo.setor);
              return (
                <div key={grupo.setor}>
                  <button
                    onClick={() => toggleSetor(grupo.setor)}
                    className="w-full flex items-center justify-between py-1.5 px-1 rounded-lg hover:bg-gray-50 transition-colors text-left"
                  >
                    <span className="text-[10px] font-bold uppercase tracking-wider text-ink-muted">
                      {getSectorEmoji(grupo.setor)} {grupo.setor}
                    </span>
                    <div className="flex items-center gap-1.5">
                      <span className="text-[10px] text-ink-muted">{grupo.sessoes.length} sessão(ões)</span>
                      {isOpen
                        ? <ChevronDown className="h-3 w-3 text-ink-muted" />
                        : <ChevronRight className="h-3 w-3 text-ink-muted" />}
                    </div>
                  </button>
                  {isOpen && (
                    <div className="space-y-0.5 mt-0.5">
                      {grupo.sessoes.map(s => {
                        const label = s.nomeFuncionario ?? s.telefone ?? s.sessionId;
                        const isActive = selected?.sessionId === s.sessionId;
                        return (
                          <button
                            key={s.sessionId}
                            onClick={() => handleSelectSession(s)}
                            className={`w-full flex items-center justify-between py-1.5 px-2 rounded-lg border transition-colors text-left ${
                              isActive
                                ? 'bg-brand-50 border-brand-200'
                                : 'border-transparent hover:bg-gray-50'
                            }`}
                          >
                            <div className="min-w-0">
                              <p className="text-xs font-medium text-ink truncate max-w-[150px]">{label}</p>
                              <p className="text-[10px] text-ink-muted">{fmt(s.ultimaMensagem)}</p>
                            </div>
                            <span className={`ml-2 shrink-0 text-xs font-semibold px-2 py-0.5 rounded-full ${
                              isActive ? 'text-brand-700 bg-brand-100' : 'text-brand-600 bg-brand-50'
                            }`}>
                              {s.totalMensagens}
                            </span>
                          </button>
                        );
                      })}
                    </div>
                  )}
                </div>
              );
            })}
            {!loadingStats && !stats?.sessoesPorSetor.length && (
              <p className="text-xs text-ink-muted">Nenhuma sessão encontrada.</p>
            )}
          </div>
          {stats && stats.sessoesPorSetor.length > 0 && (
            <div className="mt-4 pt-3 border-t border-gray-100 flex items-center justify-between">
              <span className="text-xs text-ink-muted">Total de mensagens</span>
              <span className="text-xs font-bold text-ink">
                {stats.sessoesPorSetor
                  .flatMap(g => g.sessoes)
                  .reduce((acc, s) => acc + s.totalMensagens, 0)
                  .toLocaleString('pt-BR')}
              </span>
            </div>
          )}
        </div>

        {/* Painel de mensagens — só aparece quando uma sessão está selecionada */}
        {selected ? (
          <div className="lg:col-span-2 bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
            <div className="px-5 py-4 border-b border-gray-100 flex items-center justify-between">
              <div>
                <h2 className="text-sm font-semibold text-ink">{selected.label}</h2>
                <p className="text-xs text-ink-muted">Histórico completo desta conversa</p>
              </div>
              <button
                onClick={() => setSelected(null)}
                className="p-1.5 rounded-lg hover:bg-gray-100 transition-colors text-ink-muted"
                title="Fechar"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-50 border-b border-gray-100">
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-ink-muted uppercase tracking-wide">Tipo</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-ink-muted uppercase tracking-wide">Conteúdo</th>
                    <th className="px-4 py-2.5 text-left text-xs font-semibold text-ink-muted uppercase tracking-wide whitespace-nowrap">Data/Hora</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-50">
                  {loadingMessages && (
                    <tr><td colSpan={3} className="px-4 py-8 text-center text-ink-muted text-xs">Carregando...</td></tr>
                  )}
                  {messages?.map(m => (
                    <tr key={m.id} className="hover:bg-gray-50 transition-colors">
                      <td className="px-4 py-2.5">{typeBadge(m.tipo)}</td>
                      <td className="px-4 py-2.5 text-xs text-ink max-w-xs truncate" title={m.conteudo}>
                        {m.conteudo || <span className="text-ink-muted italic">sem conteúdo</span>}
                      </td>
                      <td className="px-4 py-2.5 text-xs text-ink-muted whitespace-nowrap">{fmt(m.criadoEm)}</td>
                    </tr>
                  ))}
                  {!loadingMessages && !messages?.length && (
                    <tr><td colSpan={3} className="px-4 py-8 text-center text-ink-muted text-xs">Nenhuma mensagem encontrada.</td></tr>
                  )}
                </tbody>
              </table>
            </div>
          </div>
        ) : (
          <div className="lg:col-span-2 bg-gray-50 rounded-2xl border border-dashed border-gray-200 flex items-center justify-center min-h-[200px]">
            <p className="text-sm text-ink-muted">Selecione uma sessão para ver as mensagens</p>
          </div>
        )}
      </div>
    </div>
  );
}
