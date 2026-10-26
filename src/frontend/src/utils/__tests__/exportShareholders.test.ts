import { describe, expect, it } from 'vitest';
import { buildShareholdersCsv } from '../exportShareholders';
import type { Shareholder } from '@/types';

const base = {
  id: '1',
  name: 'Ana "A" Silva',
  companyName: 'Empresa X',
  type: 0,
  status: 0,
  document: '123',
  email: '=cmd@x.com',
  phone: '',
  createdAt: '2026-01-02T00:00:00',
} as unknown as Shareholder;

describe('buildShareholdersCsv', () => {
  it('gera cabeçalho e linha, escapando aspas e fórmulas', () => {
    const lines = buildShareholdersCsv([base]).split('\r\n');
    expect(lines).toHaveLength(2);
    expect(lines[0]).toContain('"Nome";"Empresa"');
    expect(lines[1]).toContain('"Ana ""A"" Silva"');
    expect(lines[1]).toContain(`"'=cmd@x.com"`);
  });
});
