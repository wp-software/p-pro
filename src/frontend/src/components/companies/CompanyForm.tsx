import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { X, Search, Loader2 } from 'lucide-react';
import { useState } from 'react';
import { Button, Input } from '@/components/ui';
import { useCreateCompany, useUpdateCompany } from '@/hooks/useCompanies';
import { LegalForm, LegalFormLabels } from '@/types/company.types';
import type { Company } from '@/types';

const legalFormOptions = Object.entries(LegalFormLabels).map(([value, label]) => ({
  value: Number(value),
  label,
}));

// Schema de validação para CNPJ (formato: XX.XXX.XXX/XXXX-XX ou apenas números)
const cnpjRegex = /^(\d{2}\.?\d{3}\.?\d{3}\/?\d{4}-?\d{2})$/;
const cepRegex = /^(\d{5}-?\d{3})$/;

const addressSchema = {
  cep: z.string().regex(cepRegex, 'CEP inválido').optional().or(z.literal('')),
  street: z.string().max(255, 'Máximo 255 caracteres').optional().or(z.literal('')),
  number: z.string().max(20, 'Máximo 20 caracteres').optional().or(z.literal('')),
  complement: z.string().max(100, 'Máximo 100 caracteres').optional().or(z.literal('')),
  neighborhood: z.string().max(100, 'Máximo 100 caracteres').optional().or(z.literal('')),
  city: z.string().max(100, 'Máximo 100 caracteres').optional().or(z.literal('')),
  state: z.string().max(2, 'Máximo 2 caracteres').optional().or(z.literal('')),
};

const createCompanySchema = z.object({
  name: z.string().min(1, 'Nome é obrigatório').max(200, 'Máximo 200 caracteres'),
  tradingName: z.string().max(200, 'Máximo 200 caracteres').optional().or(z.literal('')),
  cnpj: z.string().regex(cnpjRegex, 'CNPJ inválido'),
  legalForm: z.coerce.number().min(1, 'Forma jurídica é obrigatória'),
  foundationDate: z.string().min(1, 'Data de fundação é obrigatória'),
  totalShares: z.coerce.number().min(1, 'Total de ações deve ser maior que 0'),
  sharePrice: z.coerce.number().min(0.01, 'Preço por ação deve ser maior que 0'),
  currency: z.string().default('BRL'),
  ...addressSchema,
});

const updateCompanySchema = z.object({
  name: z.string().min(1, 'Nome é obrigatório').max(200, 'Máximo 200 caracteres'),
  tradingName: z.string().max(200, 'Máximo 200 caracteres').optional().or(z.literal('')),
  logoUrl: z.string().url('URL inválida').optional().or(z.literal('')),
  legalForm: z.coerce.number().min(1, 'Forma jurídica é obrigatória'),
  foundationDate: z.string().min(1, 'Data de fundação é obrigatória'),
  totalShares: z.coerce.number().min(1, 'Total de ações deve ser maior que 0'),
  sharePrice: z.coerce.number().min(0.01, 'Preço por ação deve ser maior que 0'),
  currency: z.string().default('BRL'),
  ...addressSchema,
});

type CreateFormData = z.infer<typeof createCompanySchema>;
type UpdateFormData = z.infer<typeof updateCompanySchema>;
type FormData = CreateFormData | UpdateFormData;

function parseLocalizedNumber(value: string): number {
  const normalized = value.replace(/[^\d,.-]/g, '');
  if (!normalized) return Number.NaN;

  const commaIndex = normalized.lastIndexOf(',');
  const dotIndex = normalized.lastIndexOf('.');
  if (commaIndex >= 0) {
    return Number(normalized.replace(/\./g, '').replace(',', '.'));
  }
  if (dotIndex >= 0) {
    const decimalDigits = normalized.length - dotIndex - 1;
    return Number(decimalDigits === 3 ? normalized.replace(/\./g, '') : normalized);
  }
  return Number(normalized);
}

function formatCurrencyInput(value: number, currency: string): string {
  if (!Number.isFinite(value)) return '';
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(value);
}

