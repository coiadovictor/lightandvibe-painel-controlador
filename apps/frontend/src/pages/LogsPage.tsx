import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { PageHeader } from '@/components/ui/PageHeader';
import { DataTable } from '@/components/ui/DataTable';
import type { LogEntry } from '@/types/api';

export function LogsPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['logs'],
    queryFn: async () => {
      const res = await api.get<LogEntry[]>('/logs');
      return res.data;
    },
  });

  return (
    <div>
      <PageHeader
        title="Logs"
        description="Estrutura inicial — filtros e detalhamento serão definidos em iteração futura."
      />
      {isError ? (
        <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
          Não foi possível carregar os logs.
        </div>
      ) : (
        <DataTable
          loading={isLoading}
          rows={data ?? []}
          rowKey={(r) => r.id}
          empty="Sem logs por enquanto."
          columns={[
            {
              key: 'created',
              header: 'Quando',
              render: (r) => new Date(r.createdAt).toLocaleString('pt-BR'),
            },
            { key: 'level', header: 'Nível', render: (r) => r.level },
            { key: 'source', header: 'Origem', render: (r) => r.source ?? '—' },
            { key: 'msg', header: 'Mensagem', render: (r) => r.message },
          ]}
        />
      )}
    </div>
  );
}
