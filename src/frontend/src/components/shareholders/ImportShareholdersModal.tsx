import { useRef, useState } from 'react';
import { Download, Upload, X } from 'lucide-react';
import { useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { Button } from '@/components/ui';
import { shareholderService } from '@/services/shareholderService';
import { useAuthStore } from '@/stores/authStore';
import { useClientCompanies } from '@/hooks/useClient';
import { useClientStore } from '@/stores/clientStore';
import { DocumentType, ShareholderType, Gender, MaritalStatus, type CreateShareholderRequest } from '@/types';

interface Props {
  isOpen: boolean;
  onClose: () => void;
}

const TYPE_MAP: Record<string, ShareholderType> = {
  fundador: ShareholderType.Founder,
  investidor: ShareholderType.Investor,
  funcionario: ShareholderType.Employee,
  consultor: ShareholderType.Advisor,
  esop: ShareholderType.ESOP,
  socio: ShareholderType.Partner,
  outro: ShareholderType.Other,
};

const MARITAL_MAP: Record<string, MaritalStatus> = {
  solteiro: MaritalStatus.Single,
  casado: MaritalStatus.Married,
  'uniao estavel': MaritalStatus.StableUnion,
  divorciado: MaritalStatus.Divorced,
  viuvo: MaritalStatus.Widowed,
};

const GENDER_MAP: Record<string, Gender> = {
  masculino: Gender.Male,
  feminino: Gender.Female,
  outro: Gender.Other,
  'nao informado': Gender.NotInformed,
};

const BOOL_FIELDS = [
  ['earnOut', 'Earn-out'],
  ['tagAlong', 'Tag Along'],
  ['dragAlong', 'Drag Along'],
  ['shareholdersAgreement', 'Acordo de Sócios'],
  ['rightOfFirstRefusal', 'Direito de Preferência'],
  ['liquidationPreferenceRight', 'Preferência de Liquidação'],
  ['antiDilution', 'Anti-diluição'],
] as const;

const COLUMNS = [
  { header: 'Nome*', key: 'name', width: 32 },
  { header: 'Tipo Documento* (CPF/CNPJ)', key: 'documentType', width: 26 },
  { header: 'Documento* (somente números)', key: 'document', width: 28 },
  { header: 'Tipo Sócio* (Fundador/Investidor/Funcionario/Consultor/ESOP/Socio/Outro)', key: 'type', width: 40 },
  { header: 'E-mail', key: 'email', width: 30 },
  { header: 'Telefone', key: 'phone', width: 18 },
  { header: 'Data Nascimento (AAAA-MM-DD)', key: 'birthDate', width: 22 },
  { header: 'Gênero (Masculino/Feminino/Outro/Nao informado)', key: 'gender', width: 28 },
  { header: 'Estado Civil (Solteiro/Casado/Uniao estavel/Divorciado/Viuvo)', key: 'maritalStatus', width: 30 },
  { header: 'Logradouro', key: 'addressStreet', width: 30 },
  { header: 'Número', key: 'addressNumber', width: 12 },
  { header: 'Complemento', key: 'addressComplement', width: 20 },
  { header: 'CEP', key: 'addressZipCode', width: 12 },
  { header: 'Cidade', key: 'addressCity', width: 20 },
  { header: 'UF (2 letras)', key: 'addressState', width: 10 },
  ...BOOL_FIELDS.map(([key, label]) => ({ header: `${label} (Sim/Nao)`, key, width: 18 })),
  { header: 'Observações', key: 'notes', width: 30 },
  { header: 'Empresa* (nome exato; repita o documento em outra linha para vincular a outra empresa)', key: 'company', width: 40 },
];

const normalize = (v: string) =>
  v.normalize('NFD').replace(/[\u0300-\u036f]/g, '').trim().toLowerCase();

const cellText = (v: unknown): string => {
  if (v == null) return '';
  if (v instanceof Date) return v.toISOString().slice(0, 10);
  if (typeof v === 'object') {
    const o = v as { text?: string; result?: unknown; hyperlink?: string };
    return String(o.text ?? o.result ?? o.hyperlink ?? '').trim();
  }
  return String(v).trim();
};

export function ImportShareholdersModal({ isOpen, onClose }: Props) {
  const fileRef = useRef<HTMLInputElement>(null);
  const queryClient = useQueryClient();
  const { user } = useAuthStore();
  const { selectedCompanyId } = useClientStore();
  const { data: companies } = useClientCompanies(user?.clientId || '');
  const [file, setFile] = useState<File | null>(null);
  const [importing, setImporting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);

  if (!isOpen) return null;

  const handleClose = () => {
    setFile(null);
    setErrors([]);
    onClose();
  };

  const downloadTemplate = async () => {
    const ExcelJS = (await import('exceljs')).default;
    const wb = new ExcelJS.Workbook();
    const ws = wb.addWorksheet('Sócios');
    ws.columns = COLUMNS;
    ws.getRow(1).font = { bold: true };
    ws.getRow(1).alignment = { wrapText: true, vertical: 'middle' };
    ws.getRow(1).height = 45;
    ws.addRow({
      name: 'João da Silva',
      documentType: 'CPF',
      document: '12345678909',
      type: 'Fundador',
      email: 'joao@exemplo.com',
      phone: '11999999999',
      birthDate: '1985-03-20',
      gender: 'Masculino',
      maritalStatus: 'Casado',
      addressStreet: 'Av. Paulista',
      addressNumber: '1000',
      addressComplement: 'Sala 10',
      addressZipCode: '01310100',
      addressCity: 'São Paulo',
      addressState: 'SP',
      earnOut: 'Nao',
      tagAlong: 'Sim',
      dragAlong: 'Sim',
      shareholdersAgreement: 'Sim',
      rightOfFirstRefusal: 'Nao',
      liquidationPreferenceRight: 'Nao',
      antiDilution: 'Nao',
      notes: 'Exemplo - apague esta linha antes de importar',
      company: companies?.[0]?.name ?? '',
    });

    const list = (items: string) => ({ type: 'list' as const, allowBlank: true, formulae: [`"${items}"`] });
    const boolStart = COLUMNS.findIndex((c) => c.key === BOOL_FIELDS[0][0]) + 1;
    const textCols = ['document', 'addressNumber', 'addressZipCode', 'phone', 'birthDate'];
    for (let r = 2; r <= 500; r++) {
      ws.getCell(r, 2).dataValidation = list('CPF,CNPJ');
      ws.getCell(r, 4).dataValidation = list('Fundador,Investidor,Funcionario,Consultor,ESOP,Socio,Outro');
      ws.getCell(r, 8).dataValidation = list('Masculino,Feminino,Outro,Nao informado');
      ws.getCell(r, 9).dataValidation = list('Solteiro,Casado,Uniao estavel,Divorciado,Viuvo');
      BOOL_FIELDS.forEach((_, i) => {
        ws.getCell(r, boolStart + i).dataValidation = list('Sim,Nao');
      });
      textCols.forEach((k) => {
        ws.getCell(r, COLUMNS.findIndex((c) => c.key === k) + 1).numFmt = '@';
      });
    }
    const companyCol = COLUMNS.findIndex((c) => c.key === 'company') + 1;
    if (companies?.length) {
      const wsC = wb.addWorksheet('Empresas');
      wsC.getColumn(1).width = 40;
      wsC.addRow(['Empresas do grupo']);
      wsC.getRow(1).font = { bold: true };
      companies.forEach((c) => wsC.addRow([c.name]));
      for (let r = 2; r <= 500; r++) {
        ws.getCell(r, companyCol).dataValidation = {
          type: 'list',
          allowBlank: true,
          formulae: [`Empresas!$A$2:$A$${companies.length + 1}`],
        };
      }
    }
    const buffer = await wb.xlsx.writeBuffer();
    const url = URL.createObjectURL(
      new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
    );
    const a = document.createElement('a');
    a.href = url;
    a.download = 'modelo-importacao-socios.xlsx';
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleImport = async () => {
    if (!file) return;
    const companyId = selectedCompanyId || user?.companyId || '';
    if (!companyId) {
      toast.error('Selecione uma empresa antes de importar');
      return;
    }

    setImporting(true);
    setErrors([]);
    const errs: string[] = [];
    let ok = 0;

    try {
      const ExcelJS = (await import('exceljs')).default;
      const wb = new ExcelJS.Workbook();
      await wb.xlsx.load(await file.arrayBuffer());
      const ws = wb.worksheets[0];
      if (!ws) throw new Error('Planilha vazia');

      const rows: { line: number; values: string[] }[] = [];
      ws.eachRow((row, line) => {
        if (line === 1) return;
        const values = COLUMNS.map((_, i) => cellText(row.getCell(i + 1).value));
        if (values.some(Boolean)) rows.push({ line, values });
      });

      if (rows.length === 0) {
        toast.error('Nenhum registro encontrado na planilha');
        return;
      }

      for (const { line, values } of rows) {
        const v: Record<string, string> = {};
        COLUMNS.forEach((c, i) => { v[c.key] = values[i]; });
        const name = v.name;
        const document = v.document.replace(/\D/g, '');
        const isCnpj = normalize(v.documentType) === 'cnpj';
        const type = TYPE_MAP[normalize(v.type)];

        if (!name) { errs.push(`Linha ${line}: nome é obrigatório`); continue; }
        if (!['cpf', 'cnpj'].includes(normalize(v.documentType))) { errs.push(`Linha ${line}: tipo de documento inválido`); continue; }
        if (!document) { errs.push(`Linha ${line}: documento é obrigatório`); continue; }
        if (!type) { errs.push(`Linha ${line}: tipo de sócio inválido`); continue; }

        let rowCompanyId = companyId;
        if (v.company) {
          const found = companies?.find((c) => normalize(c.name) === normalize(v.company));
          if (!found) { errs.push(`Linha ${line}: empresa "${v.company}" não encontrada`); continue; }
          rowCompanyId = found.id;
        }

        const gender = v.gender ? GENDER_MAP[normalize(v.gender)] : undefined;
        if (v.gender && !gender) { errs.push(`Linha ${line}: gênero inválido`); continue; }
        const maritalStatus = v.maritalStatus ? MARITAL_MAP[normalize(v.maritalStatus)] : undefined;
        if (v.maritalStatus && !maritalStatus) { errs.push(`Linha ${line}: estado civil inválido`); continue; }
        if (v.birthDate && !/^\d{4}-\d{2}-\d{2}$/.test(v.birthDate)) { errs.push(`Linha ${line}: data de nascimento deve ser AAAA-MM-DD`); continue; }
        if (v.addressState && v.addressState.length !== 2) { errs.push(`Linha ${line}: UF deve ter 2 letras`); continue; }

        const bools: Record<string, boolean | undefined> = {};
        let boolError = false;
        for (const [key, label] of BOOL_FIELDS) {
          const raw = normalize(v[key]);
          if (!raw) continue;
          if (!['sim', 'nao'].includes(raw)) { errs.push(`Linha ${line}: ${label} deve ser Sim ou Nao`); boolError = true; break; }
          bools[key] = raw === 'sim';
        }
        if (boolError) continue;

        const payload: CreateShareholderRequest = {
          companyId: rowCompanyId,
          name,
          document,
          documentType: isCnpj ? DocumentType.Cnpj : DocumentType.Cpf,
          type,
          email: v.email || undefined,
          phone: v.phone || undefined,
          birthDate: v.birthDate || undefined,
          gender,
          maritalStatus,
          addressStreet: v.addressStreet || undefined,
          addressNumber: v.addressNumber || undefined,
          addressComplement: v.addressComplement || undefined,
          addressZipCode: v.addressZipCode || undefined,
          addressCity: v.addressCity || undefined,
          addressState: v.addressState ? v.addressState.toUpperCase() : undefined,
          notes: v.notes || undefined,
          ...bools,
        };

        try {
          await shareholderService.createShareholder(payload);
          ok++;
        } catch (e) {
          const err = e as { response?: { data?: { message?: string; errors?: { message?: string }[] | string[] } } };
          const first = err.response?.data?.errors?.[0];
          const detail =
            (typeof first === 'string' ? first : first?.message) ||
            err.response?.data?.message ||
            'erro ao salvar';
          errs.push(`Linha ${line} (${name}): ${detail}`);
        }
      }

      queryClient.invalidateQueries({ queryKey: ['shareholders'] });
      if (ok > 0) toast.success(`${ok} sócio(s) importado(s) com sucesso`);
      if (errs.length === 0) {
        handleClose();
      } else {
        setErrors(errs);
      }
    } catch {
      toast.error('Não foi possível ler o arquivo. Use o modelo .xlsx disponibilizado.');
    } finally {
      setImporting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-lg p-6 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Importar sócios</h2>
          <button onClick={handleClose} aria-label="Fechar">
            <X className="w-5 h-5" />
          </button>
        </div>

        <p className="text-sm text-gray-600 dark:text-gray-300">
          1. Baixe o modelo, 2. preencha os dados (apague a linha de exemplo), 3. selecione o arquivo e importe.
        </p>

        <Button variant="secondary" icon={<Download className="w-4 h-4" />} onClick={downloadTemplate}>
          Baixar modelo Excel
        </Button>

        <div>
          <input
            ref={fileRef}
            type="file"
            accept=".xlsx"
            onChange={(e) => {
              setFile(e.target.files?.[0] ?? null);
              setErrors([]);
            }}
            className="block w-full text-sm"
          />
        </div>

        {errors.length > 0 && (
          <div className="max-h-40 overflow-auto rounded border border-red-300 bg-red-50 p-3 text-sm text-red-700">
            {errors.map((e, i) => (
              <div key={i}>{e}</div>
            ))}
          </div>
        )}

        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={handleClose}>
            Fechar
          </Button>
          <Button icon={<Upload className="w-4 h-4" />} onClick={handleImport} disabled={!file || importing}>
            {importing ? 'Importando...' : 'Importar'}
          </Button>
        </div>
      </div>
    </div>
  );
}
