import {
  shareholderStatusLabels,
  shareholderTypeLabels,
  type Shareholder,
} from '@/types';

const HEADERS = ['Nome', 'Empresa', 'Tipo', 'Status', 'Documento', 'E-mail', 'Telefone', 'Cadastro'];

// Aspas escapadas; prefixo ' evita execução de fórmulas ao abrir no Excel
function escapeCell(value: string): string {
  const safe = /^[=+\-@\t\r]/.test(value) ? `'${value}` : value;
  return `"${safe.replace(/"/g, '""')}"`;
}

export function buildShareholdersCsv(shareholders: Shareholder[]): string {
  const rows = shareholders.map((s) => [
    s.name,
    s.companies && s.companies.length > 1
      ? s.companies.map((c) => c.name).join(', ')
      : (s.companyName ?? ''),
    shareholderTypeLabels[s.type] ?? '',
    shareholderStatusLabels[s.status] ?? '',
    s.documentFormatted || s.document || '',
    s.email ?? '',
    s.phone ?? '',
    new Date(s.createdAt).toLocaleDateString('pt-BR'),
  ]);

  return [HEADERS, ...rows].map((row) => row.map((c) => escapeCell(String(c))).join(';')).join('\r\n');
}

export function exportShareholdersToCsv(shareholders: Shareholder[]): void {
  // BOM para o Excel reconhecer UTF-8; separador ';' é o padrão do Excel pt-BR
  const blob = new Blob(['\uFEFF' + buildShareholdersCsv(shareholders)], {
    type: 'text/csv;charset=utf-8;',
  });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = `socios-${new Date().toISOString().slice(0, 10)}.csv`;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
