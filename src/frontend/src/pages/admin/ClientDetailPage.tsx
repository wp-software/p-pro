import { FormEvent, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Trash2 } from 'lucide-react';
import toast from 'react-hot-toast';
import { useTranslation } from 'react-i18next';
import { Badge, Button, Card, Input } from '@/components/ui';
import { ConfirmDialog } from '@/components/ui/ConfirmDialog';
import {
  useClient,
  useClientCompanies,
  useClientUsers,
  useDeleteClient,
  useResendAdministratorActivation,
  useUpdateClient,
  useUpdateClientEmail,
} from '@/hooks/useClient';
import { companyService } from '@/services/companyService';
import { clientService } from '@/services/clientService';
import { userService } from '@/services/userService';

const roleOptions = [
  { value: 2, label: 'Admin' },
  { value: 3, label: 'Founder' },
  { value: 4, label: 'BoardMember' },
  { value: 5, label: 'Legal' },
  { value: 6, label: 'Finance' },
  { value: 7, label: 'HR' },
  { value: 8, label: 'Employee' },
  { value: 9, label: 'Investor' },
  { value: 10, label: 'Viewer' },
];

type Tab = 'data' | 'companies' | 'users';

function statusVariant(status: string): 'active' | 'inactive' | 'pending' {
  if (status === 'Active') return 'active';
  if (status === 'Suspended') return 'pending';
  return 'inactive';
}

function errorMessage(error: unknown, fallback: string) {
  return (
    (error as { response?: { data?: { message?: string } } }).response?.data
      ?.message || fallback
  );
}

function DataTab({ clientId }: { clientId: string }) {
  const { t } = useTranslation();
  const { data: client } = useClient(clientId);
  const updateClient = useUpdateClient();
  const updateEmail = useUpdateClientEmail();
  const [form, setForm] = useState({
    name: '',
    tradingName: '',
    phone: '',
    logoUrl: '',
  });
  const [email, setEmail] = useState('');

  useEffect(() => {
    if (!client) return;
    setForm({
      name: client.name,
      tradingName: client.tradingName ?? '',
      phone: client.phone ?? '',
      logoUrl: client.logoUrl ?? '',
    });
    setEmail(client.email);
  }, [client]);

  if (!client) return null;

  const submit = (event: FormEvent) => {
    event.preventDefault();
    updateClient.mutate({
      id: clientId,
      data: {
        name: form.name,
        tradingName: form.tradingName || undefined,
        phone: form.phone || undefined,
        logoUrl: form.logoUrl || undefined,
      },
    });
  };

  const submitEmail = (event: FormEvent) => {
    event.preventDefault();
    updateEmail.mutate({ id: clientId, email });
  };

  return (
    <div className="space-y-6">
      <form onSubmit={submit} className="space-y-4">
        <div className="grid gap-4 md:grid-cols-2">
          <Input
            required
            label={t('coreClientOnboarding.clientName')}
            value={form.name}
            onChange={(e) => setForm({ ...form, name: e.target.value })}
          />
          <Input
            label={t('coreClientOnboarding.tradingName')}
            value={form.tradingName}
            onChange={(e) => setForm({ ...form, tradingName: e.target.value })}
          />
          <Input
            readOnly
            label={t('coreClientOnboarding.document')}
            value={client.documentFormatted}
          />
          <Input
            label={t('coreClientOnboarding.phone')}
            value={form.phone}
            onChange={(e) => setForm({ ...form, phone: e.target.value })}
          />
          <Input
            label={t('coreClientOnboarding.logoUrl')}
            value={form.logoUrl}
            onChange={(e) => setForm({ ...form, logoUrl: e.target.value })}
          />
        </div>
        <Button type="submit" loading={updateClient.isPending}>
          {t('coreClientOnboarding.save')}
        </Button>
      </form>

      <form
        onSubmit={submitEmail}
        className="flex flex-wrap items-end gap-3 border-t border-primary-100 pt-5"
      >
        <div className="min-w-[280px] flex-1">
          <Input
            required
            type="email"
            label={t('coreClientOnboarding.email')}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
          />
        </div>
        <Button
          type="submit"
          variant="secondary"
          loading={updateEmail.isPending}
          disabled={email === client.email}
        >
          {t('coreClientOnboarding.updateEmail')}
        </Button>
      </form>
    </div>
  );
}

