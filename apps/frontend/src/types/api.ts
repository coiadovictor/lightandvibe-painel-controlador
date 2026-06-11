export interface HealthResponse {
  status: string;
  version: string;
  timestamp: string;
}

export interface AccessByEmployee {
  employeeId: string;
  employeeName: string;
  accessCount: number;
}

export interface DashboardSummary {
  totalEmployeesLogged: number;
  totalAccesses: number;
  totalConversations: number;
  accessesByEmployee: AccessByEmployee[];
}

// Funcionários que já se logaram em outro sistema — pag_funcionario_acesso JOIN pag_funcionario
export interface Employee {
  id: string;          // matrícula
  name: string;
  email?: string | null;
  department?: string | null;
  phone?: string | null;
  lastContactAt?: string | null;
}

export interface InformationType {
  id: string;
  name: string;
  description?: string | null;
}

export interface HollerithLinha {
  codigo: number;
  descricao: string;
  quantidade: string;
  vencimento: number | null;
  desconto: number | null;
}

export interface HollerithData {
  matricula: string;
  nome: string;
  cpf: string;
  empresa: string;
  cnpjEmpresa: string;
  local: string;
  secao: string;
  cargo: string;
  admissao: string;
  pis: string;
  salario: number | null;
  mes: number;
  ano: number;
  linhas: HollerithLinha[];
  totalVencimentos: number;
  totalDescontos: number;
  liquido: number;
}

export interface AlteracaoCargo {
  data: string;
  cargo: string;
  motivo: string | null;
  cboEsocial: string | null;
}

export interface AlteracaoSalarial {
  data: string;
  salario: number;
  indice: number | null;
  motivo: string | null;
}

export interface Ferias {
  periodoAquisitivo: string;
  periodoGozo: string | null;
  diasGozo: number | null;
  diasAbono: number | null;
  diasLicenca: number | null;
}

export interface Afastamento {
  periodo: string;
  afastamento: string | null;
  qtdDias: number | null;
}

export interface FichaRegistro {
  empresa: string;
  cnpjEmpresa: string | null;
  enderecoEmpresa: string | null;
  bairroEmpresa: string | null;
  municipioEmpresa: string | null;
  ufEmpresa: string | null;
  cnaeEmpresa: string | null;
  matricula: string;
  nome: string;
  cpf: string | null;
  pis: string | null;
  nascimento: string | null;
  sexo: string | null;
  estadoCivil: string | null;
  nacionalidade: string | null;
  nomeMae: string | null;
  nomePai: string | null;
  grauInstrucao: string | null;
  endereco: string | null;
  bairro: string | null;
  cep: string | null;
  municipio: string | null;
  uf: string | null;
  rg: string | null;
  rgOrgao: string | null;
  rgExpedicao: string | null;
  rgUf: string | null;
  ctps: string | null;
  ctpsSerie: string | null;
  ctpsExpedicao: string | null;
  ctpsUf: string | null;
  cargo: string | null;
  cboEsocial: string | null;
  departamento: string | null;
  setor: string | null;
  admissao: string | null;
  desligamento: string | null;
  salarioAtual: number | null;
  horasMensais: string | null;
  sindicato: string | null;
  formaRemuneracao: string | null;
  emailContato: string | null;
  celular: string | null;
  alteracoesCargo: AlteracaoCargo[];
  alteracoesSalariais: AlteracaoSalarial[];
  ferias: Ferias[];
  afastamentos: Afastamento[];
}

export interface LogEntry {
  id: string;
  level: string;
  message: string;
  source?: string | null;
  createdAt: string;
}

export interface Integracao {
  id: number;
  endpoint: string;
  tabela: string;
  acao?: string | null;
  origem?: string | null;
  quantidadeRegistros?: number | null;
  situacao: 'em_andamento' | 'sucesso' | 'erro';
  mensagemErro?: string | null;
  dataInicio: string;
  dataFim?: string | null;
}

// --- Logs Internos do Ambiente ---

export interface ContainerHealth {
  alias: string;
  matcher: string;
  name?: string | null;
  id?: string | null;
  found: boolean;
  status: string;        // running | exited | restarting | not_found | ...
  restartCount: number;
  oomKilled: boolean;
  exitCode: number;
  startedAt?: string | null;
  image?: string | null;
}

export interface Incident {
  timestamp?: string | null;
  container: string;
  type: 'restart' | 'oom' | 'exit' | 'log';
  severity: 'warning' | 'error' | 'critical';
  message: string;
  detail?: string | null;
}

export interface AmbienteOverview {
  available: boolean;
  message?: string | null;
  windowHours: number;
  containers: ContainerHealth[];
  incidents: Incident[];
}

export interface LogLine {
  timestamp?: string | null;
  stream: 'stdout' | 'stderr';
  text: string;
}

export interface ContainerLogs {
  available: boolean;
  message?: string | null;
  container: string;
  lines: LogLine[];
}
