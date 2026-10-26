import { useEffect, useState } from 'react';
import toast from 'react-hot-toast';
import { useForm, type FieldErrors } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { X, Plus, Trash2 } from 'lucide-react';
import { Button, Input } from '@/components/ui';
import {
  ShareholderType,
  ShareholderStatus,
  Gender,
  MaritalStatus,
  DocumentType,
  shareholderTypeLabels,
  shareholderStatusLabels,
  genderLabels,
  maritalStatusLabels,
  documentTypeLabels,
  type Shareholder,
  type CreateShareholderRequest,
  type UpdateShareholderRequest,
} from '@/types';
import { useCreateShareholder, useUpdateShareholder } from '@/hooks/useShareholders';
import { useClientCompanies } from '@/hooks/useClient';
import { useAuthStore } from '@/stores/authStore';
import { useClientStore } from '@/stores/clientStore';
import { isValidCnpj, isValidCpf, normalizeDocument } from '@/utils/documentValidators';

const brazilStates = [
  'AC',
  'AL',
  'AP',
  'AM',
  'BA',
  'CE',
  'DF',
  'ES',
  'GO',
  'MA',
  'MT',
  'MS',
  'MG',
  'PA',
  'PB',
  'PR',
  'PE',
  'PI',
  'RJ',
  'RN',
  'RS',
  'RO',
  'RR',
  'SC',
  'SP',
  'SE',
  'TO',
];

type GovernanceFlags = {
  earnOut: boolean;
  tagAlong: boolean;
  dragAlong: boolean;
  shareholdersAgreement: boolean;
  rightOfFirstRefusal: boolean;
  liquidationPreferenceRight: boolean;
  antiDilution: boolean;
};

type CompanyRow = GovernanceFlags & { companyId: string; type: ShareholderType };

const pickFlags = (s: Partial<GovernanceFlags>): GovernanceFlags => ({
  earnOut: !!s.earnOut,
  tagAlong: !!s.tagAlong,
  dragAlong: !!s.dragAlong,
  shareholdersAgreement: !!s.shareholdersAgreement,
  rightOfFirstRefusal: !!s.rightOfFirstRefusal,
  liquidationPreferenceRight: !!s.liquidationPreferenceRight,
  antiDilution: !!s.antiDilution,
});

const newCompanyRow = (companyId = ''): CompanyRow => ({
  companyId,
  type: ShareholderType.Investor,
  ...pickFlags({}),
});

// Validation schema
const shareholderSchema = z
  .object({
    companyId: z.string().min(1, 'Selecione uma empresa'),
    name: z.string().min(2, 'Nome deve ter pelo menos 2 caracteres'),
    document: z
      .string()
      .min(1, 'Documento é obrigatório')
      .transform((value) => normalizeDocument(value)),
    documentType: z.nativeEnum(DocumentType),
    type: z.nativeEnum(ShareholderType),
    status: z.nativeEnum(ShareholderStatus).optional(),
    email: z.string().email('E-mail inválido').optional().or(z.literal('')),
    phone: z.string().optional(),
    notes: z.string().optional(),
    addressStreet: z.string().optional(),
    addressNumber: z.string().optional(),
    addressComplement: z.string().optional(),
    addressZipCode: z.string().optional().or(z.literal('')),
    addressCity: z.string().optional(),
    addressState: z.string().optional(),
    maritalStatus: z.nativeEnum(MaritalStatus).optional(),
    gender: z.nativeEnum(Gender).optional(),
    birthDate: z.string().optional().or(z.literal('')),
    earnOut: z.boolean().default(false),
    tagAlong: z.boolean().default(false),
    dragAlong: z.boolean().default(false),
    shareholdersAgreement: z.boolean().default(false),
    rightOfFirstRefusal: z.boolean().default(false),
    liquidationPreferenceRight: z.boolean().default(false),
    antiDilution: z.boolean().default(false),
  })
  .superRefine((data, ctx) => {
    const documentLength = data.document?.length ?? 0;

    if (data.documentType === DocumentType.Cpf) {
      if (documentLength !== 11 || !isValidCpf(data.document)) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Documento inválido',
          path: ['document'],
        });
      }
    }

    if (data.documentType === DocumentType.Cnpj) {
      if (documentLength !== 14 || !isValidCnpj(data.document)) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Documento inválido',
          path: ['document'],
        });
      }
    }

    const cep = normalizeDocument(data.addressZipCode);
    if (cep && cep.length !== 8) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'CEP inválido',
        path: ['addressZipCode'],
      });
    }

    if (data.addressState && data.addressState.trim().length !== 2) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'UF inválida',
        path: ['addressState'],
      });
    }

    if (data.birthDate) {
      const birth = new Date(data.birthDate);
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      if (Number.isNaN(birth.getTime()) || birth > today) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Data de nascimento inválida',
          path: ['birthDate'],
        });
      }
    }
  });