function CompaniesTab({ clientId }: { clientId: string }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { data: companies, isLoading } = useClientCompanies(clientId);
  const [deleting, setDeleting] = useState<string | null>(null);

  const refresh = () =>
    queryClient.invalidateQueries({ queryKey: ['clients', clientId] });

  const toggle = useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      active
        ? companyService.deactivateCompany(id)
        : companyService.activateCompany(id),
    onSuccess: refresh,
    onError: (e) => toast.error(errorMessage(e, 'Erro ao alterar empresa')),
  });

  const remove = useMutation({
    mutationFn: (id: string) => companyService.deleteCompany(id),
    onSuccess: () => {
      setDeleting(null);
      refresh();
    },
    onError: (e) => toast.error(errorMessage(e, 'Erro ao excluir empresa')),
  });

  if (isLoading) return <p className="p-4">{t('common.loading')}</p>;
  if (!companies?.length)
    return (
      <p className="p-4 text-primary-500">
        {t('coreClientOnboarding.noCompanies')}
      </p>
    );

  return (
    <>
      <table className="w-full">
        <thead className="bg-primary-50 text-left text-xs uppercase text-primary-600">
          <tr>
            <th className="px-4 py-3">{t('coreClientOnboarding.name')}</th>
            <th className="px-4 py-3">CNPJ</th>
            <th className="px-4 py-3">{t('common.status')}</th>
            <th className="px-4 py-3 text-right">{t('common.actions')}</th>
          </tr>
        </thead>
        <tbody className="divide-y divide-primary-100">
          {companies.map((company) => {
            const active = String(company.status) === 'Active';
            return (
              <tr key={company.id}>
                <td className="px-4 py-3 font-medium">{company.name}</td>
                <td className="px-4 py-3 text-sm">
                  {company.cnpjFormatted ?? company.cnpj}
                </td>
                <td className="px-4 py-3">
                  <Badge variant={active ? 'active' : 'inactive'}>
                    {String(company.status)}
                  </Badge>
                </td>
                <td className="px-4 py-3">
                  <div className="flex justify-end gap-2">
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() =>
                        toggle.mutate({ id: company.id, active })
                      }
                    >
                      {active
                        ? t('coreClientOnboarding.deactivate')
                        : t('coreClientOnboarding.activate')}
                    </Button>
                    <Button
                      size="sm"
                      variant="danger"
                      aria-label={t('coreClientOnboarding.delete')}
                      onClick={() => setDeleting(company.id)}
                      icon={<Trash2 className="h-3.5 w-3.5" />}
                    >
                      {t('coreClientOnboarding.delete')}
                    </Button>
                  </div>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
      <ConfirmDialog
        isOpen={!!deleting}
        title={t('coreClientOnboarding.delete')}
        message={t('coreClientOnboarding.confirmCompanyDelete')}
        confirmVariant="danger"
        isLoading={remove.isPending}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting)}
      />
    </>
  );
}

