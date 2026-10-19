import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Plus, Upload, Users, Search, ChevronLeft, ChevronRight } from 'lucide-react';
import { Button, Card, Spinner, StatCard } from '@/components/ui';
import { ShareholderCard, ShareholderFilters, ShareholderFormModal, ImportShareholdersModal } from '@/components/shareholders';
import { useShareholders, useDeleteShareholder } from '@/hooks/useShareholders';
import { useAuthStore } from '@/stores/authStore';
import { useConfirm } from '@/components/ui';
import {
  ShareholderType,
  ShareholderStatus,
  type Shareholder,
} from '@/types';

export default function ShareholdersListPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { confirm } = useConfirm();
  const isAdmin = useAuthStore((s) => s.user?.roles?.includes('Admin') ?? false);
  const [isImportOpen, setIsImportOpen] = useState(false);

  // Modal state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingShareholder, setEditingShareholder] = useState<Shareholder | undefined>(undefined);

  // Filter state
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<ShareholderType | null>(null);
  const [statusFilter, setStatusFilter] = useState<ShareholderStatus | null>(null);
  const [page, setPage] = useState(1);
  const pageSize = 12;

  // Build filters object
  const filters = useMemo(
    () => ({
      page,
      pageSize,
      search: search || undefined,
      type: typeFilter ?? undefined,
      status: statusFilter ?? undefined,
    }),
    [page, search, typeFilter, statusFilter]
  );

  // Query
  const { data, isLoading, isError, refetch } = useShareholders(filters);
  const deleteShareholderMutation = useDeleteShareholder();

  // Stats calculation
  const stats = useMemo(() => {
    if (!data?.items) return { total: 0, active: 0, pending: 0 };
    return {
      total: data.totalCount,
      active: data.items.filter((s) => s.status === ShareholderStatus.Active).length,
      pending: data.items.filter((s) => s.status === ShareholderStatus.Pending).length,
    };
  }, [data]);

  // Handlers
  const handleClearFilters = () => {
    setSearch('');
    setTypeFilter(null);
    setStatusFilter(null);
    setPage(1);
  };

  const handleView = (shareholder: Shareholder) => {
    navigate(`/shareholders/${shareholder.id}`);
  };

  const handleEdit = (shareholder: Shareholder) => {
    setEditingShareholder(shareholder);
    setIsModalOpen(true);
  };

  const handleDelete = async (shareholder: Shareholder) => {
    const confirmed = await confirm({
      title: t('shareholders.deleteShareholder'),
      message: t('shareholders.deleteConfirm', { name: shareholder.name }),
      confirmText: t('common.delete'),
      cancelText: t('common.cancel'),
      confirmVariant: 'danger',
    });

    if (confirmed) {
      deleteShareholderMutation.mutate(shareholder.id);
    }
  };

  const handleAddNew = () => {
    setEditingShareholder(undefined);
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setEditingShareholder(undefined);
  };

  // Pagination
  const totalPages = data?.totalPages || 1;
  const hasPreviousPage = page > 1;
  const hasNextPage = page < totalPages;

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div className="page-header">
        <div>
          <h1 className="page-title">{t('shareholders.title')}</h1>
          <p className="page-subtitle">
            {t('shareholders.manageSubtitle')}
          </p>
        </div>
        <div className="flex gap-2">
          {isAdmin && (
            <Button variant="secondary" icon={<Upload className="w-5 h-5" />} onClick={() => setIsImportOpen(true)}>
              Importar sócios
            </Button>
          )}
          <Button icon={<Plus className="w-5 h-5" />} onClick={handleAddNew}>
            {t('shareholders.addShareholder')}
          </Button>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <StatCard
           icon={<Users className="w-6 h-6 text-white" />}
          iconColor="bg-primary"
          value={stats.total}
          label={t('shareholders.totalShareholders')}
        />
        <StatCard
           icon={<Users className="w-6 h-6 text-white" />}
          iconColor="bg-success"
          value={stats.active}
          label={t('shareholders.activeShareholders')}
        />
        <StatCard
           icon={<Users className="w-6 h-6 text-white" />}
          iconColor="bg-warning"
          value={stats.pending}
          label={t('shareholders.pendingShareholders')}
        />
      </div>

      {/* Filters */}
      <ShareholderFilters
        search={search}
        onSearchChange={(val) => {
          setSearch(val);
          setPage(1);
        }}
        typeFilter={typeFilter}
        onTypeFilterChange={(val) => {
          setTypeFilter(val);
          setPage(1);
        }}
        statusFilter={statusFilter}
        onStatusFilterChange={(val) => {
          setStatusFilter(val);
          setPage(1);
        }}
        onClearFilters={handleClearFilters}
      />

      {/* Loading State */}
      {isLoading && (
        <div className="flex justify-center items-center py-12">
          <Spinner className="w-8 h-8" />
        </div>
      )}

      {/* Error State */}
      {isError && (
        <Card className="text-center py-12">
          <div className="w-16 h-16 bg-error-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <Users className="w-9 h-9 text-error-500" />
          </div>
          <h3 className="font-semibold text-primary mb-2">
            {t('shareholders.loadingError')}
          </h3>
          <p className="text-primary-500 mb-4">
            {t('shareholders.loadingErrorDetails')}
          </p>
          <Button variant="secondary" onClick={() => refetch()}>
            {t('shareholders.retryLoading')}
          </Button>
        </Card>
      )}

      {/* Cards Grid */}
      {!isLoading && !isError && data?.items && data.items.length > 0 && (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            {data.items.map((shareholder) => (
              <ShareholderCard
                key={shareholder.id}
                shareholder={shareholder}
                onView={handleView}
                onEdit={handleEdit}
                onDelete={handleDelete}
              />
            ))}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between pt-4">
              <p className="text-sm text-primary-500">
                {t('shareholders.showingPagination', {
                  from: (page - 1) * pageSize + 1,
                  to: Math.min(page * pageSize, data.totalCount),
                  total: data.totalCount,
                })}
              </p>
              <div className="flex items-center gap-2">
                <Button
                  variant="secondary"
                  size="sm"
                  disabled={!hasPreviousPage}
                  onClick={() => setPage((p) => p - 1)}
                >
                  <ChevronLeft className="w-5 h-5" />
                </Button>
                <span className="text-sm text-primary-600 px-2">
                  {t('shareholders.pageOf', { page, total: totalPages })}
                </span>
                <Button
                  variant="secondary"
                  size="sm"
                  disabled={!hasNextPage}
                  onClick={() => setPage((p) => p + 1)}
                >
                  <ChevronRight className="w-5 h-5" />
                </Button>
              </div>
            </div>
          )}
        </>
      )}

      {/* Empty State */}
      {!isLoading && !isError && data?.items && data.items.length === 0 && (
        <Card className="text-center py-12">
          <div className="w-16 h-16 bg-primary-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <Search className="w-9 h-9 text-primary-400" />
          </div>
          <h3 className="font-semibold text-primary mb-2">
            {t('shareholders.noShareholdersFound')}
          </h3>
          <p className="text-primary-500 mb-4">
            {search || typeFilter !== null || statusFilter !== null
              ? t('shareholders.tryAdjustFilters')
              : t('shareholders.addFirstShareholder')}
          </p>
          {search || typeFilter !== null || statusFilter !== null ? (
            <Button variant="secondary" onClick={handleClearFilters}>
              {t('shareholders.clearFilters')}
            </Button>
          ) : (
            <Button onClick={handleAddNew}>
              <Plus className="w-5 h-5 mr-2" />
              {t('shareholders.addShareholder')}
            </Button>
          )}
        </Card>
      )}

      {/* Modal de Criação/Edição */}
      <ShareholderFormModal
        isOpen={isModalOpen}
        onClose={handleCloseModal}
        shareholder={editingShareholder}
      />

      {isAdmin && <ImportShareholdersModal isOpen={isImportOpen} onClose={() => setIsImportOpen(false)} />}
    </div>
  );
}
