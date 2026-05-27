import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { PageHeader } from '@/components/ui/PageHeader';
import { DataTable } from '@/components/ui/DataTable';
import type { Employee } from '@/types/api';

function formatPhone(phone: string | null | undefined) {
  if (!phone) return '—';
  // Remove sufixo do WhatsApp (@s.whatsapp.net)
  return phone.replace(/@.*$/, '').replace(/^55/, '+55 ');
}

export function EmployeesPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['employees'],
    queryFn: async () => {
      const res = await api.get<Employee[]>('/employees');
      return res.data;
    },
  });

  return (
    <div>
      <PageHeader
        title="Funcionários"
        description="Funcionários que já acessaram o sistema via WhatsApp."
      />
      {isError ? (
        <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
          Não foi possível carregar os funcionários.
        </div>
      ) : (
        <DataTable
          loading={isLoading}
          rows={data ?? []}
          rowKey={(r) => r.id}
          empty="Nenhum acesso registrado ainda."
          columns={[
            { key: 'name', header: 'Nome', render: (r) => r.name },
            { key: 'dept', header: 'Departamento', render: (r) => r.department ?? '—' },
            { key: 'email', header: 'E-mail', render: (r) => r.email ?? '—' },
            { key: 'phone', header: 'Celular', render: (r) => formatPhone(r.phone) },
            {
              key: 'last',
              header: 'Último acesso',
              render: (r) =>
                r.lastContactAt ? new Date(r.lastContactAt).toLocaleString('pt-BR') : '—',
            },
          ]}
        />
      )}
    </div>
  );
}
