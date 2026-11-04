import { useState } from 'react';
import type { KeyboardEvent } from 'react';
import { useMutation } from '@tanstack/react-query';
import { AxiosError } from 'axios';
import clsx from 'clsx';
import { api } from '../lib/api';
import { PageHeader } from '../components/ui/PageHeader';

type Ambiente = 'producao' | 'homolog';

interface SqlResultado {
  colunas: string[];
  linhas: unknown[][];
  truncado: boolean;
  duracaoMs: number;
}

const LIMITE = 1000;

function formatarCelula(valor: unknown) {
  if (valor === null || valor === undefined) return <span className="text-slate-400">NULL</span>;
  if (typeof valor === 'object') return JSON.stringify(valor);
  return String(valor);
}

export function SqlPage() {
  const [ambiente, setAmbiente] = useState<Ambiente>('producao');
  const [sql, setSql] = useState('');
  const [resultado, setResultado] = useState<SqlResultado | null>(null);
  const [erro, setErro] = useState<string | null>(null);

  const consulta = useMutation({
    mutationFn: async () => {
      const res = await api.post<SqlResultado>('/sql/consultar', { ambiente, sql }, { timeout: 25000 });
      return res.data;
    },
    onSuccess: (data) => {
      setResultado(data);
      setErro(null);
    },
    onError: (err: AxiosError<{ message?: string }>) => {
      setErro(err.response?.data?.message || err.message);
      setResultado(null);
    },
  });

  const podeExecutar = sql.trim() !== '' && !consulta.isPending;

  function executar() {
    if (podeExecutar) consulta.mutate();
  }

  function trocarAmbiente(novo: Ambiente) {
    setAmbiente(novo);
    setResultado(null);
    setErro(null);
  }

  function aoTeclar(e: KeyboardEvent<HTMLTextAreaElement>) {
    if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
      e.preventDefault();
      executar();
    }
  }

  return (
    <div>
      <PageHeader title="SQL" description="Consultas de leitura no banco do RH. Só SELECT." />

      <div className="mb-4 flex flex-wrap items-center gap-3">
        <div className="inline-flex overflow-hidden rounded-lg border border-slate-200 text-sm">
          <button
            type="button"
            onClick={() => trocarAmbiente('producao')}
            disabled={consulta.isPending}
            className={clsx('px-4 py-2 font-medium', ambiente === 'producao' ? 'border-amber-300 bg-amber-100 text-amber-900' : 'bg-white text-slate-600 hover:bg-slate-50')}
          >
            Produção
          </button>
          <button
            type="button"
            onClick={() => trocarAmbiente('homolog')}
            disabled={consulta.isPending}
            className={clsx('px-4 py-2 font-medium', ambiente === 'homolog' ? 'bg-slate-900 text-white' : 'bg-white text-slate-600 hover:bg-slate-50')}
          >
            Homolog
          </button>
        </div>
        <button
          type="button"
          onClick={executar}
          disabled={!podeExecutar}
          className="ml-auto rounded-lg bg-indigo-600 px-4 py-2 text-sm font-medium text-white hover:bg-indigo-700 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {consulta.isPending ? 'Executando…' : 'Executar'}
        </button>
      </div>

      <textarea
        value={sql}
        onChange={(e) => setSql(e.target.value)}
        onKeyDown={aoTeclar}
        rows={8}
        spellCheck={false}
        placeholder="select * from pag_funcionario limit 10"
        className="mb-4 w-full resize-y rounded-xl border border-slate-200 bg-white p-3 font-mono text-sm shadow-sm focus:border-indigo-400 focus:outline-none"
      />

      {erro && (
        <pre className="mb-4 whitespace-pre-wrap rounded-xl border border-red-200 bg-red-50 p-3 text-sm text-red-800">{erro}</pre>
      )}

      {resultado && (
        <div>
          <p className="mb-2 text-sm text-slate-500">
            {resultado.linhas.length} linha(s) · {resultado.duracaoMs} ms
            {resultado.truncado && ` · mostrando as primeiras ${LIMITE}`}
          </p>
          {resultado.linhas.length === 0 ? (
            <p className="text-sm text-slate-400">Nenhuma linha.</p>
          ) : (
            <div className="overflow-x-auto rounded-xl border border-slate-200 bg-white shadow-sm">
              <table className="min-w-full divide-y divide-slate-200 text-sm">
                <thead className="bg-slate-50">
                  <tr>
                    {resultado.colunas.map((c, i) => (
                      <th key={`${c}-${i}`} scope="col" className="whitespace-nowrap px-4 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-500">
                        {c}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {resultado.linhas.map((linha, i) => (
                    <tr key={i} className="hover:bg-slate-50">
                      {resultado.colunas.map((c, j) => (
                        <td key={`${c}-${j}`} className="whitespace-nowrap px-4 py-2 font-mono text-xs text-slate-700">
                          {formatarCelula(linha[j])}
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
