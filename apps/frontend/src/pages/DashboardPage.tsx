import { useState, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Users, KeyRound, ScrollText, Newspaper } from 'lucide-react';
import { api } from '@/lib/api';
import { PageHeader } from '@/components/ui/PageHeader';
import { StatCard } from '@/components/ui/StatCard';
import type { DashboardSummary } from '@/types/api';

interface LogStats {
  totalMensagens: number;
}

const HR_NEWS = [
  {
    title: 'Férias',
    body: 'O pedido de férias deve ser solicitado com 30 dias de antecedência pelo portal de RH.',
  },
  {
    title: 'Benefícios',
    body: 'O prazo para atualização de dependentes no plano de saúde vai até o último dia útil do mês.',
  },
  {
    title: 'Ponto eletrônico',
    body: 'Atrasos devem ser justificados em até 48 horas pelo sistema de ponto.',
  },
  {
    title: 'Treinamentos',
    body: 'Confira a agenda de treinamentos corporativos no portal de RH — novas turmas disponíveis.',
  },
  {
    title: 'Vale-refeição',
    body: 'O crédito do VR é realizado no 1.º dia útil do mês. Em caso de divergência, acione o RH.',
  },
  {
    title: 'Clima organizacional',
    body: 'A pesquisa de clima está aberta. Sua participação é anônima e muito importante!',
  },
  {
    title: 'Saúde & bem-estar',
    body: 'Faça pausas regulares durante o expediente. Pequenos intervalos aumentam a produtividade.',
  },
  {
    title: 'Curiosidade RH',
    body: 'Empresas com programas de reconhecimento têm até 31 % menos rotatividade (Gallup, 2024).',
  },
];

export function DashboardPage() {
  const [newsIndex, setNewsIndex] = useState(0);

  useEffect(() => {
    const timer = setInterval(() => setNewsIndex(i => (i + 1) % HR_NEWS.length), 6000);
    return () => clearInterval(timer);
  }, []);

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: async () => {
      const res = await api.get<DashboardSummary>('/dashboard/summary');
      return res.data;
    },
  });

  const { data: logStats } = useQuery<LogStats>({
    queryKey: ['logs-stats-dashboard'],
    queryFn: () => api.get<LogStats>('/logs/stats').then(r => r.data),
    staleTime: 60_000,
    refetchInterval: 60_000,
  });

  const news = HR_NEWS[newsIndex];

  return (
    <div>
      <PageHeader
        title="Dashboard"
        description="Visão geral dos funcionários, acessos e conversas."
      />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <StatCard
          label="Funcionários que já utilizaram"
          value={data?.totalEmployeesLogged ?? 0}
          icon={Users}
          loading={isLoading}
          tone="brand"
        />
        <StatCard
          label="Acessos totais"
          value={data?.totalAccesses ?? 0}
          icon={KeyRound}
          loading={isLoading}
          tone="green"
        />
        <StatCard
          label="Mensagens trocadas"
          value={logStats?.totalMensagens != null
            ? logStats.totalMensagens.toLocaleString('pt-BR')
            : '—'}
          icon={ScrollText}
          tone="blue"
        />
      </div>

      <section className="mt-8 grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Lista compacta de funcionários que acessaram */}
        <div className="lg:col-span-2 bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="px-5 py-3.5 border-b border-gray-100">
            <h2 className="text-sm font-semibold text-ink">Funcionários que acessaram</h2>
          </div>
          <div className="divide-y divide-gray-50 max-h-72 overflow-y-auto">
            {isLoading && (
              <p className="px-5 py-4 text-xs text-ink-muted">Carregando...</p>
            )}
            {data?.accessesByEmployee.map(r => (
              <div key={r.employeeId} className="px-5 py-2 text-sm text-ink">
                {r.employeeName}
              </div>
            ))}
            {!isLoading && !data?.accessesByEmployee.length && (
              <p className="px-5 py-4 text-xs text-ink-muted">Sem acessos registrados ainda.</p>
            )}
          </div>
        </div>

        {/* Card de notícias RH & Curiosidades */}
        <div className="bg-white rounded-2xl border border-gray-200 shadow-sm p-5 flex flex-col">
          <div className="flex items-center gap-2 mb-4">
            <Newspaper className="h-4 w-4 text-brand-500" />
            <h2 className="text-sm font-semibold text-ink">Notícias RH & Curiosidades</h2>
          </div>
          <div className="flex-1 flex flex-col justify-center min-h-[100px]">
            <p className="text-[11px] font-bold text-brand-600 uppercase tracking-wider mb-2">
              {news.title}
            </p>
            <p className="text-sm text-ink leading-relaxed">{news.body}</p>
          </div>
          <div className="mt-5 flex items-center gap-1.5 justify-center">
            {HR_NEWS.map((_, i) => (
              <button
                key={i}
                onClick={() => setNewsIndex(i)}
                className={`w-1.5 h-1.5 rounded-full transition-colors ${
                  i === newsIndex ? 'bg-brand-500' : 'bg-gray-300 hover:bg-gray-400'
                }`}
              />
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}
