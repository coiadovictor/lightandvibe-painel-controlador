import { X, Printer } from 'lucide-react';
import type { FichaRegistro } from '@/types/api';

interface Props {
  ficha: FichaRegistro;
  onClose: () => void;
}

function Field({ label, value, wide }: { label: string; value?: string | null; wide?: boolean }) {
  return (
    <div className={`flex flex-col ${wide ? 'col-span-2' : ''}`}>
      <span className="text-[9px] font-semibold uppercase text-gray-500 leading-tight">{label}</span>
      <span className="text-[11px] text-gray-900 border-b border-gray-300 min-h-[16px] pb-0.5">
        {value || ''}
      </span>
    </div>
  );
}

function SectionHeader({ title }: { title: string }) {
  return (
    <div className="bg-gray-100 border border-gray-300 text-center py-1 mt-3 mb-1">
      <span className="text-[11px] font-bold uppercase text-gray-700">{title}</span>
    </div>
  );
}

function fmt(n: number | null | undefined) {
  if (n == null) return '';
  return n.toLocaleString('pt-BR', { minimumFractionDigits: 2 });
}

export function FichaModal({ ficha, onClose }: Props) {
  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center bg-black/50 overflow-y-auto py-6 px-4">
      <div className="relative bg-white w-full max-w-3xl rounded shadow-xl print:shadow-none">

        {/* Toolbar (not printed) */}
        <div className="print:hidden flex items-center justify-between px-4 py-2 border-b border-gray-200 bg-gray-50 rounded-t">
          <span className="text-sm font-semibold text-gray-700">Ficha de Registro — {ficha.nome}</span>
          <div className="flex items-center gap-2">
            <button
              onClick={() => window.print()}
              className="flex items-center gap-1.5 text-xs px-3 py-1.5 rounded bg-brand-600 text-white hover:bg-brand-700 transition-colors"
            >
              <Printer className="h-3.5 w-3.5" />
              Imprimir
            </button>
            <button
              onClick={onClose}
              className="p-1.5 rounded hover:bg-gray-200 transition-colors text-gray-500"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
        </div>

        {/* Document body */}
        <div id="ficha-print" className="p-6 text-[11px] font-sans">

          {/* Header */}
          <div className="flex items-start justify-between mb-2">
            <img src="/logo.jpeg" alt="Logo" className="h-14 object-contain" />
            <div className="text-right text-[10px] text-gray-600">
              <p><span className="font-semibold">Matrícula:</span> {ficha.matricula}</p>
            </div>
          </div>

          <div className="text-center text-sm font-bold uppercase border-t border-b border-gray-400 py-1 mb-2">
            Ficha de Registro do Empregado
          </div>

          {/* Identificação do Empregador */}
          <SectionHeader title="Identificação do Empregador" />
          <div className="border border-gray-300 p-2 grid grid-cols-4 gap-x-3 gap-y-2">
            <Field label="Razão Social" value={ficha.empresa} wide />
            <Field label="CNPJ" value={ficha.cnpjEmpresa} />
            <Field label="CNAE" value={ficha.cnaeEmpresa} />
            <Field label="Endereço" value={ficha.enderecoEmpresa} wide />
            <Field label="Bairro" value={ficha.bairroEmpresa} />
            <Field label="Município" value={ficha.municipioEmpresa} />
            <Field label="UF" value={ficha.ufEmpresa} />
          </div>

          {/* Identificação do Empregado */}
          <SectionHeader title="Identificação do Empregado" />
          <div className="border border-gray-300 p-2 grid grid-cols-4 gap-x-3 gap-y-2">
            <Field label="Nome" value={ficha.nome} wide />
            <Field label="Nascido em" value={ficha.nascimento} />
            <Field label="Estado Civil" value={ficha.estadoCivil} />

            <Field label="Nome da Mãe" value={ficha.nomeMae} wide />
            <Field label="Nome do Pai" value={ficha.nomePai} wide />

            <Field label="Nacionalidade" value={ficha.nacionalidade} />
            <Field label="Grau de Instrução" value={ficha.grauInstrucao} wide />

            <Field label="Sexo" value={ficha.sexo} />
            <Field label="Endereço" value={ficha.endereco} wide />
            <Field label="Bairro" value={ficha.bairro} />

            <Field label="Município" value={ficha.municipio} />
            <Field label="UF" value={ficha.uf} />
            <Field label="CEP" value={ficha.cep} />
            <Field label="Número do CPF" value={ficha.cpf} />

            <Field label="CTPS Número" value={ficha.ctps} />
            <Field label="CTPS Série" value={ficha.ctpsSerie} />
            <Field label="Expedida em" value={ficha.ctpsExpedicao} />
            <Field label="UF CTPS" value={ficha.ctpsUf} />

            <Field label="RG Número" value={ficha.rg} />
            <Field label="RG Órgão Exp." value={ficha.rgOrgao} />
            <Field label="Expedida em" value={ficha.rgExpedicao} />
            <Field label="UF RG" value={ficha.rgUf} />

            <Field label="Admissão" value={ficha.admissao} />
            <Field label="Cargo" value={ficha.cargo} wide />
            <Field label="CBO Esocial" value={ficha.cboEsocial} />

            <Field label="PIS/PASEP" value={ficha.pis} />
            <Field label="E-mail" value={ficha.emailContato} wide />
            <Field label="Celular" value={ficha.celular} />

            <Field label="Setor" value={ficha.setor} />
            <Field label="Departamento" value={ficha.departamento} wide />

            <Field label="Qtde Horas Mensais" value={ficha.horasMensais} />
            <Field label="Salário Atual" value={ficha.salarioAtual != null ? `R$ ${fmt(ficha.salarioAtual)}` : ''} />
            <Field label="Forma de Remuneração" value={ficha.formaRemuneracao} />
            <Field label="Sindicato" value={ficha.sindicato} wide />
            {ficha.desligamento && <Field label="Desligamento" value={ficha.desligamento} />}
          </div>

          {/* Alterações de Cargo */}
          {ficha.alteracoesCargo.length > 0 && (
            <>
              <SectionHeader title="Alterações de Cargo" />
              <table className="w-full border-collapse border border-gray-300 text-[10px]">
                <thead className="bg-gray-50">
                  <tr>
                    {['Data', 'Cargo', 'Motivo', 'CBO Esocial'].map(h => (
                      <th key={h} className="border border-gray-300 px-2 py-1 text-left font-semibold">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {ficha.alteracoesCargo.map((r, i) => (
                    <tr key={i} className="even:bg-gray-50">
                      <td className="border border-gray-300 px-2 py-0.5 whitespace-nowrap">{r.data}</td>
                      <td className="border border-gray-300 px-2 py-0.5">{r.cargo}</td>
                      <td className="border border-gray-300 px-2 py-0.5">{r.motivo ?? ''}</td>
                      <td className="border border-gray-300 px-2 py-0.5">{r.cboEsocial ?? ''}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}

          {/* Alterações Salariais */}
          {ficha.alteracoesSalariais.length > 0 && (
            <>
              <SectionHeader title="Alterações Salariais" />
              <table className="w-full border-collapse border border-gray-300 text-[10px]">
                <thead className="bg-gray-50">
                  <tr>
                    {['Data', 'Salário', 'Índice', 'Motivo'].map(h => (
                      <th key={h} className="border border-gray-300 px-2 py-1 text-left font-semibold">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {ficha.alteracoesSalariais.map((r, i) => (
                    <tr key={i} className="even:bg-gray-50">
                      <td className="border border-gray-300 px-2 py-0.5 whitespace-nowrap">{r.data}</td>
                      <td className="border border-gray-300 px-2 py-0.5 text-right">{fmt(r.salario)}</td>
                      <td className="border border-gray-300 px-2 py-0.5 text-right">{r.indice != null ? r.indice.toFixed(2) : ''}</td>
                      <td className="border border-gray-300 px-2 py-0.5">{r.motivo ?? ''}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}

          {/* Registros de Férias */}
          {ficha.ferias.length > 0 && (
            <>
              <SectionHeader title="Registros de Férias" />
              <table className="w-full border-collapse border border-gray-300 text-[10px]">
                <thead className="bg-gray-50">
                  <tr>
                    {['Período Aquisitivo', 'Período de Gozo', 'Dias Gozo', 'Dias Abono', 'Dias Licença'].map(h => (
                      <th key={h} className="border border-gray-300 px-2 py-1 text-left font-semibold">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {ficha.ferias.map((r, i) => (
                    <tr key={i} className="even:bg-gray-50">
                      <td className="border border-gray-300 px-2 py-0.5">{r.periodoAquisitivo}</td>
                      <td className="border border-gray-300 px-2 py-0.5">{r.periodoGozo ?? ''}</td>
                      <td className="border border-gray-300 px-2 py-0.5 text-center">{r.diasGozo ?? ''}</td>
                      <td className="border border-gray-300 px-2 py-0.5 text-center">{r.diasAbono ?? ''}</td>
                      <td className="border border-gray-300 px-2 py-0.5 text-center">{r.diasLicenca ?? ''}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}

          {/* Registros de Afastamentos */}
          {ficha.afastamentos.length > 0 && (
            <>
              <SectionHeader title="Registros de Afastamentos" />
              <table className="w-full border-collapse border border-gray-300 text-[10px]">
                <thead className="bg-gray-50">
                  <tr>
                    {['Período', 'Afastamento', 'Qtde. de Dias'].map(h => (
                      <th key={h} className="border border-gray-300 px-2 py-1 text-left font-semibold">{h}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {ficha.afastamentos.map((r, i) => (
                    <tr key={i} className="even:bg-gray-50">
                      <td className="border border-gray-300 px-2 py-0.5">{r.periodo}</td>
                      <td className="border border-gray-300 px-2 py-0.5">{r.afastamento ?? ''}</td>
                      <td className="border border-gray-300 px-2 py-0.5 text-center">{r.qtdDias ?? ''}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}

        </div>
      </div>
    </div>
  );
}
