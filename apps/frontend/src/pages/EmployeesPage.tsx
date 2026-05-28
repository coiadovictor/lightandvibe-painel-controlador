import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { FileText } from 'lucide-react';
import { api } from '@/lib/api';
import { PageHeader } from '@/components/ui/PageHeader';
import { DataTable } from '@/components/ui/DataTable';
import { FichaModal } from '@/components/FichaModal';
import type { Employee, FichaRegistro } from '@/types/api';

function formatPhone(phone: string | null | undefined) {
  if (!phone) return '—';
  return phone.replace(/@.*$/, '').replace(/^55/, '+55 ');
}

export function EmployeesPage() {
  const [fichaMatricula, setFichaMatricula] = useState<string | null>(null);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['employees'],
    queryFn: async () => {
      const res = await api.get<Employee[]>('/employees');
      return res.data;
    },
  });

  const { data: fichaData, isFetching: loadingFicha } = useQuery<FichaRegistro>({
    queryKey: ['ficha', fichaMatricula],
    queryFn: async () => {
      const res = await api.get<FichaRegistro>(`/employees/${fichaMatricula}/ficha`);
      return res.data;
    },
    enabled: !!fichaMatricula,
    staleTime: 5 * 60_000,
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
            {
              key: 'ficha',
              header: '',
              render: (r) => (
                <button
                  onClick={() => setFichaMatricula(r.id)}
                  className="flex items-center gap-1.5 text-xs px-2.5 py-1 rounded-lg border border-brand-200 text-brand-700 hover:bg-brand-50 transition-colors whitespace-nowrap"
                  title="Ver Ficha de Registro"
                >
                  <FileText className="h-3.5 w-3.5" />
                  Ficha
                </button>
              ),
            },
          ]}
        />
      )}

      {/* Loading overlay */}
      {fichaMatricula && loadingFicha && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="bg-white rounded-xl px-6 py-4 text-sm text-gray-600 shadow-xl">
            Carregando ficha…
          </div>
        </div>
      )}

      {/* Ficha modal */}
      {fichaData && !loadingFicha && (
        <FichaModal ficha={fichaData} onClose={() => setFichaMatricula(null)} />
      )}
    </div>
  );
}
