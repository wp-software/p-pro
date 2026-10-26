import { useState, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { Plus, Upload, Users, Search, ChevronLeft, ChevronRight, LayoutGrid, List, Download } from 'lucide-react';
import { Button, Card, Spinner, StatCard } from '@/components/ui';
import { ShareholderCard, ShareholderList, ShareholderFilters, ShareholderFormModal, ImportShareholdersModal } from '@/components/shareholders';
import { useShareholders, useDeleteShareholder } from '@/hooks/useShareholders';
import { exportShareholdersToCsv } from '@/utils/exportShareholders';
import { shareholderService } from '@/services/shareholderService';
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
  const [viewMode, setViewMode] = useState<'cards' | 'list'>(() =>
    localStorage.getItem('shareholders.viewMode') === 'list' ? 'list' : 'cards'
  );
  const changeViewMode = (mode: 'cards' | 'list') => {
    setViewMode(mode);
    localStorage.setItem('shareholders.viewMode', mode);
  };
  const isAdmin = useAuthStore((s) => s.user?.roles?.includes('Admin') ?? false);
  const [isImportOpen, setIsImportOpen] = useState(false);
  const [isExporting, setIsExporting] = useState(false);

  // Modal state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingShareholder, setEditingShareholder] = useState<Shareholder | undefined>(undefined);

  // Filter state
  const [search, setSearch] = useState('');
  const [typeFilter, setTypeFilter] = useState<ShareholderType | null>(null);
  const [statusFilter, setStatusFilter] = useState<ShareholderStatus | null>(null);
  const [sortDirection, setSortDirection] = useState<'' | 'asc' | 'desc'>('');
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
      sortDirection: sortDirection || undefined,
    }),
    [page, search, typeFilter, statusFilter, sortDirection]
  );

  // Query
  const { data, isLoading, isError, refetch } = useShareholders(filters);
  const deleteShareholderMutation = useDeleteShareholder();

  // Stats calculation
  const countFilters = (status: ShareholderStatus) => ({
    page: 1,
    pageSize: 1,
    search: search || undefined,
    type: typeFilter ?? undefined,
    status,
  });
  const { data: activeData } = useShareholders(countFilters(ShareholderStatus.Active));
  const { data: pendingData } = useShareholders(countFilters(ShareholderStatus.Pending));
  const stats = {
    total: data?.totalCount ?? 0,
    active: activeData?.totalCount ?? 0,
    pending: pendingData?.totalCount ?? 0,
  };

  // Handlers
  const handleClearFilters = () => {
    setSearch('');
    setTypeFilter(null);
    setStatusFilter(null);
    setPage(1);
  };

  // Exporta todos os sócios que atendem aos filtros/ordenação atuais, não só a página visível
  const handleExport = async () => {
    setIsExporting(true);
    try {
      const all = await shareholderService.getAllFiltered({
        search: filters.search,
        type: filters.type,
        status: filters.status,
        sortDirection: filters.sortDirection,
      });
      exportShareholdersToCsv(all);
    } catch {
      alert('Não foi possível exportar os sócios. Tente novamente.');
    } finally {
      setIsExporting(false);
    }
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

      <div className="flex justify-end items-center gap-3">
        {viewMode === 'list' && (
          <Button
            variant="secondary"
            icon={<Download className="w-5 h-5" />}
            onClick={handleExport}
            disabled={!data?.items?.length || isExporting}
          >
            {isExporting ? 'Exportando...' : 'Exportar'}
          </Button>
        )}
        <select
          aria-label="Ordenar sócios"
          value={sortDirection}
          onChange={(e) => {
            setSortDirection(e.target.value as '' | 'asc' | 'desc');
            setPage(1);
          }}
          className="rounded-lg border border-primary-200 bg-white px-3 py-2 text-sm text-primary-700"
        >
          <option value="">Mais recentes</option>
          <option value="asc">Nome (A–Z)</option>
          <option value="desc">Nome (Z–A)</option>
        </select>
        <div className="inline-flex rounded-lg border border-primary-200 overflow-hidden">
          <button
            type="button"
            title="Cards"
            aria-pressed={viewMode === 'cards'}
            onClick={() => changeViewMode('cards')}
            className={`p-2 ${viewMode === 'cards' ? 'bg-primary text-white' : 'text-primary-500 hover:bg-primary-50'}`}
          >
            <LayoutGrid className="w-5 h-5" />
          </button>
          <button
            type="button"
            title="Lista"
            aria-pressed={viewMode === 'list'}
            onClick={() => changeViewMode('list')}
            className={`p-2 ${viewMode === 'list' ? 'bg-primary text-white' : 'text-primary-500 hover:bg-primary-50'}`}
          >
            <List className="w-5 h-5" />
          </button>
        </div>
      </div>

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
          {viewMode === 'list' ? (
            <ShareholderList shareholders={data.items} onView={handleView} onEdit={handleEdit} />
          ) : (
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
          )}

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
