import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { FileText, AlertCircle } from 'lucide-react';
import { api } from '../lib/api';
import { MonthYearModal } from '../components/hollerith/MonthYearModal';
import { generateHollerithPdf } from '../lib/hollerithPdf';
import type { Employee, HollerithData } from '../types/api';

interface SelectedEmployee {
  id: string;
  name: string;
}

export function HollerithPage() {
  const [selected, setSelected] = useState<SelectedEmployee | null>(null);
  const [generating, setGenerating] = useState(false);
  const [genError, setGenError] = useState<string | null>(null);

  const { data: employees, isLoading, isError } = useQuery<Employee[]>({
    queryKey: ['employees'],
    queryFn: () => api.get<Employee[]>('/employees').then(r => r.data),
  });

  async function handleConfirm(mes: number, ano: number) {
    if (!selected) return;
    setGenerating(true);
    setGenError(null);
    try {
      const { data } = await api.get<HollerithData>(
        `/hollerith/${encodeURIComponent(selected.id)}`,
        { params: { mes, ano } },
      );
      await generateHollerithPdf(data);
      setSelected(null);
    } catch (err: unknown) {
      const msg =
        (err as { response?: { data?: { error?: string } } })?.response?.data?.error ??
        'Nenhum movimento encontrado para o período informado.';
      setGenError(msg);
    } finally {
      setGenerating(false);
    }
  }

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-ink">Holerite</h1>
        <p className="text-sm text-ink-muted mt-1">
          Clique em um funcionário para gerar o demonstrativo de pagamento em PDF.
        </p>
      </div>

      {genError && (
        <div className="mb-4 flex items-center gap-2 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
          <AlertCircle className="h-4 w-4 shrink-0" />
          {genError}
          <button className="ml-auto text-red-400 hover:text-red-600" onClick={() => setGenError(null)}>✕</button>
        </div>
      )}

      <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-100 bg-gray-50">
              <th className="px-4 py-3 text-left font-semibold text-ink-muted uppercase text-xs tracking-wide">Matrícula</th>
              <th className="px-4 py-3 text-left font-semibold text-ink-muted uppercase text-xs tracking-wide">Nome</th>
              <th className="px-4 py-3 text-left font-semibold text-ink-muted uppercase text-xs tracking-wide hidden md:table-cell">Departamento</th>
              <th className="px-4 py-3 text-left font-semibold text-ink-muted uppercase text-xs tracking-wide hidden lg:table-cell">E-mail</th>
              <th className="px-4 py-3 text-center font-semibold text-ink-muted uppercase text-xs tracking-wide">Holerite</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-50">
            {isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-ink-muted">
                  Carregando funcionários...
                </td>
              </tr>
            )}
            {isError && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-red-500">
                  Erro ao carregar funcionários.
                </td>
              </tr>
            )}
            {employees?.map(emp => (
              <tr key={emp.id} className="hover:bg-gray-50 transition-colors">
                <td className="px-4 py-3 text-ink-muted font-mono text-xs">{emp.id}</td>
                <td className="px-4 py-3 font-medium text-ink">{emp.name}</td>
                <td className="px-4 py-3 text-ink-muted hidden md:table-cell">{emp.department ?? '—'}</td>
                <td className="px-4 py-3 text-ink-muted hidden lg:table-cell text-xs">{emp.email ?? '—'}</td>
                <td className="px-4 py-3 text-center">
                  <button
                    type="button"
                    onClick={() => { setGenError(null); setSelected({ id: emp.id, name: emp.name }); }}
                    className="inline-flex items-center gap-1.5 rounded-lg bg-brand-50 hover:bg-brand-100 text-brand-700 px-3 py-1.5 text-xs font-medium transition"
                  >
                    <FileText className="h-3.5 w-3.5" />
                    Ver PDF
                  </button>
                </td>
              </tr>
            ))}
            {employees?.length === 0 && !isLoading && (
              <tr>
                <td colSpan={5} className="px-4 py-10 text-center text-ink-muted">
                  Nenhum funcionário encontrado.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {selected && (
        <MonthYearModal
          employeeName={selected.name}
          onConfirm={handleConfirm}
          onClose={() => { setSelected(null); setGenError(null); }}
          loading={generating}
        />
      )}
    </div>
  );
}
