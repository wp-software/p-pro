import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Edit, Plus, Search, Trash2, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useShareClasses, useCreateShareClass, useUpdateShareClass, useDeleteShareClass } from '@/hooks/useShareClasses';
import { useClientStore } from '@/stores/clientStore';
import type { ShareClass } from '@/services/shareClassService';
import type { ShareClassRequest } from '@/services/shareClassService';
import { Button, Card, Input } from '@/components/ui';

const formSchema = z.object({
  name: z.string().min(1, 'Informe o nome da classe').max(100, 'Máximo de 100 caracteres'),
  code: z.string().min(1, 'Informe o código').max(20, 'Máximo de 20 caracteres').regex(/^[A-Za-z0-9]+$/, 'Use apenas letras e números'),
  description: z.string().max(500, 'Máximo de 500 caracteres').optional(),
  hasVotingRights: z.boolean(),
  votesPerShare: z.coerce.number().min(0, 'O valor não pode ser negativo'),
  liquidationPreference: z.coerce.number().min(0, 'O valor não pode ser negativo'),
  participating: z.boolean(),
  dividendPreference: z.union([z.literal(''), z.coerce.number().min(0).max(100)]),
  isConvertible: z.boolean(),
  convertsToClassId: z.string(),
  conversionRatio: z.union([z.literal(''), z.coerce.number().positive('A razão deve ser maior que zero')]),
  antiDilutionType: z.enum(['', 'None', 'FullRatchet', 'WeightedAverage']),
  rights: z.string().optional(),
  displayOrder: z.coerce.number().int().min(0, 'A ordem não pode ser negativa'),
}).superRefine((values, context) => {
  if (values.isConvertible && values.conversionRatio === '') {
    context.addIssue({
      code: z.ZodIssueCode.custom,
      path: ['conversionRatio'],
      message: 'Informe a razão de conversão',
    });
  }
});

type FormValues = z.infer<typeof formSchema>;

const emptyForm: FormValues = {
  name: '',
  code: '',
  description: '',
  hasVotingRights: true,
  votesPerShare: 1,
  liquidationPreference: 1,
  participating: false,
  dividendPreference: '',
  isConvertible: false,
  convertsToClassId: '',
  conversionRatio: '',
  antiDilutionType: '',
  rights: '',
  displayOrder: 0,
};

interface ShareClassFormProps {
  shareClass: ShareClass | null;
  classes: ShareClass[];
  onClose: () => void;
}

