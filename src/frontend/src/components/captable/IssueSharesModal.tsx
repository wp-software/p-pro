import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { X, Plus, DollarSign } from 'lucide-react';
import { Button, Input, Card } from '@/components/ui';
import { ShareholderSelect } from '@/components/ui/ShareholderSelect';
import { useIssueShares } from '@/hooks/useCapTable';
import { useShareholders } from '@/hooks/useShareholders';
import { useShareClassesByCompany } from '@/hooks/useShareClasses';
import type { IssueSharesRequest } from '@/types';

const issueSharesSchema = z.object({
  shareholderId: z.string().min(1, 'Selecione um sócio'),
  shareClassId: z.string().min(1, 'Selecione uma classe de ações'),
  quantity: z.coerce.number().min(1, 'Quantidade deve ser maior que 0'),
  pricePerShare: z.coerce.number().min(0, 'Preço deve ser maior ou igual a 0'),
  referenceDate: z.string().min(1, 'Data de referência é obrigatória'),
  certificateNumber: z.string().optional(),
  transactionNumber: z.string().optional(),
  reason: z.string().optional(),
  documentReference: z.string().optional(),
  notes: z.string().optional(),
});

type IssueSharesFormData = z.infer<typeof issueSharesSchema>;

interface IssueSharesModalProps {
  isOpen: boolean;
  onClose: () => void;
  companyId: string;
  onSuccess?: () => void;
}

