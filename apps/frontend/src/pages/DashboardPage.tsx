import { useQuery } from '@tanstack/react-query';
import { Users, KeyRound, MessageSquare, ScrollText } from 'lucide-react';
import { api } from '@/lib/api';
import { PageHeader } from '@/components/ui/PageHeader';
import { StatCard } from '@/components/ui/StatCard';
import { DataTable } from '@/components/ui/DataTable';
import type { DashboardSummary } from '@/types/api';

export function DashboardPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: async () => {
      const res = await api.get<DashboardSummary>('/dashboard/summary');
      return res.data;
    },
  });

  return (
    <div>
      <PageHeader
        title="Dashboard"
        description="Visão geral dos funcionários, acessos e conversas."
      />

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          label="Funcionários logados"
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
          label="Conversas"
          value={data?.totalConversations ?? 0}
          icon={MessageSquare}
          loading={isLoading}
          tone="blue"
        />
        <StatCard
          label="Logs"
          value="—"
          icon={ScrollText}
          hint="Disponível em breve"
          tone="neutral"
        />
      </div>

      <section className="mt-8">
        <h2 className="mb-3 text-lg font-semibold text-slate-900">Acessos por funcionário</h2>
        {isError ? (
          <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
            Não foi possível carregar os dados. Verifique se o backend está rodando.
          </div>
        ) : (
          <DataTable
            loading={isLoading}
            rows={data?.accessesByEmployee ?? []}
            rowKey={(r) => r.employeeId}
            empty="Sem acessos registrados ainda."
            columns={[
              { key: 'name', header: 'Funcionário', render: (r) => r.employeeName },
              { key: 'count', header: 'Acessos', render: (r) => r.accessCount },
            ]}
          />
        )}
      </section>
    </div>
  );
}
