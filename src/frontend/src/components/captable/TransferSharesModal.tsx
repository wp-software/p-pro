import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { X, ArrowRightLeft, DollarSign, FileText } from 'lucide-react';
import { Button, Input, Card } from '@/components/ui';
import { ShareholderSelect } from '@/components/ui/ShareholderSelect';
import { useTransferShares } from '@/hooks/useCapTable';
import { useShareholders } from '@/hooks/useShareholders';
import { useShareClassesByCompany } from '@/hooks/useShareClasses';
import type { TransferSharesRequest } from '@/types';

const transferSharesSchema = z.object({
  fromShareholderId: z.string().min(1, 'Selecione o sócio de origem'),
  toShareholderId: z.string().min(1, 'Selecione o sócio de destino'),
  shareClassId: z.string().min(1, 'Selecione uma classe de ações'),
  quantity: z.coerce.number().min(1, 'Quantidade deve ser maior que 0'),
  pricePerShare: z.coerce.number().min(0, 'Preço deve ser maior ou igual a 0'),
  referenceDate: z.string().min(1, 'Data de referência é obrigatória'),
  transactionNumber: z.string().optional(),
  reason: z.string().optional(),
  documentReference: z.string().optional(),
  notes: z.string().optional(),
}).refine((data) => data.fromShareholderId !== data.toShareholderId, {
  message: 'Sócio de origem e destino devem ser diferentes',
  path: ['toShareholderId'],
});

type TransferSharesFormData = z.infer<typeof transferSharesSchema>;

interface TransferSharesModalProps {
  isOpen: boolean;
  onClose: () => void;
  companyId: string;
  onSuccess?: () => void;
}