export default function IssueSharesModal({
  isOpen,
  onClose,
  companyId,
  onSuccess,
}: IssueSharesModalProps) {
  const [activeTab, setActiveTab] = useState<'issue' | 'additional'>('issue');

  const { data: shareholdersData, isLoading: loadingShareholders } = useShareholders({ 
    companyId, 
    pageSize: 100 
  });
  const { data: shareClassesData, isLoading: loadingClasses } = useShareClassesByCompany(companyId);

  const issueShares = useIssueShares();

  const {
    register,
    handleSubmit,
    watch,
    reset,
    setValue,
    formState: { errors },
  } = useForm<IssueSharesFormData>({
    resolver: zodResolver(issueSharesSchema),
    defaultValues: {
      shareholderId: '',
      shareClassId: '',
      quantity: 0,
      pricePerShare: 0,
      referenceDate: new Date().toISOString().split('T')[0],
      certificateNumber: '',
      transactionNumber: '',
      reason: '',
      documentReference: '',
      notes: '',
    },
  });

  const watchQuantity = watch('quantity');
  const watchPrice = watch('pricePerShare');
  const selectedShareholderId = watch('shareholderId');
  const totalValue = (watchQuantity || 0) * (watchPrice || 0);

  const onSubmit = async (data: IssueSharesFormData) => {
    const request: IssueSharesRequest = {
      companyId,
      shareholderId: data.shareholderId,
      shareClassId: data.shareClassId,
      quantity: data.quantity,
      pricePerShare: data.pricePerShare,
      referenceDate: data.referenceDate,
      certificateNumber: data.certificateNumber || undefined,
      transactionNumber: data.transactionNumber || undefined,
      reason: data.reason || undefined,
      documentReference: data.documentReference || undefined,
      notes: data.notes || undefined,
    };

    issueShares.mutate(request, {
      onSuccess: () => {
        reset();
        setActiveTab('issue');
        onClose();
        onSuccess?.();
      },
    });
  };

  const handleClose = () => {
    reset();
    setActiveTab('issue');
    onClose();
  };

  const handleInvalidSubmit = (formErrors: typeof errors) => {
    if (
      formErrors.shareholderId ||
      formErrors.shareClassId ||
      formErrors.quantity ||
      formErrors.pricePerShare ||
      formErrors.referenceDate
    ) {
      setActiveTab('issue');
    }
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex min-h-screen items-center justify-center p-4">
        {/* Backdrop */}
        <div className="fixed inset-0 bg-black/50 transition-opacity" onClick={handleClose} />

        {/* Modal */}
        <div className="relative w-full max-w-lg bg-white rounded-xl shadow-xl">
          {/* Header */}
          <div className="flex items-center justify-between p-6 border-b">
            <div className="flex items-center gap-3">
              <div className="w-10 h-10 bg-green-100 rounded-lg flex items-center justify-center">
                <Plus className="w-5 h-5 text-green-600" />
              </div>
              <div>
                <h2 className="text-lg font-semibold text-gray-900">Emitir Ações</h2>
                <p className="text-sm text-gray-500">Registrar nova emissão de ações</p>
              </div>
            </div>
            <button onClick={handleClose} className="text-gray-400 hover:text-gray-600">
              <X className="w-5 h-5" />
            </button>
          </div>

          {/* Body */}
          <form
            onSubmit={handleSubmit(onSubmit, handleInvalidSubmit)}
            className="flex max-h-[calc(100vh-10rem)] flex-col"
          >
            <nav className="flex border-b px-6" role="tablist" aria-label="Seções da emissão de ações">
              {([
                { id: 'issue', label: 'Dados da emissão' },
                { id: 'additional', label: 'Dados adicionais' },
              ] as const).map((tab) => (
                <button
                  key={tab.id}
                  type="button"
                  role="tab"
                  id={`issue-shares-tab-${tab.id}`}
                  aria-controls={`issue-shares-panel-${tab.id}`}
                  aria-selected={activeTab === tab.id}
                  onClick={() => setActiveTab(tab.id)}
                  className={`border-b-2 px-5 py-3 text-sm font-medium transition-colors ${
                    activeTab === tab.id
                      ? 'border-primary-600 text-primary-700'
                      : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'
                  }`}
                >
                  {tab.label}
                </button>
              ))}
            </nav>

            <div className="flex-1 overflow-y-auto p-6">
              {activeTab === 'issue' && (
                <div
                  className="space-y-4"
                  role="tabpanel"
                  id="issue-shares-panel-issue"
                  aria-labelledby="issue-shares-tab-issue"
                >
                  {/* Sócio */}
                  <div>
                    <label className="input-label">Sócio *</label>
                    <input type="hidden" {...register('shareholderId')} />
                    <ShareholderSelect
                      shareholders={shareholdersData?.items ?? []}
                      value={selectedShareholderId}
                      onChange={(shareholderId) =>
                        setValue('shareholderId', shareholderId, {
                          shouldDirty: true,
                          shouldValidate: true,
                        })
                      }
                      disabled={loadingShareholders}
                      placeholder="Pesquisar sócio por nome ou email..."
                    />
                    {errors.shareholderId && (
                      <p className="input-error-message">{errors.shareholderId.message}</p>
                    )}
                  </div>

                  {/* Classe de Ações */}
                  <div>
                    <label className="input-label">Classe de Ações *</label>
                    <select
                      {...register('shareClassId')}
                      className="input"
                      disabled={loadingClasses || !shareClassesData?.length}
                    >
                      <option value="">
                        {loadingClasses
                          ? 'Carregando...'
                          : !shareClassesData?.length
                          ? 'Nenhuma classe de ações cadastrada'
                          : 'Selecione uma classe'}
                      </option>
                      {shareClassesData?.map((shareClass) => (
                        <option key={shareClass.id} value={shareClass.id}>
                          {shareClass.code} - {shareClass.name}
                        </option>
                      ))}
                    </select>
                    {errors.shareClassId && (
                      <p className="input-error-message">{errors.shareClassId.message}</p>
                    )}
                    {!loadingClasses && !shareClassesData?.length && (
                      <p className="text-xs text-amber-600 mt-1">
                        Cadastre as classes de ações da empresa antes de emitir.{' '}
                        <a
                          href="/share-classes"
                          target="_blank"
                          rel="noreferrer"
                          className="font-medium underline"
                        >
                          Criar classes de ações
                        </a>
                      </p>
                    )}
                  </div>

                  {/* Quantidade e Preço */}
                  <div className="grid grid-cols-2 gap-4">
                    <Input
                      label="Quantidade *"
                      type="number"
                      step="1"
                      min="1"
                      {...register('quantity')}
                      error={errors.quantity?.message}
                    />
                    <Input
                      label="Preço por Ação (R$) *"
                      type="number"
                      step="0.01"
                      min="0"
                      {...register('pricePerShare')}
                      error={errors.pricePerShare?.message}
                    />
                  </div>

                  {/* Valor Total (calculado) */}
                  <Card className="p-4 bg-gray-50">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2 text-gray-600">
                        <DollarSign className="w-4 h-4" />
                        <span className="text-sm">Valor Total</span>
                      </div>
                      <span className="text-lg font-semibold text-gray-900">
                        {totalValue.toLocaleString('pt-BR', {
                          style: 'currency',
                          currency: 'BRL',
                        })}
                      </span>
                    </div>
                  </Card>

                  {/* Data de Referência */}
                  <Input
                    label="Data de Referência *"
                    type="date"
                    {...register('referenceDate')}
                    error={errors.referenceDate?.message}
                  />
                </div>
              )}

              {activeTab === 'additional' && (
                <div
                  className="space-y-4"
                  role="tabpanel"
                  id="issue-shares-panel-additional"
                  aria-labelledby="issue-shares-tab-additional"
                >
                  <div className="grid grid-cols-2 gap-4">
                    <Input
                      label="Nº do Certificado"
                      {...register('certificateNumber')}
                      error={errors.certificateNumber?.message}
                    />
                    <Input
                      label="Nº da Transação"
                      {...register('transactionNumber')}
                      error={errors.transactionNumber?.message}
                    />
                  </div>
                  <Input
                    label="Motivo"
                    {...register('reason')}
                    error={errors.reason?.message}
                  />
                  <Input
                    label="Referência de Documento"
                    {...register('documentReference')}
                    error={errors.documentReference?.message}
                  />
                  <div>
                    <label className="input-label">Observações</label>
                    <textarea
                      {...register('notes')}
                      className="input min-h-[80px]"
                      placeholder="Observações adicionais..."
                    />
                  </div>
                </div>
              )}
            </div>

            {/* Actions */}
            <div className="flex justify-end gap-3 border-t px-6 py-4">
              <Button type="button" variant="secondary" onClick={handleClose}>
                Cancelar
              </Button>
              <Button
                type="submit"
                variant="success"
                loading={issueShares.isPending}
                icon={<Plus className="w-4 h-4" />}
              >
                Emitir Ações
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