function ShareClassForm({ shareClass, classes, onClose }: ShareClassFormProps) {
  const { t } = useTranslation();
  const createShareClass = useCreateShareClass();
  const updateShareClass = useUpdateShareClass();
  const isEditing = !!shareClass;
  const {
    register,
    handleSubmit,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: shareClass
      ? {
          name: shareClass.name,
          code: shareClass.code,
          description: shareClass.description ?? '',
          hasVotingRights: shareClass.hasVotingRights,
          votesPerShare: shareClass.votesPerShare,
          liquidationPreference: shareClass.liquidationPreference,
          participating: shareClass.participating,
          dividendPreference: shareClass.dividendPreference ?? '',
          isConvertible: shareClass.isConvertible,
          convertsToClassId: shareClass.convertsToClassId ?? '',
          conversionRatio: shareClass.conversionRatio ?? '',
          antiDilutionType: shareClass.antiDilutionType ?? '',
          rights: shareClass.rights ?? '',
          displayOrder: shareClass.displayOrder,
        }
      : emptyForm,
  });
  const isConvertible = watch('isConvertible');

  const onSubmit = (values: FormValues) => {
    const request: ShareClassRequest = {
      ...values,
      description: values.description || undefined,
      dividendPreference: values.dividendPreference === '' ? null : values.dividendPreference,
      convertsToClassId: isConvertible && values.convertsToClassId ? values.convertsToClassId : null,
      conversionRatio: isConvertible && values.conversionRatio !== '' ? values.conversionRatio : null,
      antiDilutionType: values.antiDilutionType || null,
      rights: values.rights || undefined,
    };

    if (shareClass) {
      updateShareClass.mutate({ id: shareClass.id, data: request }, { onSuccess: onClose });
    } else {
      createShareClass.mutate(request, { onSuccess: onClose });
    }
  };

  const saving = isSubmitting || createShareClass.isPending || updateShareClass.isPending;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="fixed inset-0 bg-black/50" onClick={onClose} />
      <div className="relative flex min-h-full items-center justify-center p-4">
        <div className="relative w-full max-w-2xl rounded-xl bg-white shadow-xl">
          <div className="flex items-center justify-between border-b p-5">
            <h2 className="text-lg font-semibold text-gray-900">
              {t(isEditing ? 'shareClasses.editTitle' : 'shareClasses.createTitle')}
            </h2>
            <button type="button" onClick={onClose} aria-label={t('common.close')} className="text-gray-400 hover:text-gray-600">
              <X className="h-5 w-5" />
            </button>
          </div>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4 p-5">
            <div className="grid gap-4 sm:grid-cols-2">
              <Input label={t('shareClasses.name')} {...register('name')} error={errors.name?.message} />
              <Input label={t('shareClasses.code')} {...register('code')} error={errors.code?.message} />
            </div>
            <Input label={t('shareClasses.description')} {...register('description')} error={errors.description?.message} />

            <div className="grid gap-4 sm:grid-cols-2">
              <Input type="number" min="0" step="any" label={t('shareClasses.votesPerShare')} {...register('votesPerShare')} error={errors.votesPerShare?.message} />
              <Input type="number" min="0" step="any" label={t('shareClasses.liquidationPreference')} {...register('liquidationPreference')} error={errors.liquidationPreference?.message} />
              <Input type="number" min="0" max="100" step="any" label={t('shareClasses.dividendPreference')} {...register('dividendPreference')} error={errors.dividendPreference?.message} />
              <Input type="number" min="0" step="1" label={t('shareClasses.displayOrder')} {...register('displayOrder')} error={errors.displayOrder?.message} />
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input type="checkbox" {...register('hasVotingRights')} />
                {t('shareClasses.hasVotingRights')}
              </label>
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input type="checkbox" {...register('participating')} />
                {t('shareClasses.participating')}
              </label>
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input type="checkbox" {...register('isConvertible')} />
                {t('shareClasses.isConvertible')}
              </label>
            </div>

            {isConvertible && (
              <div className="grid gap-4 sm:grid-cols-2">
                <div>
                  <label className="input-label">{t('shareClasses.convertsTo')}</label>
                  <select className="input w-full" {...register('convertsToClassId')}>
                    <option value="">{t('shareClasses.noConversionTarget')}</option>
                    {classes.filter((item) =>
                      item.id !== shareClass?.id &&
                      (!shareClass || !item.companyId || item.companyId === shareClass.companyId)
                    ).map((item) => (
                      <option key={item.id} value={item.id}>{item.code} - {item.name}</option>
                    ))}
                  </select>
                </div>
                <Input type="number" min="0.01" step="any" label={t('shareClasses.conversionRatio')} {...register('conversionRatio')} error={errors.conversionRatio?.message} />
              </div>
            )}

            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <label className="input-label">{t('shareClasses.antiDilution')}</label>
                <select className="input w-full" {...register('antiDilutionType')}>
                  <option value="">{t('shareClasses.none')}</option>
                  <option value="FullRatchet">{t('shareClasses.fullRatchet')}</option>
                  <option value="WeightedAverage">{t('shareClasses.weightedAverage')}</option>
                </select>
              </div>
              <Input label={t('shareClasses.additionalRights')} {...register('rights')} />
            </div>

            <div className="flex justify-end gap-3 border-t pt-4">
              <Button type="button" variant="secondary" onClick={onClose}>{t('common.cancel')}</Button>
              <Button type="submit" loading={saving}>{t('common.save')}</Button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}

