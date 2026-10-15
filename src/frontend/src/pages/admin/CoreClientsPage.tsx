import { FormEvent, useState } from 'react';
import { Building2, Check, Plus, Search, Shield, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Badge, Button, Card, Input } from '@/components/ui';
import {
  useActivateClient,
  useClients,
  useDeactivateClient,
  useOnboardClient,
  useResendAdministratorActivation,
  useSuspendClient,
} from '@/hooks/useClient';
import {
  DocumentType,
  type ClientOnboardingResponse,
  type OnboardClientRequest,
} from '@/types';

const legalForms = [
  { value: 1, label: 'LTDA' },
  { value: 2, label: 'S.A.' },
  { value: 3, label: 'EIRELI' },
  { value: 4, label: 'MEI' },
  { value: 5, label: 'SLU' },
  { value: 99, label: 'Outra' },
];

const emptyForm: OnboardClientRequest = {
  client: {
    name: '',
    tradingName: '',
    document: '',
    documentType: DocumentType.Cnpj,
    email: '',
    phone: '',
  },
  company: {
    name: '',
    tradingName: '',
    cnpj: '',
    legalForm: 1,
    foundationDate: '',
    totalShares: 1,
    sharePrice: 1,
    currency: 'BRL',
  },
  administratorName: '',
  administratorEmail: '',
};

function statusVariant(status: string): 'active' | 'inactive' | 'pending' {
  if (status === 'Active') return 'active';
  if (status === 'Suspended') return 'pending';
  return 'inactive';
}

