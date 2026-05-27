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

export interface LogEntry {
  id: string;
  level: string;
  message: string;
  source?: string | null;
  createdAt: string;
}