export default function ShareClassesPage() {
  const { t } = useTranslation();
  const { selectedCompanyId } = useClientStore();
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [formClass, setFormClass] = useState<ShareClass | null | undefined>(undefined);
  const { data, isLoading, isError, refetch } = useShareClasses(
    selectedCompanyId ?? undefined,
    page,
    10,
    search || undefined,
    status || undefined
  );
  const { data: classOptions } = useShareClasses(
    selectedCompanyId ?? undefined,
    1,
    100,
    undefined,
    undefined
  );
  const deleteShareClass = useDeleteShareClass();
  const totalPages = data?.totalPages ?? 1;

  return (
    <div className="space-y-6">
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{t('shareClasses.title')}</h1>
          <p className="mt-1 text-sm text-gray-500">{t('shareClasses.subtitle')}</p>
        </div>
        <Button
          icon={<Plus size={16} />}
          onClick={() => setFormClass(null)}
        >
          {t('shareClasses.new')}
        </Button>
      </div>

      <Card className="p-4">
        <div className="flex flex-col gap-3 sm:flex-row">
              <div className="relative flex-1">
                <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
                <Input
                  aria-label={t('shareClasses.search')}
                  placeholder={t('shareClasses.search')}
                  value={search}
                  onChange={(event) => { setSearch(event.target.value); setPage(1); }}
                  className="pl-9"
                />
              </div>
              <select
                aria-label={t('common.status')}
                className="input sm:w-44"
                value={status}
                onChange={(event) => { setStatus(event.target.value); setPage(1); }}
              >
                <option value="">{t('shareClasses.allStatuses')}</option>
                <option value="Active">{t('shareClasses.active')}</option>
                <option value="Inactive">{t('shareClasses.inactive')}</option>
              </select>
        </div>
      </Card>

      <Card>
            {isLoading ? (
              <div className="py-12 text-center text-gray-500">{t('common.loading')}</div>
            ) : isError ? (
              <div className="py-12 text-center">
                <p className="text-red-600">{t('shareClasses.loadError')}</p>
                <Button variant="secondary" size="sm" className="mt-3" onClick={() => refetch()}>{t('shareClasses.retry')}</Button>
              </div>
            ) : !data?.items.length ? (
              <div className="py-12 text-center text-gray-500">
                <p>{t('shareClasses.empty')}</p>
                <Button variant="secondary" size="sm" className="mt-4" onClick={() => setFormClass(null)}>
                  {t('shareClasses.createFirst')}
                </Button>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-gray-100 text-left text-xs uppercase tracking-wide text-gray-400">
                      <th className="px-4 py-3">{t('shareClasses.code')}</th>
                      <th className="px-4 py-3">{t('shareClasses.name')}</th>
                      <th className="px-4 py-3">{t('shareClasses.scope')}</th>
                      <th className="px-4 py-3">{t('shareClasses.voting')}</th>
                      <th className="px-4 py-3">{t('common.status')}</th>
                      <th className="px-4 py-3 text-right">{t('common.actions')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-50">
                    {data.items.map((item) => (
                      <tr key={item.id} className="hover:bg-gray-50">
                        <td className="px-4 py-3 font-medium text-gray-800">{item.code}</td>
                        <td className="px-4 py-3">
                          <p className="font-medium text-gray-800">{item.name}</p>
                          {item.description && <p className="mt-0.5 max-w-sm truncate text-xs text-gray-400">{item.description}</p>}
                        </td>
                        <td className="px-4 py-3 text-gray-600">
                          {item.companyName || t('shareClasses.allCompanies')}
                        </td>
                        <td className="px-4 py-3 text-gray-600">
                          {item.hasVotingRights ? `${item.votesPerShare} ${t('shareClasses.votesPerShareShort')}` : t('shareClasses.noVotingRights')}
                        </td>
                        <td className="px-4 py-3">
                          <span className={`rounded-full px-2 py-1 text-xs ${item.status === 'Active' ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-600'}`}>
                            {item.status === 'Active' ? t('shareClasses.active') : t('shareClasses.inactive')}
                          </span>
                        </td>
                        <td className="px-4 py-3 text-right">
                          <div className="flex justify-end gap-2">
                            <button
                              type="button"
                              aria-label={`${t('common.edit')} ${item.name}`}
                              onClick={() => setFormClass(item)}
                              className="rounded p-1.5 text-gray-500 hover:bg-blue-50 hover:text-blue-600"
                            >
                              <Edit size={16} />
                            </button>
                            <button
                              type="button"
                              aria-label={`${t('common.delete')} ${item.name}`}
                              disabled={deleteShareClass.isPending}
                              onClick={() => {
                                if (window.confirm(t('shareClasses.confirmDelete', { name: item.name }))) {
                                  deleteShareClass.mutate(item.id);
                                }
                              }}
                              className="rounded p-1.5 text-gray-500 hover:bg-red-50 hover:text-red-600 disabled:opacity-50"
                            >
                              <Trash2 size={16} />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
            {data && data.totalPages > 1 && (
              <div className="flex items-center justify-between border-t border-gray-100 px-4 py-3 text-sm">
                <span className="text-gray-500">{data.totalCount} {t('shareClasses.total')}</span>
                <div className="flex items-center gap-3">
                  <Button variant="secondary" size="sm" disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>
                    {t('common.previous')}
                  </Button>
                  <span className="text-gray-500">{page}/{totalPages}</span>
                  <Button variant="secondary" size="sm" disabled={page >= totalPages} onClick={() => setPage((current) => current + 1)}>
                    {t('common.next')}
                  </Button>
                </div>
              </div>
            )}
      </Card>

      {formClass !== undefined && (
        <ShareClassForm
          shareClass={formClass}
          classes={classOptions?.items ?? []}
          onClose={() => setFormClass(undefined)}
        />
      )}
    </div>
  );
}
