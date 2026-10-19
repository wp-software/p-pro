import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Edit2,
  Trash2,
  Mail,
  Phone,
  Building2,
  Calendar,
  FileText,
  User,
} from 'lucide-react';
import { Button, Card, Spinner } from '@/components/ui';
import { ShareholderTypeBadge, ShareholderStatusBadge, ShareholderFormModal } from '@/components/shareholders';
import { useShareholder, useDeleteShareholder } from '@/hooks/useShareholders';
import { useConfirm } from '@/components/ui';
import { documentTypeLabels } from '@/types';

export default function ShareholderDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { confirm } = useConfirm();
  
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);

  const { data: shareholder, isLoading, isError } = useShareholder(id || '');
  const deleteShareholderMutation = useDeleteShareholder();

  const handleBack = () => {
    navigate('/shareholders');
  };

  const handleEdit = () => {
    setIsEditModalOpen(true);
  };

  const handleDelete = async () => {
    if (!shareholder) return;

    const confirmed = await confirm({
      title: 'Excluir Sócio',
      message: `Tem certeza que deseja excluir "${shareholder.name}"? Esta ação não pode ser desfeita.`,
      confirmText: 'Excluir',
      cancelText: 'Cancelar',
      confirmVariant: 'danger',
    });

    if (confirmed) {
      deleteShareholderMutation.mutate(shareholder.id, {
        onSuccess: () => navigate('/shareholders'),
      });
    }
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-64">
        <Spinner className="w-8 h-8" />
      </div>
    );
  }

  if (isError || !shareholder) {
    return (
      <Card className="text-center py-12">
        <div className="w-16 h-16 bg-error-100 rounded-full flex items-center justify-center mx-auto mb-4">
          <User className="w-8 h-8 text-error-500" />
        </div>
        <h3 className="font-semibold text-primary mb-2">
          Sócio não encontrado
        </h3>
        <p className="text-primary-500 mb-4">
          O sócio solicitado não existe ou foi removido.
        </p>
        <Button variant="secondary" onClick={handleBack}>
          Voltar para lista
        </Button>
      </Card>
    );
  }

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Button variant="ghost" onClick={handleBack}>
            <ArrowLeft className="w-4 h-4 mr-2" />
            Voltar
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-primary">{shareholder.name}</h1>
            <div className="flex items-center gap-2 mt-1">
              <ShareholderTypeBadge type={shareholder.type} />
              <ShareholderStatusBadge status={shareholder.status} />
            </div>
          </div>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" onClick={handleEdit}>
            <Edit2 className="w-4 h-4 mr-2" />
            Editar
          </Button>
          <Button
            variant="danger"
            onClick={handleDelete}
            loading={deleteShareholderMutation.isPending}
          >
            <Trash2 className="w-4 h-4 mr-2" />
            Excluir
          </Button>
        </div>
      </div>

      {/* Content Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Info */}
        <Card className="lg:col-span-2">
          <h2 className="text-lg font-semibold text-primary mb-4">
            Informações Gerais
          </h2>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* Company */}
            <div className="flex items-start gap-3">
              <div className="w-10 h-10 bg-primary-100 rounded-lg flex items-center justify-center">
                <Building2 className="w-5 h-5 text-primary-600" />
              </div>
              <div>
                <p className="text-sm text-primary-500">Empresa</p>
                <p className="font-medium text-primary">{shareholder.companies && shareholder.companies.length > 1
                    ? shareholder.companies.map((c) => c.name).join(', ')
                    : shareholder.companyName}</p>
              </div>
            </div>

            {/* Document */}
            <div className="flex items-start gap-3">
              <div className="w-10 h-10 bg-primary-100 rounded-lg flex items-center justify-center">
                <FileText className="w-5 h-5 text-primary-600" />
              </div>
              <div>
                <p className="text-sm text-primary-500">
                  {documentTypeLabels[shareholder.documentType]}
                </p>
                <p className="font-medium text-primary font-mono">
                  {shareholder.documentFormatted || shareholder.document}
                </p>
              </div>
            </div>

            {/* Email */}
            {shareholder.email && (
              <div className="flex items-start gap-3">
                <div className="w-10 h-10 bg-primary-100 rounded-lg flex items-center justify-center">
                  <Mail className="w-5 h-5 text-primary-600" />
                </div>
                <div>
                  <p className="text-sm text-primary-500">E-mail</p>
                  <a
                    href={`mailto:${shareholder.email}`}
                    className="font-medium text-accent hover:underline"
                  >
                    {shareholder.email}
                  </a>
                </div>
              </div>
            )}

            {/* Phone */}
            {shareholder.phone && (
              <div className="flex items-start gap-3">
                <div className="w-10 h-10 bg-primary-100 rounded-lg flex items-center justify-center">
                  <Phone className="w-5 h-5 text-primary-600" />
                </div>
                <div>
                  <p className="text-sm text-primary-500">Telefone</p>
                  <a
                    href={`tel:${shareholder.phone}`}
                    className="font-medium text-accent hover:underline"
                  >
                    {shareholder.phone}
                  </a>
                </div>
              </div>
            )}

            {/* Created At */}
            <div className="flex items-start gap-3">
              <div className="w-10 h-10 bg-primary-100 rounded-lg flex items-center justify-center">
                <Calendar className="w-5 h-5 text-primary-600" />
              </div>
              <div>
                <p className="text-sm text-primary-500">Data de Cadastro</p>
                <p className="font-medium text-primary">
                  {new Date(shareholder.createdAt).toLocaleDateString('pt-BR', {
                    day: '2-digit',
                    month: 'long',
                    year: 'numeric',
                  })}
                </p>
              </div>
            </div>

            {/* Updated At */}
            <div className="flex items-start gap-3">
              <div className="w-10 h-10 bg-primary-100 rounded-lg flex items-center justify-center">
                <Calendar className="w-5 h-5 text-primary-600" />
              </div>
              <div>
                <p className="text-sm text-primary-500">Última Atualização</p>
                <p className="font-medium text-primary">
                  {new Date(shareholder.updatedAt).toLocaleDateString('pt-BR', {
                    day: '2-digit',
                    month: 'long',
                    year: 'numeric',
                  })}
                </p>
              </div>
            </div>
          </div>

          {/* Notes */}
          {shareholder.notes && (
            <div className="mt-6 pt-6 border-t">
              <h3 className="text-sm font-medium text-primary-500 mb-2">
                Observações
              </h3>
              <p className="text-primary whitespace-pre-wrap">{shareholder.notes}</p>
            </div>
          )}
        </Card>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Quick Actions */}
          <Card>
            <h2 className="text-lg font-semibold text-primary mb-4">
              Ações Rápidas
            </h2>
            <div className="space-y-2">
              {shareholder.email && (
                <Button
                  variant="secondary"
                  className="w-full justify-start"
                  onClick={() => window.location.href = `mailto:${shareholder.email}`}
                >
                  <Mail className="w-4 h-4 mr-2" />
                  Enviar E-mail
                </Button>
              )}
              {shareholder.phone && (
                <Button
                  variant="secondary"
                  className="w-full justify-start"
                  onClick={() => window.location.href = `tel:${shareholder.phone}`}
                >
                  <Phone className="w-4 h-4 mr-2" />
                  Ligar
                </Button>
              )}
            </div>
          </Card>

          {/* Future: Shares summary will go here */}
          <Card>
            <h2 className="text-lg font-semibold text-primary mb-4">
              Participação Societária
            </h2>
            <p className="text-sm text-primary-500">
              As informações de ações e participação serão exibidas aqui após a
              implementação do módulo de CapTable.
            </p>
          </Card>
        </div>
      </div>

      {/* Edit Modal */}
      <ShareholderFormModal
        isOpen={isEditModalOpen}
        onClose={() => setIsEditModalOpen(false)}
        shareholder={shareholder}
      />
    </div>
  );
}
