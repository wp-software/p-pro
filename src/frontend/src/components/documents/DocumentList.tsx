import { useState } from 'react';
import { Search, ChevronLeft, ChevronRight } from 'lucide-react';
import { ConfirmDialog, Spinner, useConfirm } from '@/components/ui';
import { useDocuments, useVerifyDocument, useDeleteDocument } from '@/hooks';
import { type DocumentFilters } from '@/types';
import DocumentCard from './DocumentCard';

// ─── Props ────────────────────────────────────────────────────────────────────

export interface DocumentListProps {
  companyId: string;
  referenceType?: string;
  referenceId?: string;
  readonly?: boolean;
  allowedCategories?: string[];
}

// ─── Component ────────────────────────────────────────────────────────────────

export default function DocumentList({
  companyId,
  referenceType,
  referenceId,
  readonly = false,
}: DocumentListProps) {
  const { confirm, confirmState, handleCancel } = useConfirm();
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const pageSize = 8;

  const filters: DocumentFilters = {
    companyId,
    page,
    pageSize,
    search: search || undefined,
    entityType: referenceType,
    entityId: referenceId,
  };

  const { data, isLoading, refetch } = useDocuments(filters);
  const verifyDoc = useVerifyDocument();
  const deleteDoc = useDeleteDocument();

  const [verifyingId, setVerifyingId] = useState<string | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  async function handleVerify(id: string) {
    setVerifyingId(id);
    await verifyDoc.mutateAsync(id).finally(() => setVerifyingId(null));
  }

  async function handleDelete(id: string) {
    const document = items.find((item) => item.id === id);
    if (!document) return;
    const confirmed = await confirm({
      title: 'Excluir documento',
      message: `Tem certeza que deseja excluir "${document.name}"?`,
      confirmText: 'Excluir',
      confirmVariant: 'danger',
    });
    if (!confirmed) return;
    setDeletingId(id);
    deleteDoc.mutate(id, {
      onSettled: () => {
        setDeletingId(null);
        refetch();
      },
    });
  }

  const totalPages = data?.totalPages ?? 1;
  const items = data?.items ?? [];

  return (
    <div className="space-y-3">
      <ConfirmDialog {...confirmState} onCancel={handleCancel} />
      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
        <input
          type="text"
          placeholder="Buscar documentos..."
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          className="w-full pl-9 pr-4 py-2 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>

      {/* List */}
      {isLoading ? (
        <div className="flex justify-center py-8">
          <Spinner className="w-6 h-6" />
        </div>
      ) : items.length === 0 ? (
        <div className="text-center py-10 text-gray-400 text-sm">
          {search ? 'Nenhum documento encontrado.' : 'Nenhum documento cadastrado.'}
        </div>
      ) : (
        <div className="space-y-2">
          {items.map((doc) => (
            <DocumentCard
              key={doc.id}
              document={doc}
              readonly={readonly}
              onVerify={handleVerify}
              onDelete={handleDelete}
              isVerifying={verifyingId === doc.id}
              isDeleting={deletingId === doc.id}
            />
          ))}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between pt-2">
          <span className="text-xs text-gray-400">
            {data?.total ?? 0} documento(s)
          </span>
          <div className="flex items-center gap-1">
            <button
              onClick={() => setPage((p) => Math.max(1, p - 1))}
              disabled={page === 1}
              className="p-1.5 rounded-lg hover:bg-gray-100 disabled:opacity-40"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <span className="text-xs text-gray-600 px-2">{page}/{totalPages}</span>
            <button
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="p-1.5 rounded-lg hover:bg-gray-100 disabled:opacity-40"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