type ShareholderFormData = z.infer<typeof shareholderSchema>;

interface ShareholderFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  shareholder?: Shareholder;
}

export function ShareholderFormModal({
  isOpen,
  onClose,
  shareholder,
}: ShareholderFormModalProps) {
  const isEditing = !!shareholder;
  const { user } = useAuthStore();
  const { selectedCompanyId } = useClientStore();
  const { data: companiesData } = useClientCompanies(user?.clientId || '');
  const [companyRows, setCompanyRows] = useState<CompanyRow[]>([]);

  const createMutation = useCreateShareholder();
  const updateMutation = useUpdateShareholder();

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    setError,
    clearErrors,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<ShareholderFormData>({
    resolver: zodResolver(shareholderSchema),
    defaultValues: {
      companyId: selectedCompanyId || user?.companyId || '',
      name: '',
      document: '',
      documentType: DocumentType.Cpf,
      type: ShareholderType.Founder,
      status: ShareholderStatus.Active,
      email: '',
      phone: '',
      notes: '',
      addressStreet: '',
      addressNumber: '',
      addressComplement: '',
      addressZipCode: '',
      addressCity: '',
      addressState: '',
      maritalStatus: undefined,
      gender: undefined,
      birthDate: '',
      earnOut: false,
      tagAlong: false,
      dragAlong: false,
      shareholdersAgreement: false,
      rightOfFirstRefusal: false,
      liquidationPreferenceRight: false,
      antiDilution: false,
    },
  });

  // Populate form when editing
  useEffect(() => {
    if (shareholder) {
      const rows = (shareholder.companies?.length ? shareholder.companies : [{ id: shareholder.companyId, ...pickFlags(shareholder), type: shareholder.type }])
        .map((c) => ({ companyId: c.id, ...pickFlags(c), type: c.type }));
      rows.sort((a, b) => Number(b.companyId === shareholder.companyId) - Number(a.companyId === shareholder.companyId));
      setCompanyRows(rows);
    } else {
      setCompanyRows([newCompanyRow(selectedCompanyId || user?.companyId || '')]);
    }
    if (shareholder) {
      reset({
        companyId: shareholder.companyId,
        name: shareholder.name,
        document: shareholder.document,
        documentType: shareholder.documentType,
        type: shareholder.type,
        status: shareholder.status,
        email: shareholder.email || '',
        phone: shareholder.phone || '',
        notes: shareholder.notes || '',
        addressStreet: shareholder.addressStreet || '',
        addressNumber: shareholder.addressNumber || '',
        addressComplement: shareholder.addressComplement || '',
        addressZipCode: shareholder.addressZipCode || '',
        addressCity: shareholder.addressCity || '',
        addressState: shareholder.addressState || '',
        maritalStatus: shareholder.maritalStatus,
        gender: shareholder.gender,
        birthDate: shareholder.birthDate ? shareholder.birthDate.split('T')[0] : '',
        earnOut: shareholder.earnOut ?? false,
        tagAlong: shareholder.tagAlong ?? false,
        dragAlong: shareholder.dragAlong ?? false,
        shareholdersAgreement: shareholder.shareholdersAgreement ?? false,
        rightOfFirstRefusal: shareholder.rightOfFirstRefusal ?? false,
        liquidationPreferenceRight: shareholder.liquidationPreferenceRight ?? false,
        antiDilution: shareholder.antiDilution ?? false,
      });
    } else {
      reset({
        companyId: '',
        name: '',
        document: '',
        documentType: DocumentType.Cpf,
        type: ShareholderType.Founder,
        status: ShareholderStatus.Active,
        email: '',
        phone: '',
        notes: '',
        addressStreet: '',
        addressNumber: '',
        addressComplement: '',
        addressZipCode: '',
        addressCity: '',
        addressState: '',
        maritalStatus: undefined,
        gender: undefined,
        birthDate: '',
        earnOut: false,
        tagAlong: false,
        dragAlong: false,
        shareholdersAgreement: false,
        rightOfFirstRefusal: false,
        liquidationPreferenceRight: false,
        antiDilution: false,
      });
    }
  }, [shareholder, reset, selectedCompanyId, user?.companyId]);

  const [isCepLoading, setIsCepLoading] = useState(false);
  const [lastFetchedCep, setLastFetchedCep] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<'governance' | 'qualification'>('qualification');

  useEffect(() => {
    if (isOpen) setActiveTab('qualification');
  }, [isOpen]);

  useEffect(() => {
    setValue('companyId', companyRows[0]?.companyId ?? '', { shouldValidate: false });
  }, [companyRows, setValue]);

  const updateRow = (index: number, patch: Partial<CompanyRow>) =>
    setCompanyRows((rows) => rows.map((r, i) => (i === index ? { ...r, ...patch } : r)));

  const onSubmit = async (data: ShareholderFormData) => {
    if (companyRows.length === 0 || companyRows.some((r) => !r.companyId)) {
      toast.error('Selecione a empresa em todas as linhas da configuração societária');
      setActiveTab('governance');
      return;
    }
    if (new Set(companyRows.map((r) => r.companyId)).size !== companyRows.length) {
      toast.error('Há empresas repetidas na configuração societária');
      setActiveTab('governance');
      return;
    }
    const primary = companyRows[0];
    const companiesPayload = companyRows.map(({ companyId, type, ...flags }) => ({ companyId, type, ...flags }));

    try {
      const normalizedZipCode = normalizeDocument(data.addressZipCode);
      const normalizedState = data.addressState?.trim().toUpperCase();
      const normalizedDocument = normalizeDocument(data.document);

      if (isEditing && shareholder) {
        const effectiveCompanyId = shareholder.companyId;
        const updateData: UpdateShareholderRequest = {
          companyId: effectiveCompanyId,
          companies: companiesPayload,
          name: data.name,
          document: normalizedDocument,
          documentType: data.documentType,
          type: primary.type,
          status: data.status || ShareholderStatus.Active,
          email: data.email || undefined,
          phone: data.phone || undefined,
          notes: data.notes || undefined,
          addressStreet: data.addressStreet || undefined,
          addressNumber: data.addressNumber || undefined,
          addressComplement: data.addressComplement || undefined,
          addressZipCode: normalizedZipCode || undefined,
          addressCity: data.addressCity || undefined,
          addressState: normalizedState || undefined,
          maritalStatus: data.maritalStatus,
          gender: data.gender,
          birthDate: data.birthDate || undefined,
          earnOut: primary.earnOut,
          tagAlong: primary.tagAlong,
          dragAlong: primary.dragAlong,
          shareholdersAgreement: primary.shareholdersAgreement,
          rightOfFirstRefusal: primary.rightOfFirstRefusal,
          liquidationPreferenceRight: primary.liquidationPreferenceRight,
          antiDilution: primary.antiDilution,
        };
        await updateMutation.mutateAsync({ id: shareholder.id, data: updateData });
      } else {
        const effectiveCompanyId = primary.companyId;
        const createData: CreateShareholderRequest = {
          companyId: effectiveCompanyId,
          companies: companiesPayload,
          name: data.name,
          document: normalizedDocument,
          documentType: data.documentType,
          type: primary.type,
          email: data.email || undefined,
          phone: data.phone || undefined,
          notes: data.notes || undefined,
          addressStreet: data.addressStreet || undefined,
          addressNumber: data.addressNumber || undefined,
          addressComplement: data.addressComplement || undefined,
          addressZipCode: normalizedZipCode || undefined,
          addressCity: data.addressCity || undefined,
          addressState: normalizedState || undefined,
          maritalStatus: data.maritalStatus,
          gender: data.gender,
          birthDate: data.birthDate || undefined,
          earnOut: primary.earnOut,
          tagAlong: primary.tagAlong,
          dragAlong: primary.dragAlong,
          shareholdersAgreement: primary.shareholdersAgreement,
          rightOfFirstRefusal: primary.rightOfFirstRefusal,
          liquidationPreferenceRight: primary.liquidationPreferenceRight,
          antiDilution: primary.antiDilution,
        };
        await createMutation.mutateAsync(createData);
      }
      onClose();
    } catch {
      // Error handled by mutation
    }
  };

  const onInvalid = (formErrors: FieldErrors<ShareholderFormData>) => {
    const first = Object.values(formErrors)[0];
    toast.error(first?.message?.toString() || 'Verifique os campos do formulário');
    setActiveTab('qualification');
  };

  const documentType = watch('documentType');
  const zipCode = watch('addressZipCode');

  const fetchAddressByCep = async (rawCep?: string) => {
    const cep = normalizeDocument(rawCep ?? zipCode);
    if (!cep || cep.length !== 8) {
      setError('addressZipCode', { type: 'manual', message: 'CEP inválido' });
      return;
    }

    if (cep === lastFetchedCep) return;

    setIsCepLoading(true);
    clearErrors('addressZipCode');

    try {
      const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
      const data = await response.json();

      if (!response.ok || data?.erro) {
        setError('addressZipCode', { type: 'manual', message: 'CEP não encontrado' });
        return;
      }

      setValue('addressStreet', data.logradouro || '');
      setValue('addressCity', data.localidade || '');
      setValue('addressState', data.uf || '');
      setLastFetchedCep(cep);
    } catch {
      setError('addressZipCode', { type: 'manual', message: 'Erro ao buscar CEP' });
    } finally {
      setIsCepLoading(false);
    }
  };

  useEffect(() => {
    const cep = normalizeDocument(zipCode);
    if (cep.length === 8) {
      fetchAddressByCep(cep);
    }
  }, [zipCode]);

  if (!isOpen) return null;

  // Helper for governance toggle rows
  const governanceFields: { field: keyof GovernanceFlags; short: string; label: string; help: string }[] = [
    { field: 'earnOut', short: 'Earn-out', label: 'Earn-out', help: 'Parte do pagamento vinculada a resultados futuros da empresa' },
    { field: 'tagAlong', short: 'Tag Along', label: 'Tag Along', help: 'Direito de vender junto se o controlador vender' },
    { field: 'dragAlong', short: 'Drag Along', label: 'Drag Along', help: 'Obrigação de vender junto (proteção do majoritário)' },
    { field: 'shareholdersAgreement', short: 'Acordo', label: 'Shareholders Agreement', help: 'Acordo de sócios que define regras de governança' },
    { field: 'rightOfFirstRefusal', short: 'ROFR', label: 'Direitos de preferência (ROFR)', help: 'Prioridade de compra antes de terceiros' },
    { field: 'liquidationPreferenceRight', short: 'Liq. Pref.', label: 'Liquidation Preference', help: 'Prioridade de recebimento em caso de venda/liquidação' },
    { field: 'antiDilution', short: 'Anti-dil.', label: 'Anti-diluição', help: 'Proteção contra diluição em rodadas futuras a preço menor' },
  ];

  return (
    <div className="fixed inset-0 z-50 overflow-hidden">
      {/* Backdrop */}
      <div
        className="fixed inset-0 bg-black/50 transition-opacity"
        onClick={onClose}
      />

      {/* Modal */}
      <div className="flex h-full items-center justify-center p-4">
        <div className="relative bg-white rounded-xl shadow-xl w-full max-w-5xl h-[min(90vh,760px)] flex flex-col">
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b flex-shrink-0">
            <h2 className="text-lg font-semibold text-primary">
              {isEditing ? 'Editar Sócio' : 'Novo Sócio'}
            </h2>
            <button
              onClick={onClose}
              className="p-2 hover:bg-primary-100 rounded-lg transition-colors"
            >
              <X className="w-5 h-5 text-primary-500" />
            </button>
          </div>

          {/* Tabs */}
          <div className="border-b border-gray-200 px-6 flex-shrink-0">
            <nav className="-mb-px flex gap-1">
              {([
                { id: 'qualification', label: 'Qualificação' },
                { id: 'governance', label: 'Configuração Societária' },
              ] as const).map(tab => (
                <button
                  key={tab.id}
                  type="button"
                  onClick={() => setActiveTab(tab.id)}
                  className={`px-5 py-3 text-sm font-medium border-b-2 transition-colors ${
                    activeTab === tab.id
                      ? 'border-primary-600 text-primary-700'
                      : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                  }`}
                >
                  {tab.label}
                </button>
              ))}
            </nav>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit(onSubmit, onInvalid)} className="flex flex-col flex-1 min-h-0">
            <div className="flex-1 overflow-y-auto min-h-0">
            {/* Tab: Qualificação */}
            {activeTab === 'qualification' && (
              <div className="p-6 space-y-4">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                  {/* Left */}
                  <div className="space-y-4">
                    <Input
                      label="Nome *"
                      {...register('name')}
                      error={errors.name?.message}
                      placeholder="Nome completo do sócio"
                    />

                    <div className="grid grid-cols-3 gap-4">
                      <div>
                        <label className="input-label">Tipo doc. *</label>
                        <select {...register('documentType', { valueAsNumber: true })} className="input">
                          {Object.entries(documentTypeLabels).map(([value, label]) => (
                            <option key={value} value={value}>{label}</option>
                          ))}
                        </select>
                      </div>
                      <div className="col-span-2">
                        <Input
                          label="Documento *"
                          {...register('document')}
                          error={errors.document?.message}
                          placeholder={documentType === DocumentType.Cpf ? '00000000000' : '00000000000000'}
                          maxLength={documentType === DocumentType.Cpf ? 11 : 14}
                        />
                      </div>
                    </div>

                    <Input
                      label="E-mail"
                      type="email"
                      {...register('email')}
                      error={errors.email?.message}
                      placeholder="email@exemplo.com"
                    />

                    <Input
                      label="Telefone"
                      {...register('phone')}
                      placeholder="+55 11 99999-0000"
                    />

                    <div className="grid grid-cols-2 gap-4">
                      <div>
                        <label className="input-label">Sexo</label>
                        <select
                          {...register('gender', { setValueAs: (v) => (v === '' ? undefined : Number(v)) })}
                          className="input"
                        >
                          <option value="">Selecione</option>
                          {Object.entries(genderLabels).map(([value, label]) => (
                            <option key={value} value={value}>{label}</option>
                          ))}
                        </select>
                      </div>
                      <div>
                        <label className="input-label">Estado civil</label>
                        <select
                          {...register('maritalStatus', { setValueAs: (v) => (v === '' ? undefined : Number(v)) })}
                          className="input"
                        >
                          <option value="">Selecione</option>
                          {Object.entries(maritalStatusLabels).map(([value, label]) => (
                            <option key={value} value={value}>{label}</option>
                          ))}
                        </select>
                      </div>
                    </div>

                    <Input
                      label="Data de nascimento"
                      type="date"
                      {...register('birthDate')}
                      error={errors.birthDate?.message}
                    />
                  </div>

                  {/* Right — Endereço */}
                  <div className="space-y-4">
                    <div className="grid grid-cols-3 gap-4">
                      <div className="col-span-2">
                        <Input
                          label="CEP"
                          {...register('addressZipCode')}
                          error={errors.addressZipCode?.message}
                          placeholder="00000-000"
                        />
                      </div>
                      <div className="flex items-end">
                        <Button
                          type="button"
                          variant="secondary"
                          size="sm"
                          loading={isCepLoading}
                          onClick={() => fetchAddressByCep()}
                          className="w-full"
                        >
                          Buscar CEP
                        </Button>
                      </div>
                    </div>

                    <Input label="Rua" {...register('addressStreet')} placeholder="Rua / Avenida" />

                    <div className="grid grid-cols-3 gap-4">
                      <Input label="Número" {...register('addressNumber')} placeholder="123" />
                      <div className="col-span-2">
                        <Input label="Complemento" {...register('addressComplement')} placeholder="Apto, bloco, etc." />
                      </div>
                    </div>

                    <div className="grid grid-cols-3 gap-4">
                      <div className="col-span-2">
                        <Input label="Cidade" {...register('addressCity')} placeholder="Cidade" />
                      </div>
                      <div>
                        <label className="input-label">Estado</label>
                        <select {...register('addressState')} className="input">
                          <option value="">UF</option>
                          {brazilStates.map((state) => (
                            <option key={state} value={state}>{state}</option>
                          ))}
                        </select>
                        {errors.addressState && (
                          <p className="input-error-message">{errors.addressState.message}</p>
                        )}
                      </div>
                    </div>
                  </div>
                </div>

                {/* Observações */}
                <div>
                  <label className="input-label">Observações</label>
                  <textarea
                    {...register('notes')}
                    className="input min-h-[80px] resize-y"
                    placeholder="Observações adicionais..."
                  />
                </div>
              </div>
            )}

            {/* Tab: Configuração Societária */}
            {activeTab === 'governance' && (
              <div className="p-6 space-y-4">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-sm font-semibold text-gray-700 uppercase tracking-wide">
                      Participação nas empresas do grupo
                    </h3>
                    <p className="text-xs text-gray-500 mt-1">
                      Adicione cada empresa e defina o tipo de sócio e as cláusulas aplicáveis nela.
                      A primeira linha é a empresa principal.
                    </p>
                  </div>
                  <Button
                    type="button"
                    variant="secondary"
                    icon={<Plus className="w-4 h-4" />}
                    onClick={() => setCompanyRows((rows) => [...rows, newCompanyRow()])}
                  >
                    Adicionar empresa
                  </Button>
                </div>

                {isEditing && (
                  <div className="max-w-xs">
                    <label className="input-label">Status</label>
                    <select {...register('status', { valueAsNumber: true })} className="input">
                      {Object.entries(shareholderStatusLabels).map(([value, label]) => (
                        <option key={value} value={value}>{label}</option>
                      ))}
                    </select>
                  </div>
                )}

                <div className="overflow-x-auto border rounded-lg">
                  <table className="min-w-full text-sm">
                    <thead className="bg-gray-50 text-xs text-gray-600">
                      <tr>
                        <th className="px-3 py-2 text-left font-medium min-w-[220px]">Empresa *</th>
                        <th className="px-3 py-2 text-left font-medium min-w-[130px]">Tipo *</th>
                        {governanceFields.map(({ field, short, label, help }) => (
                          <th key={field} className="px-2 py-2 text-center font-medium" title={`${label}: ${help}`}>
                            {short}
                          </th>
                        ))}
                        <th className="px-2 py-2" />
                      </tr>
                    </thead>
                    <tbody className="divide-y">
                      {companyRows.map((row, index) => {
                        const isLockedPrimary = isEditing && index === 0;
                        return (
                          <tr key={index}>
                            <td className="px-3 py-2">
                              <select
                                className="input"
                                value={row.companyId}
                                disabled={isLockedPrimary}
                                onChange={(e) => updateRow(index, { companyId: e.target.value })}
                              >
                                <option value="">Selecione</option>
                                {companiesData?.map((company) => (
                                  <option
                                    key={company.id}
                                    value={company.id}
                                    disabled={company.id !== row.companyId && companyRows.some((r) => r.companyId === company.id)}
                                  >
                                    {company.name}
                                  </option>
                                ))}
                              </select>
                              {index === 0 && <span className="text-xs text-gray-500">Principal</span>}
                            </td>
                            <td className="px-3 py-2">
                              <select
                                className="input"
                                value={row.type}
                                onChange={(e) => updateRow(index, { type: Number(e.target.value) as ShareholderType })}
                              >
                                {Object.entries(shareholderTypeLabels).map(([value, label]) => (
                                  <option key={value} value={value}>{label}</option>
                                ))}
                              </select>
                            </td>
                            {governanceFields.map(({ field, label }) => (
                              <td key={field} className="px-2 py-2 text-center">
                                <input
                                  type="checkbox"
                                  aria-label={label}
                                  checked={row[field]}
                                  onChange={(e) => updateRow(index, { [field]: e.target.checked })}
                                />
                              </td>
                            ))}
                            <td className="px-2 py-2 text-center">
                              {!isLockedPrimary && companyRows.length > 1 && (
                                <button
                                  type="button"
                                  aria-label="Remover empresa"
                                  className="text-gray-400 hover:text-red-600"
                                  onClick={() => setCompanyRows((rows) => rows.filter((_, i) => i !== index))}
                                >
                                  <Trash2 className="w-4 h-4" />
                                </button>
                              )}
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
                <p className="text-xs text-gray-500">Passe o mouse sobre o título da coluna para ver a descrição da cláusula.</p>
              </div>
            )}

            </div>

            {/* Actions — always visible */}
            <div className="flex justify-end gap-3 p-6 pt-4 border-t flex-shrink-0">
              <Button type="button" variant="secondary" onClick={onClose}>
                Cancelar
              </Button>
              <Button
                type="submit"
                loading={isSubmitting || createMutation.isPending || updateMutation.isPending}
              >
                {isEditing ? 'Salvar' : 'Criar Sócio'}
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
