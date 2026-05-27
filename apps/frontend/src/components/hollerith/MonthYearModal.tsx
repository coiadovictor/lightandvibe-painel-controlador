import { useState, type FormEvent } from 'react';
import { X } from 'lucide-react';

const MESES = [
  { value: 1, label: 'Janeiro' }, { value: 2, label: 'Fevereiro' },
  { value: 3, label: 'Março' }, { value: 4, label: 'Abril' },
  { value: 5, label: 'Maio' }, { value: 6, label: 'Junho' },
  { value: 7, label: 'Julho' }, { value: 8, label: 'Agosto' },
  { value: 9, label: 'Setembro' }, { value: 10, label: 'Outubro' },
  { value: 11, label: 'Novembro' }, { value: 12, label: 'Dezembro' },
];

const MIN_YEAR = 2020;
const now = new Date();
const MAX_YEAR = now.getFullYear();
const MAX_MONTH = now.getMonth() + 1;

function availableMonths(year: number) {
  if (year < MAX_YEAR) return MESES;
  return MESES.filter(m => m.value <= MAX_MONTH);
}

function availableYears() {
  const years = [];
  for (let y = MAX_YEAR; y >= MIN_YEAR; y--) years.push(y);
  return years;
}

interface Props {
  employeeName: string;
  onConfirm: (mes: number, ano: number) => void;
  onClose: () => void;
  loading?: boolean;
}

export function MonthYearModal({ employeeName, onConfirm, onClose, loading }: Props) {
  const [ano, setAno] = useState(MAX_YEAR);
  const [mes, setMes] = useState(() => {
    const months = availableMonths(MAX_YEAR);
    return months[months.length - 1].value;
  });

  function handleYearChange(y: number) {
    setAno(y);
    const months = availableMonths(y);
    if (!months.find(m => m.value === mes)) {
      setMes(months[months.length - 1].value);
    }
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    onConfirm(mes, ano);
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center px-4">
      <div className="absolute inset-0 bg-ink/40 backdrop-blur-sm" onClick={onClose} />
      <div className="relative bg-white rounded-2xl shadow-xl w-full max-w-sm p-6">
        <button
          type="button"
          onClick={onClose}
          className="absolute top-4 right-4 text-ink-muted hover:text-ink"
        >
          <X className="h-5 w-5" />
        </button>

        <h2 className="text-base font-semibold text-ink mb-1">Selecionar período</h2>
        <p className="text-sm text-ink-muted mb-5 truncate">
          Holerite de <span className="font-medium text-ink">{employeeName}</span>
        </p>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-sm font-medium text-ink-soft mb-1">Mês</label>
              <select
                value={mes}
                onChange={e => setMes(Number(e.target.value))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-ink focus:outline-none focus:ring-2 focus:ring-brand-500 transition"
              >
                {availableMonths(ano).map(m => (
                  <option key={m.value} value={m.value}>{m.label}</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium text-ink-soft mb-1">Ano</label>
              <select
                value={ano}
                onChange={e => handleYearChange(Number(e.target.value))}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm text-ink focus:outline-none focus:ring-2 focus:ring-brand-500 transition"
              >
                {availableYears().map(y => (
                  <option key={y} value={y}>{y}</option>
                ))}
              </select>
            </div>
          </div>

          <div className="flex gap-2 pt-1">
            <button
              type="button"
              onClick={onClose}
              className="flex-1 rounded-lg border border-gray-300 py-2 text-sm font-medium text-ink-muted hover:bg-gray-50 transition"
            >
              Cancelar
            </button>
            <button
              type="submit"
              disabled={loading}
              className="flex-1 bg-brand-600 hover:bg-brand-500 disabled:opacity-60 text-white font-medium rounded-lg py-2 text-sm transition"
            >
              {loading ? 'Gerando...' : 'Ver Holerite'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
