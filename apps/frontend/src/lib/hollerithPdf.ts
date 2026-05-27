import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';

const MESES = [
  '', 'Janeiro', 'Fevereiro', 'Março', 'Abril', 'Maio', 'Junho',
  'Julho', 'Agosto', 'Setembro', 'Outubro', 'Novembro', 'Dezembro',
];

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

function fmt(value: number | null | undefined): string {
  if (value == null) return '';
  return value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

async function getLogoBase64(): Promise<string | null> {
  try {
    const res = await fetch('/logo.jpeg');
    const blob = await res.blob();
    return await new Promise<string>(resolve => {
      const reader = new FileReader();
      reader.onload = () => resolve(reader.result as string);
      reader.readAsDataURL(blob);
    });
  } catch {
    return null;
  }
}

export async function generateHollerithPdf(data: HollerithData): Promise<void> {
  const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
  const W = 210;
  const margin = 12;
  const contentW = W - margin * 2;
  let y = margin;

  const logoBase64 = await getLogoBase64();

  // ── Header ────────────────────────────────────────────────────────────────
  const headerH = 18;
  doc.setDrawColor(180);
  doc.setLineWidth(0.3);
  doc.rect(margin, y, contentW, headerH);

  // Logo (left box)
  const logoBoxW = 48;
  doc.line(margin + logoBoxW, y, margin + logoBoxW, y + headerH);
  if (logoBase64) {
    doc.addImage(logoBase64, 'JPEG', margin + 2, y + 2, logoBoxW - 4, headerH - 4);
  } else {
    doc.setFontSize(9);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(0);
    doc.text('Light & Vibe', margin + logoBoxW / 2, y + headerH / 2 + 1, { align: 'center' });
  }

  // Title (center)
  const titleBoxW = contentW - logoBoxW - 30;
  doc.setFontSize(13);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(0);
  doc.text('DEMONSTRATIVO DE PAGAMENTO', margin + logoBoxW + titleBoxW / 2, y + headerH / 2 + 2, { align: 'center' });

  // Mês/Ano box (right)
  const mesBoxX = margin + logoBoxW + titleBoxW;
  doc.line(mesBoxX, y, mesBoxX, y + headerH);
  doc.line(mesBoxX, y + 6, W - margin, y + 6);
  doc.setFontSize(6);
  doc.setFont('helvetica', 'normal');
  doc.text('MÊS/ANO', mesBoxX + (30 / 2), y + 4.5, { align: 'center' });
  doc.setFontSize(9);
  doc.setFont('helvetica', 'bold');
  doc.text(`${String(data.mes).padStart(2, '0')}/${data.ano}`, mesBoxX + (30 / 2), y + 13, { align: 'center' });

  y += headerH;

  // ── Info rows helper ──────────────────────────────────────────────────────
  function infoRow(items: { label: string; value: string; w?: number }[], h = 9) {
    doc.setLineWidth(0.3);
    let x = margin;
    for (const item of items) {
      const w = item.w ?? (contentW / items.length);
      doc.rect(x, y, w, h);
      doc.setFontSize(5.5);
      doc.setFont('helvetica', 'normal');
      doc.setTextColor(80);
      doc.text(item.label.toUpperCase(), x + 1.5, y + 3.5);
      doc.setFontSize(8);
      doc.setFont('helvetica', 'bold');
      doc.setTextColor(0);
      doc.text(item.value, x + 1.5, y + 7.5);
      x += w;
    }
    y += h;
  }

  // EMPRESA / CNPJ
  infoRow([
    { label: 'Empresa', value: data.empresa, w: contentW - 45 },
    { label: 'CNPJ/MF', value: data.cnpjEmpresa, w: 45 },
  ]);

  // NOME / CPF
  infoRow([
    { label: 'Nome do Funcionário', value: data.nome, w: contentW - 40 },
    { label: 'CPF', value: data.cpf, w: 40 },
  ]);

  // LOCAL / SEÇÃO / ADMISSÃO / MATRÍCULA
  const admissao = data.admissao ? data.admissao.substring(0, 10) : '';
  infoRow([
    { label: 'Local', value: data.local, w: 70 },
    { label: 'Seção', value: data.secao, w: 60 },
    { label: 'Admissão', value: admissao, w: 30 },
    { label: 'Matrícula', value: data.matricula, w: contentW - 160 },
  ]);

  // PIS / CARGO / SALÁRIO (somente se verba 9001 presente)
  if (data.salario != null) {
    infoRow([
      { label: 'PIS', value: data.pis, w: 40 },
      { label: 'Cargo', value: data.cargo, w: contentW - 40 - 45 },
      { label: 'Salário', value: `R$ ${fmt(data.salario)}`, w: 45 },
    ]);
  } else {
    infoRow([
      { label: 'PIS', value: data.pis, w: 40 },
      { label: 'Cargo', value: data.cargo, w: contentW - 40 },
    ]);
  }

  y += 2;

  // ── Verbas table ──────────────────────────────────────────────────────────
  const tableRows = data.linhas.map(l => [
    String(l.codigo),
    l.descricao,
    l.quantidade,
    l.vencimento != null ? fmt(l.vencimento) : '',
    l.desconto != null ? fmt(l.desconto) : '',
  ]);

  // Totals row
  tableRows.push([
    '', 'Totais', '',
    fmt(data.totalVencimentos),
    fmt(data.totalDescontos),
  ]);

  autoTable(doc, {
    startY: y,
    margin: { left: margin, right: margin },
    head: [['Cód.', 'Descrição', 'Quantidade', 'Vencimentos', 'Descontos']],
    body: tableRows,
    styles: {
      fontSize: 8,
      cellPadding: { top: 1.5, bottom: 1.5, left: 2, right: 2 },
      lineWidth: 0.2,
      lineColor: [180, 180, 180],
      textColor: [0, 0, 0],
    },
    headStyles: {
      fillColor: [245, 245, 245],
      textColor: [0, 0, 0],
      fontStyle: 'bold',
      halign: 'center',
    },
    columnStyles: {
      0: { halign: 'center', cellWidth: 14 },
      1: { cellWidth: 'auto' },
      2: { halign: 'center', cellWidth: 22 },
      3: { halign: 'right', cellWidth: 30 },
      4: { halign: 'right', cellWidth: 30 },
    },
    didParseCell(data) {
      // Bold totals row
      if (data.row.index === tableRows.length - 1) {
        data.cell.styles.fontStyle = 'bold';
        data.cell.styles.fillColor = [235, 235, 235];
      }
    },
  });

  const finalY = (doc as jsPDF & { lastAutoTable: { finalY: number } }).lastAutoTable.finalY + 2;

  // ── Líquido ──────────────────────────────────────────────────────────────
  const liqW = 60;
  const liqX = W - margin - liqW;
  doc.setLineWidth(0.3);
  doc.setDrawColor(180);
  doc.rect(liqX, finalY, liqW, 9);
  doc.line(liqX + 30, finalY, liqX + 30, finalY + 9);
  doc.setFontSize(8);
  doc.setFont('helvetica', 'bold');
  doc.setTextColor(0);
  doc.text('Líquido :', liqX + 28, finalY + 6, { align: 'right' });
  doc.text(fmt(data.liquido), liqX + liqW - 2, finalY + 6, { align: 'right' });

  // ── Footer ────────────────────────────────────────────────────────────────
  doc.setFontSize(7);
  doc.setFont('helvetica', 'normal');
  doc.setTextColor(130);
  doc.text(
    `Holerite gerado em ${new Date().toLocaleDateString('pt-BR')} — ${MESES[data.mes]}/${data.ano} — Light & Vibe AI Automation`,
    W / 2,
    287,
    { align: 'center' },
  );

  const filename = `holerite_${data.matricula}_${String(data.mes).padStart(2, '0')}_${data.ano}.pdf`;
  doc.save(filename);
}
