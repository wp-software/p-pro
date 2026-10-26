import { useState } from 'react';
import { Plus, Search, FileText, ShieldCheck, Trash2 } from 'lucide-react';
import {
  Button, Card, Spinner, Badge, StatCard,
  ConfirmDialog, useConfirm,
} from '@/components/ui';
import {
  useDocuments,
  useCreateDocument,
  useVerifyDocument,
  useDeleteDocument,
} from '@/hooks';
import { useClientStore } from '@/stores/clientStore';
import {
  documentVisibilityLabels,
  documentVisibilityColors,
  DocumentVisibility,
  type DocumentFilters,
  type CreateDocumentRequest,
} from '@/types';
import { formatDate } from '@/utils/format';
import toast from 'react-hot-toast';

const VISIBILITY_OPTIONS = [
  { value: '', label: 'Todas' },
  { value: DocumentVisibility.Public, label: 'Público' },
  { value: DocumentVisibility.Internal, label: 'Interno' },
  { value: DocumentVisibility.Shareholder, label: 'Acionista' },
  { value: DocumentVisibility.Admin, label: 'Administrador' },
];

const TYPE_OPTIONS = [
  '', 'term_sheet', 'sha', 'investment_contract', 'founding_agreement',
  'ip_assignment', 'employment_contract', 'vesting_agreement', 'board_resolution',
  'financial_report', 'valuation_report', 'other',
];

const TYPE_LABELS: Record<string, string> = {
  term_sheet: 'Term Sheet', sha: 'SHA', investment_contract: 'Contrato Invest.',
  founding_agreement: 'Acordo Fundadores', ip_assignment: 'Cessão IP',
  employment_contract: 'Contrato Emprego', vesting_agreement: 'Acordo Vesting',
  board_resolution: 'Resolução Conselho', financial_report: 'Relatório Financeiro',
  valuation_report: 'Relatório Valuation', other: 'Outro',
};

const SOURCE_LABELS: Record<string, string> = {
  dataroom: 'Data Room',
  financial: 'Financeiro',
  documents: 'Documents',
};

const SOURCE_COLORS: Record<string, string> = {
  dataroom: 'bg-blue-50 text-blue-700',
  financial: 'bg-green-50 text-green-700',
  documents: 'bg-gray-100 text-gray-600',
};

