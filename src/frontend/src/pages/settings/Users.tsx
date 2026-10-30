import { useState } from 'react';
import { Plus, Search, Edit, Trash2, Shield, Mail } from 'lucide-react';
import { Button, Input, Badge, Card } from '@/components/ui';
import { useUsers, useDeleteUser, useActivateUser, useResendActivationEmail } from '@/hooks/useUsers';
import { UserForm } from '@/components/users/UserForm';
import type { UserSummary } from '@/types';

export default function Users() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [showForm, setShowForm] = useState(false);
  const [editingUser, setEditingUser] = useState<UserSummary | null>(null);

  const { data, isLoading, error } = useUsers({ page, pageSize: 10, search });
  const deleteUser = useDeleteUser();
  const activateUser = useActivateUser();
  const resendActivation = useResendActivationEmail();

  const handleDelete = async (user: UserSummary) => {
    if (confirm(`Deseja realmente desativar o usuário ${user.name}?`)) {
      deleteUser.mutate(user.id);
    }
  };

  const handleActivate = async (user: UserSummary) => {
    activateUser.mutate(user.id);
  };

  const getStatusBadge = (status: string) => {
    const variants: Record<string, 'active' | 'pending' | 'inactive' | 'vesting' | 'investor' | 'founder'> = {
      Active: 'active',
      Pending: 'pending',
      Inactive: 'inactive',
    };
    return <Badge variant={variants[status] || 'inactive'}>{status}</Badge>;
  };

  if (error) {
    return (
      <div className="p-6">
        <Card className="p-8 text-center">
          <p className="text-red-500">Erro ao carregar usuários</p>
        </Card>
      </div>
    );
  }

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-primary">Usuários</h1>
          <p className="text-primary-500">Gerencie os usuários da sua empresa</p>
        </div>
        <Button
          onClick={() => setShowForm(true)}
          icon={<Plus className="w-4 h-4" />}
        >
          Novo Usuário
        </Button>
      </div>

      {/* Search */}
      <Card className="p-4">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-primary-400" />
          <Input
            placeholder="Buscar por nome ou email..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-10"
          />
        </div>
      </Card>

      {/* Table */}
      <Card>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-primary-50">
              <tr>
                <th className="px-4 py-3 text-left text-sm font-medium text-primary-600">
                  Nome
                </th>
                <th className="px-4 py-3 text-left text-sm font-medium text-primary-600">
                  Email
                </th>
                <th className="px-4 py-3 text-left text-sm font-medium text-primary-600">
                  Papéis
                </th>
                <th className="px-4 py-3 text-left text-sm font-medium text-primary-600">
                  Sócio vinculado
                </th>
                <th className="px-4 py-3 text-left text-sm font-medium text-primary-600">
                  Status
                </th>
                <th className="px-4 py-3 text-right text-sm font-medium text-primary-600">
                  Ações
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-primary-100">
              {isLoading ? (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-primary-500">
                    Carregando...
                  </td>
                </tr>
              ) : data?.items.length === 0 ? (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-primary-500">
                    Nenhum usuário encontrado
                  </td>
                </tr>
              ) : (
                data?.items.map((user) => (
                  <tr key={user.id} className="hover:bg-primary-50">
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <div className="w-8 h-8 rounded-full bg-primary-200 flex items-center justify-center">
                          <span className="text-sm font-medium text-primary-700">
                            {user.name.charAt(0).toUpperCase()}
                          </span>
                        </div>
                        <span className="font-medium text-primary">{user.name}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-primary-600">{user.email}</td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1">
                        {user.roles.map((role) => (
                          <Badge key={role} variant="founder">
                            {role}
                          </Badge>
                        ))}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-primary-600">
                      {user.shareholderName ?? '—'}
                    </td>
                    <td className="px-4 py-3">{getStatusBadge(user.status)}</td>
                    <td className="px-4 py-3">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          onClick={() => setEditingUser(user)}
                          className="p-1 hover:bg-primary-100 rounded"
                          title="Editar"
                        >
                          <Edit className="w-4 h-4 text-primary-500" />
                        </button>
                        {user.status === 'Pending' ? (
                          <button
                            onClick={() => resendActivation.mutate(user.id)}
                            disabled={resendActivation.isPending}
                            className="p-1 hover:bg-blue-100 rounded disabled:opacity-50"
                            title="Reenviar e-mail de ativação"
                          >
                            <Mail className="w-4 h-4 text-blue-500" />
                          </button>
                        ) : user.status === 'Inactive' ? (
                          <button
                            onClick={() => handleActivate(user)}
                            className="p-1 hover:bg-green-100 rounded"
                            title="Ativar"
                          >
                            <Shield className="w-4 h-4 text-green-500" />
                          </button>
                        ) : (
                          <button
                            onClick={() => handleDelete(user)}
                            className="p-1 hover:bg-red-100 rounded"
                            title="Desativar"
                          >
                            <Trash2 className="w-4 h-4 text-red-500" />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {data && data.totalPages > 1 && (
          <div className="px-4 py-3 border-t border-primary-100 flex items-center justify-between">
            <p className="text-sm text-primary-500">
              Mostrando {data.items.length} de {data.total} usuários
            </p>
            <div className="flex gap-2">
              <Button
                variant="secondary"
                size="sm"
                disabled={page === 1}
                onClick={() => setPage(page - 1)}
              >
                Anterior
              </Button>
              <Button
                variant="secondary"
                size="sm"
                disabled={page === data.totalPages}
                onClick={() => setPage(page + 1)}
              >
                Próximo
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* Modal Form */}
      {(showForm || editingUser) && (
        <UserForm
          user={editingUser}
          onClose={() => {
            setShowForm(false);
            setEditingUser(null);
          }}
        />
      )}
    </div>
  );
}
