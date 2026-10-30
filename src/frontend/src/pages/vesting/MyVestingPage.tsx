import { useNavigate } from 'react-router-dom';
import { TrendingUp, Clock, CheckCircle, Target } from 'lucide-react';
import { Card, Spinner } from '@/components/ui';
import { useClientStore } from '@/stores/clientStore';
import { VestingProgressBar } from '@/components/vesting';
import { useMyVestingGrants, useShareholderLink } from '@/hooks';
import { ShareholderNotLinkedNotice } from '@/components/ui/ShareholderNotLinkedNotice';
import {
  VestingGrantDetailStatus,
  vestingGrantStatusLabels,
} from '@/types';

const statusColors: Record<VestingGrantDetailStatus, string> = {
  [VestingGrantDetailStatus.Pending]: 'bg-amber-100 text-amber-700',
  [VestingGrantDetailStatus.Approved]: 'bg-blue-100 text-blue-700',
  [VestingGrantDetailStatus.Active]: 'bg-green-100 text-green-700',
  [VestingGrantDetailStatus.Exercised]: 'bg-indigo-100 text-indigo-700',
  [VestingGrantDetailStatus.Expired]: 'bg-red-100 text-red-700',
  [VestingGrantDetailStatus.Cancelled]: 'bg-gray-100 text-gray-500',
};

export default function MyVestingPage() {
  const navigate = useNavigate();
  const { selectedCompanyId } = useClientStore();

  const link = useShareholderLink();
  const { data, isLoading, isError } = useMyVestingGrants(selectedCompanyId ?? undefined);

  const grants = data ?? [];

  const totalVested = grants.reduce((acc, g) => acc + g.vestedShares, 0);
  const totalShares = grants.reduce((acc, g) => acc + g.totalShares, 0);
  const totalAvailable = grants.reduce((acc, g) => acc + g.availableToExercise, 0);
  const totalExercised = grants.reduce((acc, g) => acc + g.exercisedShares, 0);

  if (link.isLoading || isLoading) {
    return (
      <div className="flex justify-center py-20">
        <Spinner className="w-8 h-8 text-indigo-600" />
      </div>
    );
  }

  if (!link.isLinked) {
    return <ShareholderNotLinkedNotice />;
  }

  if (isError) {
    return (
      <div className="flex items-center justify-center py-20">
        <Card className="p-8 text-center">
          <p className="text-gray-500">Erro ao carregar seus grants de vesting.</p>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6 animate-fade-in max-w-4xl mx-auto">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Meu Vesting</h1>
        <p className="text-gray-500 mt-1">Acompanhe o progresso das suas ações</p>
      </div>

      {/* Summary */}
      {grants.length > 0 && (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <Card className="p-4 text-center">
            <p className="text-xs text-gray-400 mb-1">Total de ações</p>
            <p className="text-2xl font-bold text-gray-900">{totalShares.toLocaleString('pt-BR')}</p>
          </Card>
          <Card className="p-4 text-center">
            <div className="flex items-center justify-center gap-1 mb-1">
              <TrendingUp size={13} className="text-indigo-400" />
              <p className="text-xs text-gray-400">Vestidas</p>
            </div>
            <p className="text-2xl font-bold text-indigo-600">{totalVested.toLocaleString('pt-BR')}</p>
          </Card>
          <Card className="p-4 text-center">
            <div className="flex items-center justify-center gap-1 mb-1">
              <CheckCircle size={13} className="text-green-400" />
              <p className="text-xs text-gray-400">Disponíveis</p>
            </div>
            <p className="text-2xl font-bold text-green-600">{totalAvailable.toLocaleString('pt-BR')}</p>
          </Card>
          <Card className="p-4 text-center">
            <div className="flex items-center justify-center gap-1 mb-1">
              <Clock size={13} className="text-gray-400" />
              <p className="text-xs text-gray-400">Exercidas</p>
            </div>
            <p className="text-2xl font-bold text-gray-600">{totalExercised.toLocaleString('pt-BR')}</p>
          </Card>
        </div>
      )}

      {/* Grants list */}
      {grants.length === 0 ? (
        <Card className="p-12 text-center">
          <Target className="w-12 h-12 text-gray-300 mx-auto mb-3" />
          <h3 className="font-semibold text-gray-700 mb-1">Nenhum grant encontrado</h3>
          <p className="text-gray-400 text-sm">
            Você ainda não possui grants de vesting associados à sua conta. Se acredita que deveria,
            peça a um administrador para vincular seu usuário ao seu cadastro de sócio.
          </p>
        </Card>
      ) : (
        <div className="space-y-4">
          {grants.map((grant) => (
            <Card
              key={grant.id}
              className="p-5 hover:shadow-md transition-shadow cursor-pointer"
              onClick={() => navigate(`/vesting/grant/${grant.id}`)}
            >
              <div className="flex items-start justify-between gap-4 mb-4">
                <div>
                  <h3 className="font-semibold text-gray-900">{grant.vestingPlanName}</h3>
                  <p className="text-sm text-gray-500 mt-0.5">
                    {grant.totalShares.toLocaleString('pt-BR')} ações ·{' '}
                    {grant.equityPercentage.toFixed(4)}% equity
                  </p>
                </div>
                <span
                  className={`text-xs font-medium px-2.5 py-1 rounded-full flex-shrink-0 ${statusColors[grant.status]}`}
                >
                  {vestingGrantStatusLabels[grant.status]}
                </span>
              </div>

              <VestingProgressBar
                vestedPercentage={grant.vestedPercentage}
                exercisedShares={grant.exercisedShares}
                vestedShares={grant.vestedShares}
                totalShares={grant.totalShares}
                size="md"
              />

              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mt-4 text-sm">
                <div>
                  <span className="text-xs text-gray-400 block">Início</span>
                  <span className="text-gray-600">
                    {new Date(grant.vestingStartDate).toLocaleDateString('pt-BR', {
                      month: 'short',
                      year: 'numeric',
                    })}
                  </span>
                </div>
                {grant.cliffDate && (
                  <div>
                    <span className="text-xs text-gray-400 block">Cliff</span>
                    <span className="text-amber-600">
                      {new Date(grant.cliffDate).toLocaleDateString('pt-BR', {
                        month: 'short',
                        year: 'numeric',
                      })}
                    </span>
                  </div>
                )}
                <div>
                  <span className="text-xs text-gray-400 block">Fim</span>
                  <span className="text-gray-600">
                    {new Date(grant.vestingEndDate).toLocaleDateString('pt-BR', {
                      month: 'short',
                      year: 'numeric',
                    })}
                  </span>
                </div>
                {grant.availableToExercise > 0 && (
                  <div>
                    <span className="text-xs text-gray-400 block">Disponíveis</span>
                    <span className="font-medium text-green-600">
                      {grant.availableToExercise.toLocaleString('pt-BR')} ações
                    </span>
                  </div>
                )}
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