function UsersTab({ clientId }: { clientId: string }) {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [deleting, setDeleting] = useState<string | null>(null);
  const { data, isLoading } = useClientUsers(clientId, {
    page,
    pageSize: 10,
    search,
  });
  const resend = useResendAdministratorActivation();
  const { data: companies } = useClientCompanies(clientId);
  const [showInvite, setShowInvite] = useState(false);
  const [invite, setInvite] = useState({
    name: '',
    email: '',
    companyId: '',
    role: 10,
  });

  const createUser = useMutation({
    mutationFn: () =>
      clientService.createClientUser(clientId, {
        ...invite,
        companyId: invite.companyId || companies?.[0]?.id || '',
      }),
    onSuccess: () => {
      toast.success(t('coreClientOnboarding.inviteSent'));
      setShowInvite(false);
      setInvite({ name: '', email: '', companyId: '', role: 10 });
      refresh();
    },
    onError: (e) => toast.error(errorMessage(e, 'Erro ao convidar usuário')),
  });

  const refresh = () =>
    queryClient.invalidateQueries({ queryKey: ['clients', clientId, 'users'] });

  const toggle = useMutation({
    mutationFn: ({ id, active }: { id: string; active: boolean }) =>
      active ? userService.deactivate(id) : userService.activate(id),
    onSuccess: refresh,
    onError: (e) => toast.error(errorMessage(e, 'Erro ao alterar usuário')),
  });

  const remove = useMutation({
    mutationFn: (id: string) => userService.delete(id),
    onSuccess: () => {
      setDeleting(null);
      refresh();
    },
    onError: (e) => toast.error(errorMessage(e, 'Erro ao excluir usuário')),
  });

  return (
    <>
      <div className="flex items-center justify-between gap-3 border-b border-primary-100 p-4">
        <div className="max-w-md flex-1">
        <Input
          aria-label={t('coreClientOnboarding.searchUsers')}
          placeholder={t('coreClientOnboarding.searchUsers')}
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
        </div>
        <Button size="sm" onClick={() => setShowInvite((v) => !v)}>
          {t('coreClientOnboarding.newUser')}
        </Button>
      </div>
      {showInvite && (
        <form
          onSubmit={(e) => {
            e.preventDefault();
            createUser.mutate();
          }}
          className="grid gap-4 border-b border-primary-100 p-4 md:grid-cols-2"
        >
          <Input
            required
            label={t('coreClientOnboarding.name')}
            value={invite.name}
            onChange={(e) => setInvite({ ...invite, name: e.target.value })}
          />
          <Input
            required
            type="email"
            label={t('coreClientOnboarding.email')}
            value={invite.email}
            onChange={(e) => setInvite({ ...invite, email: e.target.value })}
          />
          <label className="input-label">
            {t('coreClientOnboarding.company')}
            <select
              className="input mt-1"
              value={invite.companyId || companies?.[0]?.id || ''}
              onChange={(e) =>
                setInvite({ ...invite, companyId: e.target.value })
              }
            >
              {companies?.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </label>
          <label className="input-label">
            {t('coreClientOnboarding.role')}
            <select
              className="input mt-1"
              value={invite.role}
              onChange={(e) =>
                setInvite({ ...invite, role: Number(e.target.value) })
              }
            >
              {roleOptions.map((r) => (
                <option key={r.value} value={r.value}>
                  {r.label}
                </option>
              ))}
            </select>
          </label>
          <div className="md:col-span-2">
            <Button type="submit" loading={createUser.isPending}>
              {t('coreClientOnboarding.sendInvite')}
            </Button>
          </div>
        </form>
      )}
      {isLoading ? (
        <p className="p-4">{t('common.loading')}</p>
      ) : !data?.items.length ? (
        <p className="p-4 text-primary-500">
          {t('coreClientOnboarding.noUsers')}
        </p>
      ) : (
        <table className="w-full">
          <thead className="bg-primary-50 text-left text-xs uppercase text-primary-600">
            <tr>
              <th className="px-4 py-3">{t('coreClientOnboarding.name')}</th>
              <th className="px-4 py-3">{t('coreClientOnboarding.roles')}</th>
              <th className="px-4 py-3">{t('common.status')}</th>
              <th className="px-4 py-3 text-right">{t('common.actions')}</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-primary-100">
            {data.items.map((user) => {
              const active = user.status === 'Active';
              return (
                <tr key={user.id}>
                  <td className="px-4 py-3">
                    <p className="font-medium">{user.name}</p>
                    <p className="text-sm text-primary-500">{user.email}</p>
                  </td>
                  <td className="px-4 py-3 text-sm">{user.roles.join(', ')}</td>
                  <td className="px-4 py-3">
                    <Badge variant={statusVariant(user.status)}>
                      {user.status}
                    </Badge>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex flex-wrap justify-end gap-2">
                      {user.status === 'Pending' && (
                        <Button
                          size="sm"
                          variant="secondary"
                          onClick={() => resend.mutate(user.id)}
                        >
                          {t('coreClientOnboarding.resendInvite')}
                        </Button>
                      )}
                      <Button
                        size="sm"
                        variant="secondary"
                        onClick={() => toggle.mutate({ id: user.id, active })}
                      >
                        {active
                          ? t('coreClientOnboarding.deactivate')
                          : t('coreClientOnboarding.activate')}
                      </Button>
                      <Button
                        size="sm"
                        variant="danger"
                        onClick={() => setDeleting(user.id)}
                        icon={<Trash2 className="h-3.5 w-3.5" />}
                      >
                        {t('coreClientOnboarding.delete')}
                      </Button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
      {data && data.totalPages > 1 && (
        <div className="flex justify-end gap-2 border-t border-primary-100 p-3">
          <Button
            size="sm"
            variant="secondary"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
          >
            {t('common.previous')}
          </Button>
          <Button
            size="sm"
            variant="secondary"
            disabled={page >= data.totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            {t('common.next')}
          </Button>
        </div>
      )}
      <ConfirmDialog
        isOpen={!!deleting}
        title={t('coreClientOnboarding.delete')}
        message={t('coreClientOnboarding.confirmUserDelete')}
        confirmVariant="danger"
        isLoading={remove.isPending}
        onCancel={() => setDeleting(null)}
        onConfirm={() => deleting && remove.mutate(deleting)}
      />
    </>
  );
}

export default function ClientDetailPage() {
  const { id = '' } = useParams<{ id: string }>();
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>('data');
  const [confirmDelete, setConfirmDelete] = useState(false);
  const { data: client, isLoading, error } = useClient(id);
  const deleteClient = useDeleteClient();

  if (isLoading) return <p className="p-6">{t('common.loading')}</p>;
  if (error || !client)
    return (
      <p role="alert" className="p-6 text-red-700">
        {t('coreClientOnboarding.notFound')}
      </p>
    );

  const tabs: { key: Tab; label: string }[] = [
    { key: 'data', label: t('coreClientOnboarding.tabData') },
    { key: 'companies', label: t('coreClientOnboarding.tabCompanies') },
    { key: 'users', label: t('coreClientOnboarding.tabUsers') },
  ];

  return (
    <div className="space-y-6">
      <Link
        to="/admin/clients"
        className="inline-flex items-center gap-2 text-sm text-primary-600"
      >
        <ArrowLeft className="h-4 w-4" />
        {t('coreClientOnboarding.back')}
      </Link>
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-primary">{client.name}</h1>
          <p className="mt-1 flex items-center gap-2 text-primary-500">
            {client.documentFormatted}
            <Badge variant={statusVariant(client.status)}>
              {t(
                `coreClientOnboarding.${client.status.toLowerCase()}`,
                client.status,
              )}
            </Badge>
          </p>
        </div>
        <Button
          variant="danger"
          icon={<Trash2 className="h-4 w-4" />}
          onClick={() => setConfirmDelete(true)}
        >
          {t('coreClientOnboarding.delete')}
        </Button>
      </header>

      <div role="tablist" className="flex gap-4 border-b border-primary-100">
        {tabs.map(({ key, label }) => (
          <button
            key={key}
            role="tab"
            aria-selected={tab === key}
            onClick={() => setTab(key)}
            className={`-mb-px border-b-2 px-1 py-2 text-sm font-medium ${
              tab === key
                ? 'border-primary text-primary'
                : 'border-transparent text-primary-500'
            }`}
          >
            {label}
          </button>
        ))}
      </div>

      <Card>
        <div className={tab === 'data' ? 'p-6' : ''}>
          {tab === 'data' && <DataTab clientId={id} />}
          {tab === 'companies' && <CompaniesTab clientId={id} />}
          {tab === 'users' && <UsersTab clientId={id} />}
        </div>
      </Card>

      <ConfirmDialog
        isOpen={confirmDelete}
        title={t('coreClientOnboarding.deleteTitle')}
        message={t('coreClientOnboarding.deleteMessage')}
        confirmVariant="danger"
        isLoading={deleteClient.isPending}
        onCancel={() => setConfirmDelete(false)}
        onConfirm={() =>
          deleteClient.mutate(id, {
            onSuccess: () => navigate('/admin/clients'),
          })
        }
      />
    </div>
  );
}
