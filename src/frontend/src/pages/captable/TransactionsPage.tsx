import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Plus,
  ArrowRightLeft,
  Trash2,
  RefreshCw,
  Filter,
  Calendar,
  Search,
  PieChart,
  ArrowUpCircle,
  ArrowDownCircle,
  XCircle,
  Repeat,
  ArrowLeft,
} from 'lucide-react';
import { Button, Card } from '@/components/ui';
import { useTransactions } from '@/hooks/useCapTable';
import { useClientStore } from '@/stores/clientStore';
import IssueSharesModal from '@/components/captable/IssueSharesModal';
import TransferSharesModal from '@/components/captable/TransferSharesModal';
import CancelSharesModal from '@/components/captable/CancelSharesModal';
import {
  TransactionType,
  getTransactionTypeLabel,
  getTransactionTypeColor,
  type TransactionFilters,
} from '@/types';

export default function TransactionsPage() {
  const navigate = useNavigate();
  const { selectedCompanyId: rawCompanyId } = useClientStore();
  const selectedCompanyId = rawCompanyId === '__all__' ? null : rawCompanyId;

  // Modals
  const [showIssueModal, setShowIssueModal] = useState(false);
  const [showTransferModal, setShowTransferModal] = useState(false);
  const [showCancelModal, setShowCancelModal] = useState(false);

  // Filters
  const [filters, setFilters] = useState<TransactionFilters>({
    companyId: selectedCompanyId ?? undefined,
    pageNumber: 1,
    pageSize: 20,
  });
  const [showFilters, setShowFilters] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [transactionTypeFilter, setTransactionTypeFilter] = useState<string>('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');

  const { data, isLoading, isError, refetch, isFetching } = useTransactions({
    ...filters,
    companyId: selectedCompanyId ?? undefined,
    transactionType: transactionTypeFilter ? Number(transactionTypeFilter) : undefined,
    fromDate: fromDate || undefined,
    toDate: toDate || undefined,
  });

  const handleRefresh = () => {
    refetch();
  };

  const handleApplyFilters = () => {
    setFilters((prev) => ({
      ...prev,
      pageNumber: 1,
    }));
    refetch();
  };

  const handleClearFilters = () => {
    setSearchTerm('');
    setTransactionTypeFilter('');
    setFromDate('');
    setToDate('');
    setFilters({
      companyId: selectedCompanyId ?? undefined,
      pageNumber: 1,
      pageSize: 20,
    });
  };

  const getTransactionIcon = (type: TransactionType) => {
    switch (type) {
      case TransactionType.Issue:
        return <ArrowUpCircle className="w-4 h-4 text-green-600" />;
      case TransactionType.Transfer:
        return <ArrowRightLeft className="w-4 h-4 text-blue-600" />;
      case TransactionType.Cancel:
        return <XCircle className="w-4 h-4 text-red-600" />;
      case TransactionType.Convert:
        return <Repeat className="w-4 h-4 text-purple-600" />;
      default:
        return <ArrowDownCircle className="w-4 h-4 text-gray-600" />;
    }
  };

  const getTransactionBadgeClass = (type: TransactionType) => {
    const color = getTransactionTypeColor(type);
    const colorMap: Record<string, string> = {
      green: 'bg-green-100 text-green-800',
      blue: 'bg-blue-100 text-blue-800',
      red: 'bg-red-100 text-red-800',
      purple: 'bg-purple-100 text-purple-800',
      orange: 'bg-orange-100 text-orange-800',
      gray: 'bg-gray-100 text-gray-800',
    };
    return colorMap[color] || 'bg-gray-100 text-gray-800';
  };

  if (!selectedCompanyId) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Card className="p-8 text-center">
          <PieChart className="w-12 h-12 text-gray-400 mx-auto mb-4" />
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Nenhuma empresa selecionada</h2>
          <p className="text-gray-500">Selecione uma empresa para visualizar as movimentações.</p>
        </Card>
      </div>
    );
  }

  if (isError) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Card className="p-8 text-center">
          <div className="text-red-500 mb-4">
            <PieChart className="w-12 h-12 mx-auto" />
          </div>
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Erro ao carregar dados</h2>
          <p className="text-gray-500 mb-4">Não foi possível carregar as movimentações.</p>
          <Button onClick={handleRefresh}>Tentar novamente</Button>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div className="page-header">
        <div className="flex items-center gap-4">
          <Button
            variant="ghost"
            onClick={() => navigate('/cap-table')}
            icon={<ArrowLeft className="w-4 h-4" />}
            className="!p-2"
          >
            Voltar
          </Button>
          <div>
            <h1 className="page-title">Movimentações</h1>
            <p className="page-subtitle">
              Histórico de transações de participações societárias
            </p>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <Button
            variant="secondary"
            onClick={() => setShowFilters(!showFilters)}
            icon={<Filter className="w-4 h-4" />}
          >
            Filtros
          </Button>

          <Button
            variant="secondary"
            onClick={handleRefresh}
            loading={isFetching}
            icon={<RefreshCw className="w-4 h-4" />}
          >
            Atualizar
          </Button>

          <div className="h-8 w-px bg-gray-200" />

          <Button
            variant="success"
            onClick={() => setShowIssueModal(true)}
            icon={<Plus className="w-4 h-4" />}
          >
            Emitir
          </Button>

          <Button
            variant="primary"
            onClick={() => setShowTransferModal(true)}
            icon={<ArrowRightLeft className="w-4 h-4" />}
          >
            Transferir
          </Button>

          <Button
            variant="danger"
            onClick={() => setShowCancelModal(true)}
            icon={<Trash2 className="w-4 h-4" />}
          >
            Cancelar
          </Button>
        </div>
      </div>

      {/* Filters */}
      {showFilters && (
        <Card className="p-4">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            {/* Search */}
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="text"
                placeholder="Buscar..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="input pl-10"
              />
            </div>

            {/* Transaction Type */}
            <select
              value={transactionTypeFilter}
              onChange={(e) => setTransactionTypeFilter(e.target.value)}
              className="input"
            >
              <option value="">Todos os tipos</option>
              <option value={TransactionType.Issue}>Emissão</option>
              <option value={TransactionType.Transfer}>Transferência</option>
              <option value={TransactionType.Cancel}>Cancelamento</option>
              <option value={TransactionType.Convert}>Conversão</option>
            </select>

            {/* From Date */}
            <div className="relative">
              <Calendar className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="date"
                value={fromDate}
                onChange={(e) => setFromDate(e.target.value)}
                className="input pl-10"
                placeholder="De"
              />
            </div>

            {/* To Date */}
            <div className="relative">
              <Calendar className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="date"
                value={toDate}
                onChange={(e) => setToDate(e.target.value)}
                className="input pl-10"
                placeholder="Até"
              />
            </div>
          </div>

          <div className="flex justify-end gap-2 mt-4">
            <Button variant="ghost" size="sm" onClick={handleClearFilters}>
              Limpar
            </Button>
            <Button variant="primary" size="sm" onClick={handleApplyFilters}>
              Aplicar Filtros
            </Button>
          </div>
        </Card>
      )}

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-gray-100 rounded-lg flex items-center justify-center">
              <PieChart className="w-5 h-5 text-gray-600" />
            </div>
            <div>
              <p className="text-sm text-gray-500">Total de Transações</p>
              <p className="text-xl font-semibold">{data?.totalCount ?? 0}</p>
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-green-100 rounded-lg flex items-center justify-center">
              <ArrowUpCircle className="w-5 h-5 text-green-600" />
            </div>
            <div>
              <p className="text-sm text-gray-500">Ações Transacionadas</p>
              <p className="text-xl font-semibold">
                {(data?.totalQuantity ?? 0).toLocaleString('pt-BR')}
              </p>
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-blue-100 rounded-lg flex items-center justify-center">
              <ArrowRightLeft className="w-5 h-5 text-blue-600" />
            </div>
            <div>
              <p className="text-sm text-gray-500">Valor Total</p>
              <p className="text-xl font-semibold">
                {(data?.totalValue ?? 0).toLocaleString('pt-BR', {
                  style: 'currency',
                  currency: 'BRL',
                })}
              </p>
            </div>
          </div>
        </Card>

        <Card className="p-4">
          <div className="flex items-center gap-3">
            <div className="w-10 h-10 bg-purple-100 rounded-lg flex items-center justify-center">
              <Calendar className="w-5 h-5 text-purple-600" />
            </div>
            <div>
              <p className="text-sm text-gray-500">Última Transação</p>
              <p className="text-xl font-semibold">
                {data?.items?.[0]
                  ? new Date(data.items[0].referenceDate).toLocaleDateString('pt-BR')
                  : '-'}
              </p>
            </div>
          </div>
        </Card>
      </div>

      {/* Transactions Table */}
      <Card className="overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Data
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Tipo
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  De / Para
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Classe
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Quantidade
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Preço/Ação
                </th>
                <th className="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Valor Total
                </th>
                <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                  Nº Transação
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-200">
              {isLoading ? (
                [...Array(5)].map((_, i) => (
                  <tr key={i} className="animate-pulse">
                    <td className="px-4 py-4">
                      <div className="h-4 bg-gray-200 rounded w-20" />
                    </td>
                    <td className="px-4 py-4">
                      <div className="h-4 bg-gray-200 rounded w-24" />
                    </td>
                    <td className="px-4 py-4">
                      <div className="h-4 bg-gray-200 rounded w-32" />
                    </td>
                    <td className="px-4 py-4">
                      <div className="h-4 bg-gray-200 rounded w-16" />
                    </td>
                    <td className="px-4 py-4">
                      <div className="h-4 bg-gray-200 rounded w-20 ml-auto" />
                    </td>
                    <td className="px-4 py-4">
                      <div className="h-4 bg-gray-200 rounded w-20 ml-auto" />
                    </td>
                    <td className="px-4 py-4">
                      <div className="h-4 bg-gray-200 rounded w-24 ml-auto" />
                    </td>
                    <td className="px-4 py-4">
                      <div className="h-4 bg-gray-200 rounded w-16" />
                    </td>
                  </tr>
                ))
              ) : data?.items && data.items.length > 0 ? (
                data.items.map((transaction) => (
                  <tr key={transaction.id} className="hover:bg-gray-50">
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-900">
                      {new Date(transaction.referenceDate).toLocaleDateString('pt-BR')}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap">
                      <div className="flex items-center gap-2">
                        {getTransactionIcon(transaction.transactionType)}
                        <span
                          className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getTransactionBadgeClass(
                            transaction.transactionType
                          )}`}
                        >
                          {getTransactionTypeLabel(transaction.transactionType)}
                        </span>
                      </div>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm">
                      {transaction.transactionType === TransactionType.Transfer ? (
                        <div className="flex items-center gap-1">
                          <span className="text-gray-500">{transaction.fromShareholderName}</span>
                          <ArrowRightLeft className="w-3 h-3 text-gray-400" />
                          <span className="text-gray-900">{transaction.toShareholderName}</span>
                        </div>
                      ) : transaction.transactionType === TransactionType.Issue ? (
                        <span className="text-gray-900">{transaction.toShareholderName}</span>
                      ) : (
                        <span className="text-gray-900">{transaction.fromShareholderName}</span>
                      )}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap">
                      <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-800">
                        {transaction.shareClassCode}
                      </span>
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-right text-gray-900 font-medium">
                      {transaction.quantity.toLocaleString('pt-BR')}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-right text-gray-500">
                      {transaction.pricePerShare.toLocaleString('pt-BR', {
                        style: 'currency',
                        currency: 'BRL',
                      })}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-right text-gray-900 font-semibold">
                      {transaction.totalValue.toLocaleString('pt-BR', {
                        style: 'currency',
                        currency: 'BRL',
                      })}
                    </td>
                    <td className="px-4 py-4 whitespace-nowrap text-sm text-gray-500">
                      {transaction.transactionNumber || '-'}
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={8} className="px-4 py-12 text-center text-gray-500">
                    <PieChart className="w-12 h-12 mx-auto mb-3 text-gray-300" />
                    <p className="text-lg font-medium">Nenhuma movimentação encontrada</p>
                    <p className="text-sm mt-1">
                      Comece emitindo, transferindo ou cancelando ações.
                    </p>
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>

        {/* Pagination */}
        {data && data.totalPages > 1 && (
          <div className="px-4 py-3 border-t border-gray-200 flex items-center justify-between">
            <div className="text-sm text-gray-500">
              Mostrando{' '}
              <span className="font-medium">
                {(data.pageNumber - 1) * data.pageSize + 1}
              </span>{' '}
              a{' '}
              <span className="font-medium">
                {Math.min(data.pageNumber * data.pageSize, data.totalCount)}
              </span>{' '}
              de <span className="font-medium">{data.totalCount}</span> resultados
            </div>
            <div className="flex gap-2">
              <Button
                variant="secondary"
                size="sm"
                disabled={!data.hasPreviousPage}
                onClick={() =>
                  setFilters((prev) => ({
                    ...prev,
                    pageNumber: (prev.pageNumber ?? 1) - 1,
                  }))
                }
              >
                Anterior
              </Button>
              <Button
                variant="secondary"
                size="sm"
                disabled={!data.hasNextPage}
                onClick={() =>
                  setFilters((prev) => ({
                    ...prev,
                    pageNumber: (prev.pageNumber ?? 1) + 1,
                  }))
                }
              >
                Próxima
              </Button>
            </div>
          </div>
        )}
      </Card>

      {/* Modals */}
      <IssueSharesModal
        isOpen={showIssueModal}
        onClose={() => setShowIssueModal(false)}
        companyId={selectedCompanyId}
        onSuccess={() => refetch()}
      />

      <TransferSharesModal
        isOpen={showTransferModal}
        onClose={() => setShowTransferModal(false)}
        companyId={selectedCompanyId}
        onSuccess={() => refetch()}
      />

      <CancelSharesModal
        isOpen={showCancelModal}
        onClose={() => setShowCancelModal(false)}
        companyId={selectedCompanyId}
        onSuccess={() => refetch()}
      />
    </div>
  );
}