export default function TransferSharesModal({
  isOpen,
  onClose,
  companyId,
  onSuccess,
}: TransferSharesModalProps) {
  const [showAdvanced, setShowAdvanced] = useState(false);

  const { data: shareholdersData, isLoading: loadingShareholders } = useShareholders({ 
    companyId, 
    pageSize: 100 
  });
  const { data: shareClassesData, isLoading: loadingClasses } = useShareClassesByCompany(companyId);

  const transferShares = useTransferShares();

  const {
    register,
    handleSubmit,
    watch,
    reset,
    setValue,
    formState: { errors },
  } = useForm<TransferSharesFormData>({
    resolver: zodResolver(transferSharesSchema),
    defaultValues: {
      fromShareholderId: '',
      toShareholderId: '',
      shareClassId: '',
      quantity: 0,
      pricePerShare: 0,
      referenceDate: new Date().toISOString().split('T')[0],
      transactionNumber: '',
      reason: '',
      documentReference: '',
      notes: '',
    },
  });

  const watchQuantity = watch('quantity');
  const watchPrice = watch('pricePerShare');
  const watchFromShareholder = watch('fromShareholderId');
  const watchToShareholder = watch('toShareholderId');
  const totalValue = (watchQuantity || 0) * (watchPrice || 0);

  const onSubmit = async (data: TransferSharesFormData) => {
    const request: TransferSharesRequest = {
      companyId,
      fromShareholderId: data.fromShareholderId,
      toShareholderId: data.toShareholderId,
      shareClassId: data.shareClassId,
      quantity: data.quantity,
      pricePerShare: data.pricePerShare,
      referenceDate: data.referenceDate,
      transactionNumber: data.transactionNumber || undefined,
      reason: data.reason || undefined,
      documentReference: data.documentReference || undefined,
      notes: data.notes || undefined,
    };

    transferShares.mutate(request, {
      onSuccess: () => {
        reset();
        onClose();
        onSuccess?.();
      },
    });
  };

  const handleClose = () => {
    reset();
    onClose();
  };

  if (!isOpen) return null;

  // Filtrar sócios para o destino (excluir o de origem)
  const availableDestinations = shareholdersData?.items?.filter(
    (s) => s.id !== watchFromShareholder
  ) ?? [];

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
              <div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
                <ArrowRightLeft className="w-5 h-5 text-blue-600" />
              </div>
              <div>
                <h2 className="text-lg font-semibold text-gray-900">Transferir Ações</h2>
                <p className="text-sm text-gray-500">Registrar transferência entre sócios</p>
              </div>
            </div>
            <button onClick={handleClose} className="text-gray-400 hover:text-gray-600">
              <X className="w-5 h-5" />
            </button>
          </div>

          {/* Body */}
          <form onSubmit={handleSubmit(onSubmit)} className="p-6 space-y-4">
            {/* Sócio de Origem */}
            <div>
              <label className="input-label">Sócio de Origem *</label>
              <input type="hidden" {...register('fromShareholderId')} />
              <ShareholderSelect
                shareholders={shareholdersData?.items ?? []}
                value={watchFromShareholder}
                onChange={(shareholderId) => {
                  setValue('fromShareholderId', shareholderId, {
                    shouldDirty: true,
                    shouldValidate: true,
                  });
                  if (shareholderId === watchToShareholder) {
                    setValue('toShareholderId', '', {
                      shouldDirty: true,
                      shouldValidate: true,
                    });
                  }
                }}
                disabled={loadingShareholders}
                placeholder="Pesquisar sócio de origem..."
              />
              {errors.fromShareholderId && (
                <p className="input-error-message">{errors.fromShareholderId.message}</p>
              )}
            </div>

            {/* Sócio de Destino */}
            <div>
              <label className="input-label">Sócio de Destino *</label>
              <input type="hidden" {...register('toShareholderId')} />
              <ShareholderSelect
                shareholders={availableDestinations}
                value={watchToShareholder}
                onChange={(shareholderId) =>
                  setValue('toShareholderId', shareholderId, {
                    shouldDirty: true,
                    shouldValidate: true,
                  })
                }
                disabled={loadingShareholders || !watchFromShareholder}
                placeholder="Pesquisar sócio de destino..."
              />
              {errors.toShareholderId && (
                <p className="input-error-message">{errors.toShareholderId.message}</p>
              )}
            </div>

            {/* Classe de Ações */}
            <div>
              <label className="input-label">Classe de Ações *</label>
              <select
                {...register('shareClassId')}
                className="input"
                disabled={loadingClasses}
              >
                <option value="">Selecione uma classe</option>
                {shareClassesData?.map((shareClass) => (
                  <option key={shareClass.id} value={shareClass.id}>
                    {shareClass.code} - {shareClass.name}
                  </option>
                ))}
              </select>
              {errors.shareClassId && (
                <p className="input-error-message">{errors.shareClassId.message}</p>
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
                  <span className="text-sm">Valor Total da Transferência</span>
                </div>
                <span className="text-lg font-semibold text-gray-900">
                  {totalValue.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
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

            {/* Toggle Campos Avançados */}
            <button
              type="button"
              onClick={() => setShowAdvanced(!showAdvanced)}
              className="flex items-center gap-2 text-sm text-primary hover:text-primary/80"
            >
              <FileText className="w-4 h-4" />
              {showAdvanced ? 'Ocultar campos adicionais' : 'Mostrar campos adicionais'}
            </button>

            {/* Campos Avançados */}
            {showAdvanced && (
              <div className="space-y-4 pt-2 border-t border-gray-200">
                <Input
                  label="Nº da Transação"
                  {...register('transactionNumber')}
                  error={errors.transactionNumber?.message}
                />
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

            {/* Actions */}
            <div className="flex justify-end gap-3 pt-4 border-t">
              <Button type="button" variant="secondary" onClick={handleClose}>
                Cancelar
              </Button>
              <Button
                type="submit"
                variant="primary"
                loading={transferShares.isPending}
                icon={<ArrowRightLeft className="w-4 h-4" />}
              >
                Transferir Ações
              </Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
