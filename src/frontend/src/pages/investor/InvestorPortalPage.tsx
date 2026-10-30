import { useNavigate } from 'react-router-dom';
import {
  TrendingUp,
  FileText,
  Users,
  MessageSquare,
  ExternalLink,
} from 'lucide-react';
import { Card, StatCard, Spinner } from '@/components/ui';
import { useInvestorSummary, usePortalCommunications } from '@/hooks/useInvestorPortal';
import { useShareholderLink } from '@/hooks/useShareholderLink';
import { ShareholderNotLinkedNotice } from '@/components/ui/ShareholderNotLinkedNotice';
import { formatCurrency, formatDate } from '@/utils/format';
import { cn } from '@/utils/cn';

const TYPE_LABELS: Record<string, string> = {
  announcement: 'Anúncio',
  update: 'Atualização',
  report: 'Relatório',
  alert: 'Alerta',
  invitation: 'Convite',
};

const TYPE_COLORS: Record<string, string> = {
  announcement: 'bg-blue-100 text-blue-700',
  update: 'bg-green-100 text-green-700',
  report: 'bg-purple-100 text-purple-700',
  alert: 'bg-red-100 text-red-700',
  invitation: 'bg-amber-100 text-amber-700',
};

export default function InvestorPortalPage() {
  const navigate = useNavigate();
  const link = useShareholderLink();
  const { data: summary, isLoading: summaryLoading } = useInvestorSummary();
  const { data: commData, isLoading: commLoading } = usePortalCommunications({ pageSize: 3 });

  const communications = commData?.items ?? [];

  if (link.isLoading || summaryLoading) {
    return (
      <div className="flex justify-center py-20">
        <Spinner />
      </div>
    );
  }

  if (!link.isLinked) {
    return <ShareholderNotLinkedNotice />;
  }

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Hero banner */}
      <div className="bg-gradient-to-br from-primary to-primary-700 rounded-2xl p-6 text-white">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-white/70 text-sm mb-1">Bem-vindo(a)</p>
            <h1 className="text-2xl font-bold">{summary?.investorName || 'Investidor'}</h1>
            <p className="text-white/70 text-sm mt-1">{summary?.companyName}</p>
          </div>
          <div className="text-right">
            <p className="text-white/70 text-sm mb-1">Participação</p>
            <p className="text-3xl font-bold">
              {summary ? `${summary.ownershipPercentage.toFixed(2)}%` : '—'}
            </p>
          </div>
        </div>
        {summary && (
          <div className="mt-4 pt-4 border-t border-white/20">
            <p className="text-sm text-white/70">Valor estimado da participação</p>
            <p className="text-xl font-semibold">{formatCurrency(summary.estimatedValue)}</p>
          </div>
        )}
      </div>

      {/* Stats */}
      {summary && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <StatCard
            icon={<Users className="w-5 h-5 text-white" />}
            iconColor="bg-blue-500"
            value={summary.totalShares.toLocaleString('pt-BR')}
            label="Total de ações"
          />
          <StatCard
            icon={<TrendingUp className="w-5 h-5 text-white" />}
            iconColor="bg-green-500"
            value={formatCurrency(summary.currentValuation)}
            label="Valuation atual"
          />
          <StatCard
            icon={<FileText className="w-5 h-5 text-white" />}
            iconColor="bg-purple-500"
            value={summary.documentsCount}
            label="Documentos disponíveis"
          />
        </div>
      )}

      {/* Recent Communications */}
      <div>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-lg font-semibold text-primary flex items-center gap-2">
            <MessageSquare className="w-5 h-5" />
            Comunicações recentes
          </h2>
          <button
            onClick={() => navigate('/communications')}
            className="flex items-center gap-1 text-sm text-accent hover:underline"
          >
            Ver todas
            <ExternalLink className="w-3.5 h-3.5" />
          </button>
        </div>

        {commLoading ? (
          <div className="flex justify-center py-8"><Spinner /></div>
        ) : communications.length === 0 ? (
          <Card>
            <div className="flex flex-col items-center justify-center py-10 text-primary-400 gap-2">
              <MessageSquare className="w-8 h-8 opacity-40" />
              <p className="text-sm">Nenhuma comunicação recente</p>
            </div>
          </Card>
        ) : (
          <div className="space-y-3">
            {communications.map((comm) => (
              <Card
                key={comm.id}
                className="cursor-pointer hover:shadow-md transition-shadow"
                onClick={() => navigate(`/communications/${comm.id}`)}
              >
                <div className="p-4">
                  <div className="flex items-center gap-2 mb-1">
                    <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium', TYPE_COLORS[comm.commType] || 'bg-gray-100')}>
                      {TYPE_LABELS[comm.commType] || comm.commType}
                    </span>
                    {comm.publishedAt && (
                      <span className="text-xs text-primary-400">{formatDate(comm.publishedAt)}</span>
                    )}
                  </div>
                  <h3 className="font-semibold text-primary">{comm.title}</h3>
                  {comm.summary && (
                    <p className="text-sm text-primary-500 mt-1 line-clamp-2">{comm.summary}</p>
                  )}
                </div>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