export default function CoreClientsPage() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<OnboardClientRequest>(emptyForm);
  const [formError, setFormError] = useState('');
  const [lastOnboarding, setLastOnboarding] =
    useState<ClientOnboardingResponse | null>(null);
  const { data, isLoading, error } = useClients({ page, pageSize: 10, search });
  const onboardClient = useOnboardClient();
  const resendActivation = useResendAdministratorActivation();
  const activateClient = useActivateClient();
  const suspendClient = useSuspendClient();
  const deactivateClient = useDeactivateClient();

  const updateClient = (
    field: keyof OnboardClientRequest['client'],
    value: string | number,
  ) => {
    setForm((current) => ({
      ...current,
      client: { ...current.client, [field]: value },
    }));
  };

  const updateCompany = (
    field: keyof OnboardClientRequest['company'],
    value: string | number,
  ) => {
    setForm((current) => ({
      ...current,
      company: { ...current.company, [field]: value },
    }));
  };

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setFormError('');
    try {
      const result = await onboardClient.mutateAsync(form);
      setLastOnboarding(result);
      setForm(emptyForm);
      setShowForm(false);
    } catch (submitError) {
      const responseMessage = (
        submitError as { response?: { data?: { message?: string } } }
      ).response?.data?.message;
      setFormError(responseMessage || t('coreClientOnboarding.saveError'));
    }
  };

  const changeStatus = (status: string, clientId: string) => {
    if (status === 'Active') {
      suspendClient.mutate(clientId);
    } else {
      activateClient.mutate(clientId);
    }
  };

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-primary">
            {t('coreClientOnboarding.title')}
          </h1>
          <p className="mt-1 text-primary-500">
            {t('coreClientOnboarding.subtitle')}
          </p>
        </div>
        <Button
          onClick={() => {
            setFormError('');
            setShowForm((visible) => !visible);
          }}
          icon={
            showForm ? <X className="h-4 w-4" /> : <Plus className="h-4 w-4" />
          }
        >
          {showForm ? t('common.cancel') : t('coreClientOnboarding.newClient')}
        </Button>
      </header>

      {formError && (
        <div
          role="alert"
          className="border-l-4 border-amber-500 bg-amber-50 px-4 py-3 text-sm text-amber-900"
        >
          {formError}
        </div>
      )}

      {lastOnboarding && (
        <div
          className={`flex flex-wrap items-center justify-between gap-3 border-l-4 px-4 py-3 text-sm ${lastOnboarding.activationEmailSent ? 'border-emerald-600 bg-emerald-50 text-emerald-900' : 'border-amber-500 bg-amber-50 text-amber-900'}`}
        >
          <p>
            {t(
              lastOnboarding.activationEmailSent
                ? 'coreClientOnboarding.inviteSent'
                : 'coreClientOnboarding.inviteFailed',
            )}
          </p>
          {!lastOnboarding.activationEmailSent && (
            <Button
              variant="secondary"
              size="sm"
              loading={resendActivation.isPending}
              onClick={() =>
                resendActivation.mutate(lastOnboarding.administratorId, {
                  onSuccess: () =>
                    setLastOnboarding({
                      ...lastOnboarding,
                      activationEmailSent: true,
                    }),
                })
              }
            >
              {t('coreClientOnboarding.resendInvite')}
            </Button>
          )}
        </div>
      )}

      {showForm && (
        <form
          onSubmit={handleSubmit}
          className="space-y-6 border-y border-primary-100 py-6"
        >
          <section className="space-y-4">
            <h2 className="text-lg font-semibold text-primary">
              {t('coreClientOnboarding.client')}
            </h2>
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              <Input
                required
                label={t('coreClientOnboarding.clientName')}
                value={form.client.name}
                onChange={(event) => updateClient('name', event.target.value)}
              />
              <Input
                label={t('coreClientOnboarding.tradingName')}
                value={form.client.tradingName}
                onChange={(event) =>
                  updateClient('tradingName', event.target.value)
                }
              />
              <div className="grid grid-cols-[minmax(0,1fr)_120px] gap-2">
                <Input
                  required
                  label={t('coreClientOnboarding.document')}
                  value={form.client.document}
                  onChange={(event) =>
                    updateClient('document', event.target.value)
                  }
                />
                <label className="input-label">
                  {t('coreClientOnboarding.documentType')}
                  <select
                    className="input mt-1"
                    value={form.client.documentType}
                    onChange={(event) =>
                      updateClient('documentType', Number(event.target.value))
                    }
                  >
                    <option value={DocumentType.Cnpj}>CNPJ</option>
                    <option value={DocumentType.Cpf}>CPF</option>
                  </select>
                </label>
              </div>
              <Input
                required
                type="email"
                label={t('coreClientOnboarding.email')}
                value={form.client.email}
                onChange={(event) => updateClient('email', event.target.value)}
              />
              <Input
                label={t('coreClientOnboarding.phone')}
                value={form.client.phone}
                onChange={(event) => updateClient('phone', event.target.value)}
              />
            </div>
          </section>

          <section className="space-y-4 border-t border-primary-100 pt-5">
            <h2 className="text-lg font-semibold text-primary">
              {t('coreClientOnboarding.company')}
            </h2>
            <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
              <Input
                required
                label={t('coreClientOnboarding.companyName')}
                value={form.company.name}
                onChange={(event) => updateCompany('name', event.target.value)}
              />
              <Input
                label={t('coreClientOnboarding.tradingName')}
                value={form.company.tradingName}
                onChange={(event) =>
                  updateCompany('tradingName', event.target.value)
                }
              />
              <Input
                required
                label={t('coreClientOnboarding.cnpj')}
                value={form.company.cnpj}
                onChange={(event) => updateCompany('cnpj', event.target.value)}
              />
              <label className="input-label">
                {t('coreClientOnboarding.legalForm')}
                <select
                  className="input mt-1"
                  value={form.company.legalForm}
                  onChange={(event) =>
                    updateCompany('legalForm', Number(event.target.value))
                  }
                >
                  {legalForms.map((legalForm) => (
                    <option key={legalForm.value} value={legalForm.value}>
                      {legalForm.label}
                    </option>
                  ))}
                </select>
              </label>
              <Input
                required
                type="date"
                label={t('coreClientOnboarding.foundationDate')}
                value={form.company.foundationDate}
                onChange={(event) =>
                  updateCompany('foundationDate', event.target.value)
                }
              />
              <Input
                required
                type="number"
                min="0.01"
                step="any"
                label={t('coreClientOnboarding.totalShares')}
                value={form.company.totalShares}
                onChange={(event) =>
                  updateCompany('totalShares', Number(event.target.value))
                }
              />
              <Input
                required
                type="number"
                min="0.01"
                step="any"
                label={t('coreClientOnboarding.sharePrice')}
                value={form.company.sharePrice}
                onChange={(event) =>
                  updateCompany('sharePrice', Number(event.target.value))
                }
              />
              <label className="input-label">
                {t('coreClientOnboarding.currency')}
                <select
                  className="input mt-1"
                  value={form.company.currency}
                  onChange={(event) =>
                    updateCompany('currency', event.target.value)
                  }
                >
                  <option value="BRL">BRL</option>
                  <option value="USD">USD</option>
                  <option value="EUR">EUR</option>
                  <option value="GBP">GBP</option>
                </select>
              </label>
            </div>
          </section>

          <section className="space-y-4 border-t border-primary-100 pt-5">
            <h2 className="text-lg font-semibold text-primary">
              {t('coreClientOnboarding.administrator')}
            </h2>
            <div className="grid gap-4 md:grid-cols-2">
              <Input
                required
                label={t('coreClientOnboarding.administratorName')}
                value={form.administratorName}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    administratorName: event.target.value,
                  }))
                }
              />
              <Input
                required
                type="email"
                label={t('coreClientOnboarding.administratorEmail')}
                value={form.administratorEmail}
                onChange={(event) =>
                  setForm((current) => ({
                    ...current,
                    administratorEmail: event.target.value,
                  }))
                }
              />
            </div>
          </section>

          <div className="flex justify-end border-t border-primary-100 pt-5">
            <Button
              type="submit"
              loading={onboardClient.isPending}
              icon={<Shield className="h-4 w-4" />}
            >
              {t('coreClientOnboarding.createAndInvite')}
            </Button>
          </div>
        </form>
      )}

      <Card>
        <div className="border-b border-primary-100 p-4">
          <div className="relative max-w-xl">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-primary-400" />
            <Input
              aria-label={t('coreClientOnboarding.search')}
              placeholder={t('coreClientOnboarding.search')}
              value={search}
              onChange={(event) => {
                setSearch(event.target.value);
                setPage(1);
              }}
              className="pl-10"
            />
          </div>
        </div>
        {error ? (
          <p role="alert" className="p-6 text-sm text-red-700">
            {t('coreClientOnboarding.loadError')}
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[760px]">
              <thead className="bg-primary-50 text-left text-xs uppercase text-primary-600">
                <tr>
                  <th className="px-4 py-3">
                    {t('coreClientOnboarding.client')}
                  </th>
                  <th className="px-4 py-3">
                    {t('coreClientOnboarding.document')}
                  </th>
                  <th className="px-4 py-3">{t('common.status')}</th>
                  <th className="px-4 py-3 text-center">
                    {t('coreClientOnboarding.companies')}
                  </th>
                  <th className="px-4 py-3 text-center">
                    {t('coreClientOnboarding.users')}
                  </th>
                  <th className="px-4 py-3 text-right">
                    {t('common.actions')}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-primary-100">
                {isLoading ? (
                  <tr>
                    <td
                      colSpan={6}
                      className="p-8 text-center text-primary-500"
                    >
                      {t('common.loading')}
                    </td>
                  </tr>
                ) : data?.items.length ? (
                  data.items.map((client) => (
                    <tr key={client.id} className="hover:bg-primary-25">
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-3">
                          <Building2 className="h-4 w-4 shrink-0 text-primary-500" />
                          <div>
                            <p className="font-medium text-primary">
                              {client.name}
                            </p>
                            <p className="text-sm text-primary-500">
                              {client.email}
                            </p>
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-3 text-sm text-primary-600">
                        {client.documentFormatted}
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant={statusVariant(client.status)}>
                          {t(
                            `coreClientOnboarding.${client.status.toLowerCase()}`,
                            client.status,
                          )}
                        </Badge>
                      </td>
                      <td className="px-4 py-3 text-center">
                        {client.totalCompanies}
                      </td>
                      <td className="px-4 py-3 text-center">
                        {client.totalUsers}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex justify-end gap-2">
                          {client.status === 'Active' ? (
                            <>
                              <Button
                                variant="secondary"
                                size="sm"
                                onClick={() =>
                                  changeStatus(client.status, client.id)
                                }
                                icon={<X className="h-3.5 w-3.5" />}
                              >
                                {t('coreClientOnboarding.suspend')}
                              </Button>
                              <Button
                                variant="danger"
                                size="sm"
                                onClick={() =>
                                  deactivateClient.mutate(client.id)
                                }
                                icon={<X className="h-3.5 w-3.5" />}
                              >
                                {t('coreClientOnboarding.deactivate')}
                              </Button>
                            </>
                          ) : (
                            <Button
                              variant="success"
                              size="sm"
                              onClick={() =>
                                changeStatus(client.status, client.id)
                              }
                              icon={<Check className="h-3.5 w-3.5" />}
                            >
                              {t('coreClientOnboarding.activate')}
                            </Button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td
                      colSpan={6}
                      className="p-8 text-center text-primary-500"
                    >
                      {t('coreClientOnboarding.noClients')}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        )}
        {data && data.totalPages > 1 && (
          <div className="flex items-center justify-between border-t border-primary-100 px-4 py-3 text-sm">
            <span className="text-primary-500">
              {data.totalCount} {t('coreClientOnboarding.title').toLowerCase()}
            </span>
            <div className="flex gap-2">
              <Button
                variant="secondary"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage((current) => current - 1)}
              >
                {t('common.previous')}
              </Button>
              <Button
                variant="secondary"
                size="sm"
                disabled={page >= data.totalPages}
                onClick={() => setPage((current) => current + 1)}
              >
                {t('common.next')}
              </Button>
            </div>
          </div>
        )}
      </Card>
    </div>
  );
}
