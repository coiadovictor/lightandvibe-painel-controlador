import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/api';
import { PageHeader } from '@/components/ui/PageHeader';
import { DataTable } from '@/components/ui/DataTable';
import type { Integracao } from '@/types/api';

const SITUACAO_LABELS: Record<string, { label: string; classes: string }> = {
  sucesso:      { label: 'Sucesso',      classes: 'bg-green-50 text-green-700 border-green-200' },
  erro:         { label: 'Erro',         classes: 'bg-rose-50 text-rose-700 border-rose-200' },
  em_andamento: { label: 'Em andamento', classes: 'bg-yellow-50 text-yellow-700 border-yellow-200' },
};

function SituacaoBadge({ situacao }: { situacao: string }) {
  const cfg = SITUACAO_LABELS[situacao] ?? { label: situacao, classes: 'bg-gray-100 text-gray-600 border-gray-200' };
  return (
    <span className={`inline-block rounded-full border px-2.5 py-0.5 text-xs font-medium ${cfg.classes}`}>
      {cfg.label}
    </span>
  );
}

function formatDuration(inicio: string, fim?: string | null) {
  if (!fim) return '—';
  const ms = new Date(fim).getTime() - new Date(inicio).getTime();
  if (ms < 1000) return `${ms}ms`;
  return `${(ms / 1000).toFixed(1)}s`;
}

export function IntegracoesPage() {
  const [situacao, setSituacao] = useState('');
  const [tabela, setTabela] = useState('');

  const { data, isLoading, isError } = useQuery({
    queryKey: ['integracoes', situacao, tabela],
    queryFn: async () => {
      const params = new URLSearchParams({ limit: '200' });
      if (situacao) params.set('situacao', situacao);
      if (tabela)   params.set('tabela', tabela);
      const res = await api.get<Integracao[]>(`/integracoes?${params}`);
      return res.data;
    },
  });

  return (
    <div>
      <PageHeader
        title="Integrações"
        description="Histórico de execuções registradas pelo API Gateway."
      />

      <div className="mb-4 flex flex-wrap gap-3">
        <select
          value={situacao}
          onChange={(e) => setSituacao(e.target.value)}
          className="rounded-lg border border-gray-200 bg-white px-3 py-2 text-sm text-ink focus:outline-none focus:ring-2 focus:ring-brand-300"
        >
          <option value="">Todas as situações</option>
          <option value="sucesso">Sucesso</option>
          <option value="erro">Erro</option>
          <option value="em_andamento">Em andamento</option>
        </select>

        <input
          type="text"
          placeholder="Filtrar por tabela…"
          value={tabela}
          onChange={(e) => setTabela(e.target.value)}
          className="rounded-lg border border-gray-200 bg-white px-3 py-2 text-sm text-ink placeholder:text-ink-muted focus:outline-none focus:ring-2 focus:ring-brand-300"
        />
      </div>

      {isError ? (
        <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 text-sm text-rose-700">
          Não foi possível carregar as integrações.
        </div>
      ) : (
        <DataTable
          loading={isLoading}
          rows={data ?? []}
          rowKey={(r) => r.id}
          empty="Nenhuma integração registrada ainda."
          columns={[
            {
              key: 'situacao',
              header: 'Situação',
              render: (r) => <SituacaoBadge situacao={r.situacao} />,
            },
            {
              key: 'tabela',
              header: 'Tabela',
              render: (r) => <span className="font-mono text-xs">{r.tabela}</span>,
            },
            {
              key: 'acao',
              header: 'Ação',
              render: (r) => r.acao ?? '—',
            },
            {
              key: 'qtd',
              header: 'Registros',
              render: (r) => r.quantidadeRegistros ?? '—',
            },
            {
              key: 'data_inicio',
              header: 'Início',
              render: (r) => new Date(r.dataInicio).toLocaleString('pt-BR'),
            },
            {
              key: 'duracao',
              header: 'Duração',
              render: (r) => formatDuration(r.dataInicio, r.dataFim),
            },
            {
              key: 'origem',
              header: 'Origem',
              render: (r) => <span className="text-xs text-ink-muted">{r.origem ?? '—'}</span>,
            },
            {
              key: 'erro',
              header: 'Erro',
              render: (r) =>
                r.mensagemErro ? (
                  <span
                    className="max-w-xs truncate block text-xs text-rose-600"
                    title={r.mensagemErro}
                  >
                    {r.mensagemErro}
                  </span>
                ) : (
                  '—'
                ),
            },
          ]}
        />
      )}
    </div>
  );
}
