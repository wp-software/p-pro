import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Search, Copy, Edit, Trash2, Eye, FileText, Loader2 } from 'lucide-react';
import { Button, Card, Badge } from '@/components/ui';
import { ConfirmDialog } from '@/components/ui/ConfirmDialog';
import VariablesList from '@/components/contracts/VariablesList';
import { CONTRACT_STATUS_CONFIG, CONTRACT_TEMPLATE_TYPE_CONFIG } from '@/constants/contractConstants';
import type {
  ContractTemplate,
  CreateContractTemplateRequest,
  UpdateContractTemplateRequest,
} from '@/types/contract.types';
import {
  ContractStatus,
  ContractTemplateType,
} from '@/types/contract.types';
import { contractTemplateService } from '@/services/contractService';
import { useAuthStore } from '@/stores/authStore';
import { sanitizeHtml } from '@/utils/sanitizeHtml';

/**
 * ContractTemplatesPage
 * Página de gestão de templates de contratos
 * Rotas: /contracts/templates
 */
function ContractTemplatesPage() {
  const navigate = useNavigate();
  const { user } = useAuthStore();
  const canManageTemplates = user?.roles?.some((role) => role === 'Admin' || role === 'Legal') ?? false;
  const [templates, setTemplates] = useState<ContractTemplate[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedType, setSelectedType] = useState<ContractTemplateType | 'all'>('all');
  const [selectedStatus, setSelectedStatus] = useState<'all' | 'active' | 'inactive'>('all');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingTemplate, setEditingTemplate] = useState<ContractTemplate | null>(null);
  const [previewTemplate, setPreviewTemplate] = useState<ContractTemplate | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<ContractTemplate | null>(null);
  const [actionLoadingId, setActionLoadingId] = useState<string | null>(null);

  const pageSize = 10;

  if (!canManageTemplates) {
    return (
      <div className="max-w-3xl mx-auto">
        <Card className="p-8 text-center">
          <FileText className="w-12 h-12 text-gray-300 mx-auto mb-4" />
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Acesso restrito</h2>
          <p className="text-gray-600 mb-6">
            Voce nao tem permissao para gerenciar templates de contratos.
          </p>
          <Button variant="primary" onClick={() => navigate('/contracts')}>
            Voltar para Contratos
          </Button>
        </Card>
      </div>
    );
  }

  useEffect(() => {
    loadTemplates();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentPage, selectedType, selectedStatus, searchTerm]);

  const loadTemplates = async () => {
    try {
      setIsLoading(true);
      setError(null);

      const filters = {
        page: currentPage,
        pageSize,
        search: searchTerm || undefined,
        templateType: selectedType !== 'all' ? selectedType : undefined,
        isActive:
          selectedStatus === 'all' ? undefined : selectedStatus === 'active',
      };

      const response = await contractTemplateService.getTemplates(filters);
      setTemplates(response.items);
      setTotalPages(response.totalPages);
      setTotalCount(response.totalCount);
    } catch (err) {
      setError('Erro ao carregar templates. Tente novamente.');
      console.error('Erro ao carregar templates:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleRefresh = () => {
    loadTemplates();
  };

  const handleCreateTemplate = () => {
    setEditingTemplate(null);
    setIsModalOpen(true);
  };

  const handleEditTemplate = (template: ContractTemplate) => {
    setEditingTemplate(template);
    setIsModalOpen(true);
  };

  const handlePreviewTemplate = (template: ContractTemplate) => {
    setPreviewTemplate(template);
  };

  const handleCloneTemplate = async (template: ContractTemplate) => {
    try {
      setActionLoadingId(template.id);
      const clonedPayload: CreateContractTemplateRequest = {
        name: `${template.name} (Copia)`,
        code: `${template.code}-COPY-${Date.now().toString().slice(-4)}`,
        templateType: template.templateType,
        content: template.content,
        description: template.description,
        tags: template.tags,
        defaultStatus: template.defaultStatus,
      };
      await contractTemplateService.createTemplate(clonedPayload);
      await loadTemplates();
    } catch (err) {
      console.error('Erro ao clonar template:', err);
    } finally {
      setActionLoadingId(null);
    }
  };

  const handleDeleteTemplate = (template: ContractTemplate) => {
    setDeleteTarget(template);
  };

  const confirmDeleteTemplate = async () => {
    if (!deleteTarget) return;
    try {
      setIsDeleting(true);
      await contractTemplateService.deleteTemplate(deleteTarget.id);
      await loadTemplates();
      setDeleteTarget(null);
    } catch (err) {
      console.error('Erro ao deletar template:', err);
    } finally {
      setIsDeleting(false);
    }
  };

  const handleToggleActive = async (template: ContractTemplate) => {
    try {
      setActionLoadingId(template.id);
      if (template.isActive) {
        await contractTemplateService.deactivateTemplate(template.id);
      } else {
        await contractTemplateService.activateTemplate(template.id);
      }
      await loadTemplates();
    } catch (err) {
      console.error('Erro ao atualizar status do template:', err);
    } finally {
      setActionLoadingId(null);
    }
  };

  const emptyStateMessage = useMemo(() => {
    if (searchTerm || selectedType !== 'all' || selectedStatus !== 'all') {
      return 'Tente ajustar seus filtros';
    }
    return 'Crie seu primeiro template para começar';
  }, [searchTerm, selectedStatus, selectedType]);

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">Templates de Contratos</h1>
          <p className="mt-1 text-sm text-gray-500">
            Gerencie os templates reutilizáveis para criação de novos contratos
          </p>
        </div>
        <Button
          variant="primary"
          size="lg"
          icon={<Plus className="w-5 h-5" />}
          onClick={handleCreateTemplate}
        >
          Novo Template
        </Button>
      </div>

      {/* Filters and Search */}
      <Card className="p-4">
        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          {/* Search */}
          <div className="relative md:col-span-2">
            <Search className="absolute left-3 top-3 w-5 h-5 text-gray-400" />
            <input
              type="text"
              placeholder="Buscar por nome ou código..."
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setCurrentPage(1);
              }}
              className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-cyan-600"
            />
          </div>

          {/* Type Filter */}
          <select
            value={selectedType}
            onChange={(e) =>
              setSelectedType(e.target.value as ContractTemplateType | 'all')
            }
            className="px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-cyan-600"
          >
            <option value="all">Todos os Tipos</option>
            {Object.entries(CONTRACT_TEMPLATE_TYPE_CONFIG).map(([key, config]) => (
              <option key={key} value={key}>
                {config.label}
              </option>
            ))}
          </select>

          {/* Status Filter */}
          <select
            value={selectedStatus}
            onChange={(e) => {
              setSelectedStatus(e.target.value as 'all' | 'active' | 'inactive');
              setCurrentPage(1);
            }}
            className="px-4 py-2 border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-cyan-600"
          >
            <option value="all">Todos os Status</option>
            <option value="active">Ativos</option>
            <option value="inactive">Inativos</option>
          </select>
        </div>
      </Card>

      {/* Results Header */}
      <div className="flex items-center justify-between">
        <p className="text-sm text-gray-500">
          {totalCount} template{totalCount !== 1 ? 's' : ''}
          {totalPages > 1 && ` - Página ${currentPage} de ${totalPages}`}
        </p>
      </div>

      {/* Templates Table */}
      {isLoading && (
        <Card className="p-12 text-center">
          <Loader2 className="w-16 h-16 text-cyan-600 mx-auto mb-4 animate-spin" />
          <p className="text-gray-500">Carregando templates...</p>
        </Card>
      )}

      {!isLoading && error && (
        <Card className="p-12 text-center">
          <div className="text-red-600 mb-4">⚠️</div>
          <h3 className="text-lg font-medium text-gray-900 mb-2">Erro ao carregar</h3>
          <p className="text-gray-500 mb-4">{error}</p>
          <Button variant="primary" onClick={handleRefresh}>
            Tentar Novamente
          </Button>
        </Card>
      )}

      {!isLoading && !error && templates.length > 0 ? (
        <Card className="overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-gray-50 border-b border-gray-200">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Nome
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Código
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Tipo
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Versão
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Status
                  </th>
                  <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                    Ações
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {templates.map((template) => (
                  <tr key={template.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center gap-3">
                        <FileText className="w-5 h-5 text-cyan-600" />
                        <span className="font-medium text-gray-900">{template.name}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {template.code}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <Badge className="bg-blue-100 text-blue-700">
                        {CONTRACT_TEMPLATE_TYPE_CONFIG[template.templateType]?.label ||
                          template.templateType}
                      </Badge>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      v{template.version}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <Badge variant={template.isActive ? 'active' : 'inactive'}>
                        {template.isActive ? 'Ativo' : 'Inativo'}
                      </Badge>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      <div className="flex justify-end gap-2">
                        <button
                          onClick={() => handlePreviewTemplate(template)}
                          className="text-slate-600 hover:text-slate-700"
                          title="Visualizar"
                        >
                          <Eye className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleEditTemplate(template)}
                          className="text-cyan-600 hover:text-cyan-700"
                          title="Editar"
                          disabled={actionLoadingId === template.id}
                        >
                          <Edit className="w-4 h-4" />
                        </button>
                        <button
                          onClick={() => handleCloneTemplate(template)}
                          className="text-blue-600 hover:text-blue-700"
                          title="Clonar"
                          disabled={actionLoadingId === template.id}
                        >
                          {actionLoadingId === template.id ? (
                            <Loader2 className="w-4 h-4 animate-spin" />
                          ) : (
                            <Copy className="w-4 h-4" />
                          )}
                        </button>
                        <button
                          onClick={() => handleToggleActive(template)}
                          className="text-amber-600 hover:text-amber-700"
                          title={template.isActive ? 'Desativar' : 'Ativar'}
                          disabled={actionLoadingId === template.id}
                        >
                          {template.isActive ? 'Desativar' : 'Ativar'}
                        </button>
                        <button
                          onClick={() => handleDeleteTemplate(template)}
                          className="text-red-600 hover:text-red-700"
                          title="Deletar"
                          disabled={actionLoadingId === template.id}
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      ) : null}

      {/* Pagination */}
      {!isLoading && !error && templates.length > 0 && totalPages > 1 && (
        <div className="flex justify-center items-center gap-2 mt-6">
          <Button
            variant="secondary"
            size="sm"
            disabled={currentPage === 1}
            onClick={() => setCurrentPage((page) => Math.max(1, page - 1))}
          >
            Anterior
          </Button>
          <span className="text-sm text-gray-600">
            Página {currentPage} de {totalPages}
          </span>
          <Button
            variant="secondary"
            size="sm"
            disabled={currentPage === totalPages}
            onClick={() => setCurrentPage((page) => Math.min(totalPages, page + 1))}
          >
            Próxima
          </Button>
        </div>
      )}

      {/* Empty State */}
      {!isLoading && !error && templates.length === 0 && (
        <Card className="p-12 text-center">
          <FileText className="w-16 h-16 text-gray-300 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            Nenhum template encontrado
          </h3>
          <p className="text-gray-500 mb-6">
            {emptyStateMessage}
          </p>
          <Button
            variant="primary"
            icon={<Plus className="w-4 h-4" />}
            onClick={handleCreateTemplate}
          >
            Criar Primeiro Template
          </Button>
        </Card>
      )}

      <TemplateModal
        isOpen={isModalOpen}
        isSaving={isSaving}
        template={editingTemplate}
        onClose={() => setIsModalOpen(false)}
        onSave={async (payload, isEdit) => {
          try {
            setIsSaving(true);
            if (isEdit && editingTemplate) {
              await contractTemplateService.updateTemplate(editingTemplate.id, payload);
            } else {
              await contractTemplateService.createTemplate(payload as CreateContractTemplateRequest);
            }
            setIsModalOpen(false);
            setEditingTemplate(null);
            await loadTemplates();
          } finally {
            setIsSaving(false);
          }
        }}
      />

      <PreviewModal
        template={previewTemplate}
        onClose={() => setPreviewTemplate(null)}
      />

      <ConfirmDialog
        isOpen={Boolean(deleteTarget)}
        title="Excluir template"
        message={`Tem certeza que deseja excluir o template ${deleteTarget?.name}?`}
        confirmText="Excluir"
        confirmVariant="danger"
        onConfirm={confirmDeleteTemplate}
        onCancel={() => setDeleteTarget(null)}
        isLoading={isDeleting}
      />
    </div>
  );
}

export default ContractTemplatesPage;

interface TemplateModalProps {
  isOpen: boolean;
  isSaving: boolean;
  template: ContractTemplate | null;
  onClose: () => void;
  onSave: (payload: CreateContractTemplateRequest | UpdateContractTemplateRequest, isEdit: boolean) => void;
}

interface TemplateFormState {
  name: string;
  code: string;
  templateType: ContractTemplateType;
  content: string;
  description: string;
  tags: string;
  defaultStatus: ContractStatus;
  isActive: boolean;
}

function TemplateModal({ isOpen, isSaving, template, onClose, onSave }: TemplateModalProps) {
  const isEdit = Boolean(template);
  const [formState, setFormState] = useState<TemplateFormState>({
    name: template?.name || '',
    code: template?.code || '',
    templateType: template?.templateType || ContractTemplateType.Other,
    content: template?.content || '',
    description: template?.description || '',
    tags: template?.tags?.join(', ') || '',
    defaultStatus: template?.defaultStatus || ContractStatus.Draft,
    isActive: template?.isActive ?? true,
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (isOpen) {
      setFormState({
        name: template?.name || '',
        code: template?.code || '',
        templateType: template?.templateType || ContractTemplateType.Other,
        content: template?.content || '',
        description: template?.description || '',
        tags: template?.tags?.join(', ') || '',
        defaultStatus: template?.defaultStatus || ContractStatus.Draft,
        isActive: template?.isActive ?? true,
      });
      setErrors({});
    }
  }, [isOpen, template]);

  if (!isOpen) return null;

  const handleChange = (
    key: keyof TemplateFormState,
    value: string | boolean | ContractTemplateType | ContractStatus
  ) => {
    setFormState((prev) => ({ ...prev, [key]: value }));
  };

  const validate = () => {
    const nextErrors: Record<string, string> = {};
    if (!formState.name.trim()) nextErrors.name = 'Nome é obrigatório';
    if (!formState.code.trim()) nextErrors.code = 'Código é obrigatório';
    if (!formState.content.trim()) nextErrors.content = 'Conteúdo é obrigatório';
    setErrors(nextErrors);
    return Object.keys(nextErrors).length === 0;
  };

  const handleSubmit = () => {
    if (!validate()) return;

    const tags = formState.tags
      ? formState.tags.split(',').map((tag) => tag.trim()).filter(Boolean)
      : undefined;

    if (isEdit) {
      const updatePayload: UpdateContractTemplateRequest = {
        name: formState.name.trim(),
        description: formState.description.trim() || undefined,
        content: formState.content,
        tags,
        isActive: formState.isActive,
      };
      onSave(updatePayload, true);
      return;
    }

    const createPayload: CreateContractTemplateRequest = {
      name: formState.name.trim(),
      code: formState.code.trim(),
      templateType: formState.templateType,
      content: formState.content,
      description: formState.description.trim() || undefined,
      tags,
      defaultStatus: formState.defaultStatus,
    };

    onSave(createPayload, false);
  };

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl p-6 max-w-3xl w-full mx-4 max-h-[90vh] overflow-y-auto">
        <div className="flex justify-between items-center mb-6">
          <h2 className="text-2xl font-bold">
            {isEdit ? 'Editar Template' : 'Novo Template'}
          </h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
            ✕
          </button>
        </div>

        <div className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-2">Nome *</label>
              <input
                type="text"
                value={formState.name}
                onChange={(e) => handleChange('name', e.target.value)}
                className="input w-full"
                placeholder="Ex: NDA Padrão"
              />
              {errors.name && <p className="text-red-500 text-sm mt-1">{errors.name}</p>}
            </div>
            <div>
              <label className="block text-sm font-medium mb-2">Código *</label>
              <input
                type="text"
                value={formState.code}
                onChange={(e) => handleChange('code', e.target.value)}
                className="input w-full"
                placeholder="Ex: NDA-STD"
                disabled={isEdit}
              />
              {errors.code && <p className="text-red-500 text-sm mt-1">{errors.code}</p>}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label className="block text-sm font-medium mb-2">Tipo *</label>
              <select
                value={formState.templateType}
                onChange={(e) =>
                  handleChange('templateType', e.target.value as ContractTemplateType)
                }
                className="input w-full"
                disabled={isEdit}
              >
                {Object.entries(CONTRACT_TEMPLATE_TYPE_CONFIG).map(([key, config]) => (
                  <option key={key} value={key}>
                    {config.label}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-sm font-medium mb-2">Status Padrão</label>
              <select
                value={formState.defaultStatus}
                onChange={(e) =>
                  handleChange('defaultStatus', e.target.value as ContractStatus)
                }
                className="input w-full"
              >
                {Object.entries(CONTRACT_STATUS_CONFIG).map(([key, config]) => (
                  <option key={key} value={key}>
                    {config.label}
                  </option>
                ))}
              </select>
            </div>
            <div className="flex items-center gap-3 mt-6">
              <input
                type="checkbox"
                id="isActive"
                checked={formState.isActive}
                onChange={(e) => handleChange('isActive', e.target.checked)}
                className="w-4 h-4"
              />
              <label htmlFor="isActive" className="text-sm font-medium">
                Template ativo
              </label>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium mb-2">Descrição</label>
            <textarea
              value={formState.description}
              onChange={(e) => handleChange('description', e.target.value)}
              className="input w-full min-h-[80px]"
              placeholder="Descreva o template..."
            />
          </div>

          <div>
            <label className="block text-sm font-medium mb-2">Tags (separadas por vírgula)</label>
            <input
              type="text"
              value={formState.tags}
              onChange={(e) => handleChange('tags', e.target.value)}
              className="input w-full"
              placeholder="Ex: Confidencialidade, Padrão"
            />
          </div>

          <div>
            <label className="block text-sm font-medium mb-2">Conteúdo *</label>
            <textarea
              value={formState.content}
              onChange={(e) => handleChange('content', e.target.value)}
              className="input w-full min-h-[200px] font-mono"
              placeholder="Cole o HTML ou texto do template..."
            />
            {errors.content && (
              <p className="text-red-500 text-sm mt-1">{errors.content}</p>
            )}
          </div>

          <div className="flex gap-3 pt-4">
            <Button type="button" variant="secondary" onClick={onClose} className="flex-1">
              Cancelar
            </Button>
            <Button type="button" onClick={handleSubmit} className="flex-1" loading={isSaving}>
              {isEdit ? 'Salvar Alterações' : 'Criar Template'}
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}

interface PreviewModalProps {
  template: ContractTemplate | null;
  onClose: () => void;
}

function PreviewModal({ template, onClose }: PreviewModalProps) {
  if (!template) return null;

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
      <div className="bg-white rounded-xl p-6 max-w-4xl w-full mx-4 max-h-[90vh] overflow-y-auto">
        <div className="flex justify-between items-center mb-6">
          <div>
            <h2 className="text-2xl font-bold">Pré-visualização</h2>
            <p className="text-sm text-gray-500">{template.name}</p>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600">
            ✕
          </button>
        </div>

        {/* Variáveis detectadas */}
        <div className="mb-6">
          <VariablesList 
            content={template.content} 
            title="Variáveis deste template"
            showCount={true}
          />
        </div>

        {/* Conteúdo do template */}
        <div className="border border-gray-200 rounded-lg p-4 bg-gray-50">
          <div
            className="prose max-w-none"
            dangerouslySetInnerHTML={{ __html: sanitizeHtml(template.content) }}
          />
        </div>
      </div>
    </div>
  );
}