function formatFileSize(bytes?: number) {
  if (!bytes) return '—';
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

const EMPTY_FORM: CreateDocumentRequest = {
  companyId: '', name: '', documentType: '', fileName: '', fileSizeBytes: 0,
  mimeType: '', storagePath: '', visibility: DocumentVisibility.Internal,
};

export default function DocumentsPage() {
  const { selectedCompanyId } = useClientStore();
  const [search, setSearch] = useState('');
  const [visibilityFilter, setVisibilityFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [page, setPage] = useState(1);
  const [showNew, setShowNew] = useState(false);
  const [form, setForm] = useState<CreateDocumentRequest>(EMPTY_FORM);
  const { confirm, confirmState, handleCancel } = useConfirm();

  const filters: DocumentFilters = {
    companyId: selectedCompanyId || undefined,
    search: search || undefined,
    visibility: (visibilityFilter as DocumentVisibility) || undefined,
    documentType: typeFilter || undefined,
    page,
    pageSize: 15,
  };

  const { data, isLoading } = useDocuments(filters);
  const createDoc = useCreateDocument();
  const verifyDoc = useVerifyDocument();
  const deleteDoc = useDeleteDocument();

  async function handleCreate() {
    if (!form.name || !form.documentType || !form.storagePath) {
      toast.error('Preencha nome, tipo e caminho do arquivo.');
      return;
    }
    await createDoc.mutateAsync({ ...form, companyId: selectedCompanyId! });
    setForm(EMPTY_FORM);
    setShowNew(false);
  }

  async function handleDelete(docId: string, name: string) {
    const ok = await confirm({
      title: 'Excluir documento',
      message: `Tem certeza que deseja excluir "${name}"?`,
      confirmText: 'Excluir',
      confirmVariant: 'danger',
    });
    if (ok) deleteDoc.mutate(docId);
  }

  const totalCount = data?.total ?? 0;
  const verified = data?.items.filter((d) => d.isVerified).length ?? 0;
  const totalPages = data ? Math.ceil(data.total / 15) : 1;

  return (
    <div className="space-y-6 animate-fade-in">
      <ConfirmDialog {...confirmState} onCancel={handleCancel} />
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Documentos</h1>
          <p className="text-sm text-gray-500 mt-1">Gestão de documentos legais e operacionais</p>
        </div>
        <Button icon={<Plus size={16} />} onClick={() => setShowNew(!showNew)}>
          Novo Documento
        </Button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <StatCard icon={<FileText size={20} />} iconColor="bg-blue-100 text-blue-600" label="Total" value={String(totalCount)} />
        <StatCard icon={<ShieldCheck size={20} />} iconColor="bg-green-100 text-green-600" label="Verificados" value={String(verified)} />
        <StatCard icon={<FileText size={16} />} iconColor="bg-purple-100 text-purple-600" label="Públicos" value={String(data?.items.filter((d) => d.visibility === DocumentVisibility.Public).length ?? 0)} />
        <StatCard icon={<ShieldCheck size={16} />} iconColor="bg-gray-100 text-gray-500" label="Admin" value={String(data?.items.filter((d) => d.visibility === DocumentVisibility.Admin).length ?? 0)} />
      </div>

      {/* Inline New Form */}
      {showNew && (
        <Card>
          <div className="p-5 space-y-4">
            <h2 className="text-sm font-semibold text-gray-800">Novo Documento</h2>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Nome *</label>
                <input
                  className="input-base w-full"
                  value={form.name}
                  onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))}
                  placeholder="Ex.: SHA Série A"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Tipo *</label>
                <select
                  className="input-base w-full"
                  value={form.documentType}
                  onChange={(e) => setForm((f) => ({ ...f, documentType: e.target.value }))}
                >
                  <option value="">Selecione...</option>
                  {TYPE_OPTIONS.filter(Boolean).map((t) => (
                    <option key={t} value={t}>{TYPE_LABELS[t] ?? t}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Nome do Arquivo</label>
                <input
                  className="input-base w-full"
                  value={form.fileName}
                  onChange={(e) => setForm((f) => ({ ...f, fileName: e.target.value }))}
                  placeholder="contract.pdf"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Caminho (Storage) *</label>
                <input
                  className="input-base w-full"
                  value={form.storagePath}
                  onChange={(e) => setForm((f) => ({ ...f, storagePath: e.target.value }))}
                  placeholder="/uploads/..."
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">MIME Type</label>
                <input
                  className="input-base w-full"
                  value={form.mimeType}
                  onChange={(e) => setForm((f) => ({ ...f, mimeType: e.target.value }))}
                  placeholder="application/pdf"
                />
              </div>
              <div>
                <label className="block text-xs font-medium text-gray-600 mb-1">Visibilidade</label>
                <select
                  className="input-base w-full"
                  value={form.visibility}
                  onChange={(e) => setForm((f) => ({ ...f, visibility: e.target.value as DocumentVisibility }))}
                >
                  {VISIBILITY_OPTIONS.filter((o) => o.value).map((o) => (
                    <option key={o.value} value={o.value}>{o.label}</option>
                  ))}
                </select>
              </div>
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="secondary" size="sm" onClick={() => { setShowNew(false); setForm(EMPTY_FORM); }}>
                Cancelar
              </Button>
              <Button size="sm" onClick={handleCreate} loading={createDoc.isPending}>
                Criar Documento
              </Button>
            </div>
          </div>
        </Card>
      )}

      {/* Filters */}
      <Card>
        <div className="p-4 flex flex-col sm:flex-row gap-3">
          <div className="relative flex-1">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              className="input-base w-full pl-8"
              placeholder="Buscar por nome..."
              value={search}
              onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            />
          </div>
          <select
            className="input-base"
            value={typeFilter}
            onChange={(e) => { setTypeFilter(e.target.value); setPage(1); }}
          >
            <option value="">Todos os tipos</option>
            {TYPE_OPTIONS.filter(Boolean).map((t) => (
              <option key={t} value={t}>{TYPE_LABELS[t] ?? t}</option>
            ))}
          </select>
          <select
            className="input-base"
            value={visibilityFilter}
            onChange={(e) => { setVisibilityFilter(e.target.value); setPage(1); }}
          >
            {VISIBILITY_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
        </div>
      </Card>

      {/* Table */}
      <Card>
        {isLoading ? (
          <div className="flex justify-center py-12">
            <Spinner />
          </div>
        ) : !data?.items.length ? (
          <div className="text-center py-12 text-gray-400">
            <FileText size={40} className="mx-auto mb-3 opacity-30" />
            <p className="text-sm">Nenhum documento encontrado.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 text-xs text-gray-400 uppercase tracking-wide">
                  <th className="px-4 py-3 text-left">Nome</th>
                  <th className="px-4 py-3 text-left">Tipo</th>
                  <th className="px-4 py-3 text-left">Origem</th>
                  <th className="px-4 py-3 text-left">Tamanho</th>
                  <th className="px-4 py-3 text-left">Visibilidade</th>
                  <th className="px-4 py-3 text-left">Verificado</th>
                  <th className="px-4 py-3 text-left">Criado em</th>
                  <th className="px-4 py-3 text-right">Ações</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {data.items.map((doc) => (
                  <tr key={doc.id} className="hover:bg-gray-50 transition-colors">
                    <td className="px-4 py-3 font-medium text-gray-900">{doc.name}</td>
                    <td className="px-4 py-3 text-gray-500">
                      {TYPE_LABELS[doc.documentType] ?? doc.documentType}
                    </td>
                    <td className="px-4 py-3">
                      <Badge className={SOURCE_COLORS[doc.source ?? 'documents']}>
                        {SOURCE_LABELS[doc.source ?? 'documents']}
                      </Badge>
                    </td>
                    <td className="px-4 py-3 text-gray-500">
                      {formatFileSize(doc.fileSizeBytes)}
                    </td>
                    <td className="px-4 py-3">
                      <Badge className={documentVisibilityColors[doc.visibility] ?? ''}>
                        {documentVisibilityLabels[doc.visibility] ?? doc.visibility}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      {doc.isVerified ? (
                        <span className="text-green-600 flex items-center gap-1">
                          <ShieldCheck size={14} /> Sim
                        </span>
                      ) : (
                        <span className="text-gray-400">Não</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-gray-500">{formatDate(doc.createdAt)}</td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex items-center justify-end gap-2">
                        {!doc.isVerified && (
                          <button
                            onClick={() => verifyDoc.mutate(doc.id)}
                            className="p-1.5 rounded hover:bg-green-50 text-green-600 transition-colors"
                            title="Verificar"
                          >
                            <ShieldCheck size={14} />
                          </button>
                        )}
                        <button
                          onClick={() => handleDelete(doc.id, doc.name)}
                          disabled={deleteDoc.isPending}
                          className="p-1.5 rounded hover:bg-red-50 text-gray-400 hover:text-red-600 transition-colors disabled:opacity-50"
                          title="Excluir"
                          aria-label={`Excluir ${doc.name}`}
                        >
                          <Trash2 size={14} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="px-4 py-3 border-t border-gray-100 flex items-center justify-between">
            <span className="text-xs text-gray-400">{totalCount} documentos</span>
            <div className="flex gap-1">
              <button
                disabled={page === 1}
                onClick={() => setPage((p) => p - 1)}
                className="px-3 py-1.5 text-xs rounded border disabled:opacity-40 hover:bg-gray-50"
              >
                Anterior
              </button>
              <span className="px-3 py-1.5 text-xs text-gray-500">
                {page}/{totalPages}
              </span>
              <button
                disabled={page === totalPages}
                onClick={() => setPage((p) => p + 1)}
                className="px-3 py-1.5 text-xs rounded border disabled:opacity-40 hover:bg-gray-50"
              >
                Próxima
              </button>
            </div>
          </div>
        )}
      </Card>
    </div>
  );
}
