import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { PageHeader } from '@/components/ui/PageHeader';
import { DataTable } from '@/components/ui/DataTable';
import type { InformationType } from '@/types/api';

export function InformationTypesPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['information-types'],
    queryFn: async () => {
      const res = await api.get<InformationType[]>('/information-types');
      return res.data;
    },
  });

  return (
    <div>
      <PageHeader title="Tipo de Informação" description="Categorias de informação disponíveis." />
      {isError ? (
        <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
          Não foi possível carregar os tipos de informação.
        </div>
      ) : (
        <DataTable
          loading={isLoading}
          rows={data ?? []}
          rowKey={(r) => r.id}
          columns={[
            { key: 'name', header: 'Nome', render: (r) => r.name },
            { key: 'desc', header: 'Descrição', render: (r) => r.description ?? '—' },
          ]}
        />
      )}
    </div>
  );
}
