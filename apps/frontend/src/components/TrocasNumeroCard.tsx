import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { PhoneForwarded } from 'lucide-react';
import { api } from '@/lib/api';
import type { TrocaNumero } from '@/types/api';

function formatarCelular(jid: string) {
  const d = jid.replace(/\D/g, '').replace(/^55/, '');
  if (d.length === 11) return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
  if (d.length === 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return d;
}

export function TrocasNumeroCard() {
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({
    queryKey: ['trocas-numero'],
    queryFn: () => api.get<TrocaNumero[]>('/trocas-numero').then(r => r.data),
    refetchInterval: 60_000,
  });
  const decidir = useMutation({
    mutationFn: ({ id, acao }: { id: number; acao: 'autorizar' | 'recusar' }) => api.post(`/trocas-numero/${id}/${acao}`),
    onSettled: () => qc.invalidateQueries({ queryKey: ['trocas-numero'] }),
  });
  const itens = data ?? [];
  return (
    <div className="bg-white rounded-2xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="px-5 py-3.5 border-b border-gray-100 flex items-center gap-2">
        <PhoneForwarded className="h-4 w-4 text-brand-500" />
        <h2 className="text-sm font-semibold text-ink">Trocas de celular pendentes{itens.length ? ` (${itens.length})` : ''}</h2>
      </div>
      <div className="divide-y divide-gray-50">
        {isLoading && <p className="px-5 py-4 text-xs text-ink-muted">Carregando...</p>}
        {!isLoading && itens.length === 0 && <p className="px-5 py-4 text-xs text-ink-muted">Nenhuma solicitação pendente.</p>}
        {itens.map(t => (
          <div key={t.id} className="px-5 py-3 flex flex-wrap items-center justify-between gap-2">
            <div>
              <p className="text-sm font-medium text-ink">{t.nome ?? 'Funcionário não encontrado'} <span className="text-ink-muted">({t.matricula})</span></p>
              <p className="text-xs text-ink-muted">{formatarCelular(t.celularAntigo)} → {formatarCelular(t.celularNovo)} · {new Date(t.criadoEm).toLocaleString('pt-BR')}</p>
            </div>
            <div className="flex gap-2">
              <button
                className="rounded-lg bg-emerald-600 px-3 py-1 text-xs font-semibold text-white disabled:opacity-50"
                disabled={decidir.isPending}
                onClick={() => window.confirm(`Autorizar a troca de celular de ${t.nome ?? t.matricula}?`) && decidir.mutate({ id: t.id, acao: 'autorizar' })}
              >
                Autorizar
              </button>
              <button
                className="rounded-lg bg-rose-600 px-3 py-1 text-xs font-semibold text-white disabled:opacity-50"
                disabled={decidir.isPending}
                onClick={() => window.confirm(`Recusar a troca de celular de ${t.nome ?? t.matricula}?`) && decidir.mutate({ id: t.id, acao: 'recusar' })}
              >
                Recusar
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