function formatNumberInput(value: number): string {
  if (!Number.isFinite(value)) return '';
  return new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 2 }).format(value);
}

function formatUngroupedNumber(value: number): string {
  return new Intl.NumberFormat('pt-BR', {
    useGrouping: false,
    maximumFractionDigits: 2,
  }).format(value);
}

interface ViaCepResponse {
  cep: string;
  logradouro: string;
  complemento: string;
  bairro: string;
  localidade: string;
  uf: string;
  erro?: boolean;
}

interface CompanyFormProps {
  company?: Company | null;
  onClose: () => void;
}

export function CompanyForm({ company, onClose }: CompanyFormProps) {
  const isEditing = !!company;
  const createCompany = useCreateCompany();
  const updateCompany = useUpdateCompany();
  const [isSearchingCep, setIsSearchingCep] = useState(false);
  const [isEditingSharePrice, setIsEditingSharePrice] = useState(false);
  const [sharePriceInput, setSharePriceInput] = useState('');
  const [isEditingTotalShares, setIsEditingTotalShares] = useState(false);
  const [totalSharesInput, setTotalSharesInput] = useState('');

  const getLegalFormValue = (legalFormString: string): number => {
    const entry = Object.entries(LegalFormLabels).find(
      ([, label]) => label.includes(legalFormString) || legalFormString.includes(label.split(' ')[0])
    );
    if (entry) return Number(entry[0]);
    
    // Try matching by enum key
    const enumKey = Object.keys(LegalForm).find(
      (key) => key.toLowerCase() === legalFormString.toLowerCase()
    );
    if (enumKey) return LegalForm[enumKey as keyof typeof LegalForm];
    
    return LegalForm.LTDA;
  };

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    getValues,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({
    resolver: zodResolver(isEditing ? updateCompanySchema : createCompanySchema),
    defaultValues: isEditing
      ? {
          name: company.name,
          tradingName: company.tradingName || '',
          logoUrl: company.logoUrl || '',
          legalForm: getLegalFormValue(company.legalForm),
          foundationDate: company.foundationDate?.split('T')[0] || '',
          totalShares: company.totalShares,
          sharePrice: company.sharePrice,
          currency: company.currency || 'BRL',
          cep: company.cep || '',
          street: company.street || '',
          number: company.number || '',
          complement: company.complement || '',
          neighborhood: company.neighborhood || '',
          city: company.city || '',
          state: company.state || '',
        }
      : {
          name: '',
          tradingName: '',
          cnpj: '',
          legalForm: LegalForm.LTDA,
          foundationDate: new Date().toISOString().split('T')[0],
          totalShares: 1000000,
          sharePrice: 1,
          currency: 'BRL',
          cep: '',
          street: '',
          number: '',
          complement: '',
          neighborhood: '',
          city: '',
          state: '',
        },
  });

  const cepValue = watch('cep');
  const currencyValue = watch('currency') || 'BRL';
  const sharePriceValue = watch('sharePrice');
  const totalSharesValue = watch('totalShares');
  const sharePriceField = register('sharePrice');
  const totalSharesField = register('totalShares');

  const searchCep = async () => {
    const cep = cepValue?.replace(/\D/g, '');
    if (!cep || cep.length !== 8) return;

    setIsSearchingCep(true);
    try {
      const response = await fetch(`https://viacep.com.br/ws/${cep}/json/`);
      const data: ViaCepResponse = await response.json();

      if (!data.erro) {
        setValue('street', data.logradouro || '');
        setValue('neighborhood', data.bairro || '');
        setValue('city', data.localidade || '');
        setValue('state', data.uf || '');
        if (data.complemento) {
          setValue('complement', data.complemento);
        }
      }
    } catch (error) {
      console.error('Erro ao buscar CEP:', error);
    } finally {
      setIsSearchingCep(false);
    }
  };

  const handleCepBlur = () => {
    const cep = cepValue?.replace(/\D/g, '');
    if (cep && cep.length === 8) {
      searchCep();
    }
  };

  const onSubmit = async (data: FormData) => {
    try {
      if (isEditing) {
        await updateCompany.mutateAsync({
          id: company.id,
          data: {
            name: data.name,
            tradingName: data.tradingName,
            logoUrl: (data as UpdateFormData).logoUrl,
            totalShares: data.totalShares,
            sharePrice: data.sharePrice,
            currency: data.currency,
            cep: data.cep,
            street: data.street,
            number: data.number,
            complement: data.complement,
            neighborhood: data.neighborhood,
            city: data.city,
            state: data.state,
          },
        });
      } else {
        await createCompany.mutateAsync(data as CreateFormData);
      }
      onClose();
    } catch {
      // Erro tratado no hook
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-2xl mx-4 max-h-[90vh] overflow-y-auto">
        {/* Header */}
        <div className="flex items-center justify-between p-6 border-b sticky top-0 bg-white z-10">
          <h2 className="text-xl font-semibold text-primary">
            {isEditing ? 'Editar Empresa' : 'Nova Empresa'}
          </h2>
          <button
            onClick={onClose}
            className="p-2 hover:bg-primary-50 rounded-lg transition-colors"
          >
            <X className="w-5 h-5 text-primary-500" />
          </button>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit(onSubmit)} className="p-6 space-y-6">
          {/* Dados Básicos */}
          <div>
            <h3 className="text-sm font-semibold text-primary-600 uppercase tracking-wider mb-4">
              Dados Básicos
            </h3>
            <div className="space-y-4">
              <Input
                label="Razão Social"
                {...register('name')}
                error={errors.name?.message}
                placeholder="Nome da empresa"
              />

              <Input
                label="Nome Fantasia"
                {...register('tradingName')}
                error={errors.tradingName?.message}
                placeholder="Nome fantasia (opcional)"
              />

              {!isEditing && (
                <Input
                  label="CNPJ"
                  {...register('cnpj')}
                  error={(errors as any).cnpj?.message}
                  placeholder="00.000.000/0000-00"
                />
              )}

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="w-full">
                  <label className="input-label">Forma Jurídica</label>
                  <select {...register('legalForm')} className="input">
                    {legalFormOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                  {(errors as any).legalForm && (
                    <p className="input-error-message">{(errors as any).legalForm?.message}</p>
                  )}
                </div>

                <Input
                  label="Data de Fundação"
                  type="date"
                  {...register('foundationDate')}
                  error={(errors as any).foundationDate?.message}
                />
              </div>

              {isEditing && (
                <Input
                  label="URL do Logo"
                  {...register('logoUrl')}
                  error={(errors as any).logoUrl?.message}
                  placeholder="https://exemplo.com/logo.png"
                />
              )}
            </div>
          </div>

          {/* Informações Societárias */}
          <div>
            <h3 className="text-sm font-semibold text-primary-600 uppercase tracking-wider mb-4">
              Informações Societárias
            </h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Input
                label="Total de Ações"
                type="text"
                inputMode="decimal"
                {...totalSharesField}
                value={
                  isEditingTotalShares
                    ? totalSharesInput
                    : formatNumberInput(Number(totalSharesValue))
                }
                onFocus={() => {
                  setIsEditingTotalShares(true);
                  setTotalSharesInput(formatUngroupedNumber(Number(getValues('totalShares'))));
                }}
                onChange={(event) => {
                  setTotalSharesInput(event.target.value);
                  setValue('totalShares', parseLocalizedNumber(event.target.value), {
                    shouldValidate: true,
                  });
                }}
                onBlur={(event) => {
                  totalSharesField.onBlur(event);
                  setIsEditingTotalShares(false);
                }}
                error={(errors as any).totalShares?.message}
                placeholder="1000000"
              />

              <Input
                label="Preço por Ação"
                type="text"
                inputMode="decimal"
                {...sharePriceField}
                value={
                  isEditingSharePrice
                    ? sharePriceInput
                    : formatCurrencyInput(Number(sharePriceValue), currencyValue)
                }
                onFocus={() => {
                  setIsEditingSharePrice(true);
                  setSharePriceInput(formatUngroupedNumber(Number(getValues('sharePrice'))));
                }}
                onChange={(event) => {
                  setSharePriceInput(event.target.value);
                  setValue('sharePrice', parseLocalizedNumber(event.target.value), {
                    shouldValidate: true,
                  });
                }}
                onBlur={(event) => {
                  sharePriceField.onBlur(event);
                  setIsEditingSharePrice(false);
                }}
                error={(errors as any).sharePrice?.message}
                placeholder="1.00"
              />

              <div className="w-full">
                <label className="input-label">Moeda</label>
                <select {...register('currency')} className="input">
                  <option value="BRL">BRL - Real Brasileiro</option>
                  <option value="USD">USD - Dólar Americano</option>
                  <option value="EUR">EUR - Euro</option>
                </select>
              </div>
            </div>
          </div>

          {/* Endereço */}
          <div>
            <h3 className="text-sm font-semibold text-primary-600 uppercase tracking-wider mb-4">
              Endereço
            </h3>
            <div className="space-y-4">
              <div className="flex gap-2">
                <div className="flex-1">
                  <Input
                    label="CEP"
                    {...register('cep')}
                    error={(errors as any).cep?.message}
                    placeholder="00000-000"
                    onBlur={handleCepBlur}
                  />
                </div>
                <div className="flex items-end">
                  <Button
                    type="button"
                    variant="secondary"
                    onClick={searchCep}
                    disabled={isSearchingCep}
                    className="h-[42px]"
                  >
                    {isSearchingCep ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <Search className="w-4 h-4" />
                    )}
                  </Button>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="md:col-span-2">
                  <Input
                    label="Logradouro"
                    {...register('street')}
                    error={(errors as any).street?.message}
                    placeholder="Rua, Avenida, etc."
                  />
                </div>
                <Input
                  label="Número"
                  {...register('number')}
                  error={(errors as any).number?.message}
                  placeholder="123"
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <Input
                  label="Complemento"
                  {...register('complement')}
                  error={(errors as any).complement?.message}
                  placeholder="Sala, Andar, etc."
                />
                <Input
                  label="Bairro"
                  {...register('neighborhood')}
                  error={(errors as any).neighborhood?.message}
                  placeholder="Bairro"
                />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="md:col-span-2">
                  <Input
                    label="Cidade"
                    {...register('city')}
                    error={(errors as any).city?.message}
                    placeholder="Cidade"
                  />
                </div>
                <div className="w-full">
                  <label className="input-label">Estado</label>
                  <select {...register('state')} className="input">
                    <option value="">Selecione</option>
                    <option value="AC">AC</option>
                    <option value="AL">AL</option>
                    <option value="AP">AP</option>
                    <option value="AM">AM</option>
                    <option value="BA">BA</option>
                    <option value="CE">CE</option>
                    <option value="DF">DF</option>
                    <option value="ES">ES</option>
                    <option value="GO">GO</option>
                    <option value="MA">MA</option>
                    <option value="MT">MT</option>
                    <option value="MS">MS</option>
                    <option value="MG">MG</option>
                    <option value="PA">PA</option>
                    <option value="PB">PB</option>
                    <option value="PR">PR</option>
                    <option value="PE">PE</option>
                    <option value="PI">PI</option>
                    <option value="RJ">RJ</option>
                    <option value="RN">RN</option>
                    <option value="RS">RS</option>
                    <option value="RO">RO</option>
                    <option value="RR">RR</option>
                    <option value="SC">SC</option>
                    <option value="SP">SP</option>
                    <option value="SE">SE</option>
                    <option value="TO">TO</option>
                  </select>
                </div>
              </div>
            </div>
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-3 pt-4 border-t">
            <Button type="button" variant="secondary" onClick={onClose}>
              Cancelar
            </Button>
            <Button type="submit" loading={isSubmitting}>
              {isEditing ? 'Salvar Alterações' : 'Criar Empresa'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
