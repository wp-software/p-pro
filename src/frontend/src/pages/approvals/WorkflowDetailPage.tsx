import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  CheckCircle,
  XCircle,
  Clock,
  AlertCircle,
  User,
} from 'lucide-react';
import { Button, Card, Spinner } from '@/components/ui';
import { useWorkflow, useApproveStep, useRejectStep, useCancelWorkflow } from '@/hooks/useWorkflows';
import { useAuthStore } from '@/stores/authStore';
import type { WorkflowStep } from '@/types/phase6';
import { formatDate } from '@/utils/format';
import { cn } from '@/utils/cn';

const PRIORITY_COLORS: Record<string, string> = {
  low: 'bg-gray-100 text-gray-600',
  medium: 'bg-blue-100 text-blue-700',
  high: 'bg-amber-100 text-amber-700',
  urgent: 'bg-red-100 text-red-700',
};

const PRIORITY_LABELS: Record<string, string> = {
  low: 'Baixa', medium: 'Média', high: 'Alta', urgent: 'Urgente',
};

const STATUS_LABELS: Record<string, string> = {
  pending: 'Pendente', in_progress: 'Em andamento', approved: 'Aprovado',
  rejected: 'Rejeitado', cancelled: 'Cancelado',
};

const STATUS_COLORS: Record<string, string> = {
  pending: 'bg-amber-100 text-amber-700',
  in_progress: 'bg-blue-100 text-blue-700',
  approved: 'bg-green-100 text-green-700',
  rejected: 'bg-red-100 text-red-700',
  cancelled: 'bg-gray-100 text-gray-600',
};

function StepIcon({ status }: { status: WorkflowStep['status'] }) {
  if (status === 'completed') return <CheckCircle className="w-5 h-5 text-green-500" />;
  if (status === 'in_progress') return <AlertCircle className="w-5 h-5 text-blue-500" />;
  if (status === 'skipped') return <XCircle className="w-5 h-5 text-gray-400" />;
  return <Clock className="w-5 h-5 text-primary-300" />;
}

interface ApprovalModalProps {
  type: 'approve' | 'reject';
  onConfirm: (comments: string) => void;
  onClose: () => void;
  isLoading: boolean;
}

function ApprovalModal({ type, onConfirm, onClose, isLoading }: ApprovalModalProps) {
  const [comments, setComments] = useState('');

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-md">
        <div className="px-6 py-4 border-b border-primary-100">
          <h3 className="font-semibold text-primary">
            {type === 'approve' ? 'Aprovar etapa' : 'Rejeitar etapa'}
          </h3>
        </div>
        <div className="px-6 py-4 space-y-3">
          <div>
            <label className="input-label">
              Comentários {type === 'reject' && <span className="text-red-500">*</span>}
            </label>
            <textarea
              className="input min-h-[100px] resize-y"
              placeholder={type === 'approve' ? 'Comentários opcionais...' : 'Motivo da rejeição (obrigatório)'}
              value={comments}
              onChange={(e) => setComments(e.target.value)}
            />
          </div>
        </div>
        <div className="flex justify-end gap-3 px-6 py-4 border-t border-primary-100">
          <Button variant="secondary" onClick={onClose} disabled={isLoading}>Cancelar</Button>
          <Button
            variant={type === 'approve' ? 'success' : 'danger'}
            onClick={() => onConfirm(comments)}
            loading={isLoading}
            disabled={type === 'reject' && !comments.trim()}
          >
            {type === 'approve' ? 'Confirmar aprovação' : 'Confirmar rejeição'}
          </Button>
        </div>
      </div>
    </div>
  );
}

export default function WorkflowDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { user } = useAuthStore();
  const { data: workflow, isLoading } = useWorkflow(id!);
  const approveStep = useApproveStep();
  const rejectStep = useRejectStep();
  const cancelWorkflow = useCancelWorkflow();

  const [approvalModal, setApprovalModal] = useState<{
    type: 'approve' | 'reject';
    stepId: string;
  } | null>(null);

  if (isLoading) {
    return <div className="flex justify-center py-20"><Spinner /></div>;
  }

  if (!workflow) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-primary-400 gap-3">
        <p className="text-sm">Workflow não encontrado</p>
        <button onClick={() => navigate('/approvals')} className="text-accent text-sm hover:underline">
          Voltar para aprovações
        </button>
      </div>
    );
  }

  const currentStep = workflow.steps?.find((s) => s.isCurrent);
  const isGroupStep = !!currentStep && (!!currentStep.assignedGroupId || (currentStep.approvers?.length ?? 0) > 0);
  const alreadyDecided = !!currentStep?.approvals?.some((a) => a.userId === user?.id);
  const canAct =
    currentStep &&
    !alreadyDecided &&
    (isGroupStep
      ? currentStep.approvers?.some((a) => a.userId === user?.id)
      : currentStep.assignedUserId === user?.id ||
        (currentStep.assignedRole && user?.roles?.includes(currentStep.assignedRole)));

  function handleConfirmAction(comments: string) {
    if (!approvalModal) return;
    const { type, stepId } = approvalModal;
    if (type === 'approve') {
      approveStep.mutate({ workflowId: workflow!.id, stepId, comments }, {
        onSuccess: () => setApprovalModal(null),
      });
    } else {
      rejectStep.mutate({ workflowId: workflow!.id, stepId, comments }, {
        onSuccess: () => setApprovalModal(null),
      });
    }
  }

  return (
    <div className="space-y-6 animate-fade-in max-w-3xl">
      <button
        onClick={() => navigate('/approvals')}
        className="flex items-center gap-2 text-sm text-primary-500 hover:text-primary transition-colors"
      >
        <ArrowLeft className="w-4 h-4" />
        Voltar para aprovações
      </button>

      {/* Header */}
      <div className="flex items-start justify-between gap-4">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-1">
            <span className={cn('px-2.5 py-1 rounded-full text-xs font-medium', STATUS_COLORS[workflow.status] || 'bg-gray-100 text-gray-600')}>
              {STATUS_LABELS[workflow.status] || workflow.status}
            </span>
            <span className={cn('px-2.5 py-1 rounded-full text-xs font-medium', PRIORITY_COLORS[workflow.priority] || 'bg-gray-100')}>
              {PRIORITY_LABELS[workflow.priority] || workflow.priority}
            </span>
          </div>
          <h1 className="page-title">{workflow.title}</h1>
          {workflow.description && <p className="text-primary-500 text-sm mt-1">{workflow.description}</p>}
        </div>
        <div className="text-right text-sm text-primary-500 shrink-0">
          <p>Tipo: {workflow.workflowType}</p>
          {workflow.dueDate && <p>Prazo: {formatDate(workflow.dueDate)}</p>}
        </div>
      </div>

      {/* Progress */}
      <Card>
        <div className="p-4">
          <div className="flex items-center justify-between mb-2 text-sm">
            <span className="font-medium text-primary">Progresso</span>
            <span className="text-primary-500">{workflow.currentStep}/{workflow.totalSteps} etapas</span>
          </div>
          <div className="w-full bg-primary-100 rounded-full h-2">
            <div
              className="bg-accent rounded-full h-2 transition-all"
              style={{ width: `${(workflow.currentStep / workflow.totalSteps) * 100}%` }}
            />
          </div>
        </div>
      </Card>

      {/* Steps timeline */}
      {workflow.steps && workflow.steps.length > 0 && (
        <Card>
          <div className="p-4">
            <h2 className="font-semibold text-primary mb-4">Etapas do workflow</h2>
            <div className="space-y-0">
              {workflow.steps.map((step, idx) => (
                <div key={step.id} className="flex gap-4">
                  <div className="flex flex-col items-center">
                    <div className={cn(
                      'w-9 h-9 rounded-full flex items-center justify-center border-2 shrink-0',
                      step.status === 'completed' ? 'border-green-400 bg-green-50' :
                      step.status === 'in_progress' ? 'border-blue-400 bg-blue-50' :
                      step.isCurrent ? 'border-accent bg-accent/10' :
                      'border-primary-200 bg-primary-50'
                    )}>
                      <StepIcon status={step.status} />
                    </div>
                    {idx < workflow.steps!.length - 1 && (
                      <div className={cn('w-0.5 h-8 mt-1', step.status === 'completed' ? 'bg-green-200' : 'bg-primary-100')} />
                    )}
                  </div>
                  <div className="pb-6 flex-1 min-w-0">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <p className={cn('font-medium text-sm', step.isCurrent ? 'text-primary' : 'text-primary-600')}>
                          {step.name}
                        </p>
                        {step.assignedRole && (
                          <p className="text-xs text-primary-400 flex items-center gap-1 mt-0.5">
                            <User className="w-3 h-3" />
                            {step.assignedRole}
                          </p>
                        )}
                        {(step.assignedGroupId || step.assignedGroupName || (step.approvers?.length ?? 0) > 0) && (
                          <div className="mt-0.5">
                            <p className="text-xs text-primary-400 flex items-center gap-1">
                              <User className="w-3 h-3" />
                              {step.assignedGroupName ?? 'Grupo'} ·{' '}
                              {step.approvalMode === 'all'
                                ? `todos devem aprovar (${step.approvals?.filter((a) => a.decision === 'approved').length ?? 0}/${step.approvers?.length ?? 0})`
                                : 'basta um aprovar'}
                            </p>
                            {step.approvers && step.approvers.length > 0 && (
                              <p className="text-xs text-primary-400 mt-0.5">
                                {step.approvers.map((a) => {
                                  const d = step.approvals?.find((x) => x.userId === a.userId)?.decision;
                                  return `${a.userName}${d === 'approved' ? ' ✓' : d === 'rejected' ? ' ✗' : ''}`;
                                }).join(', ')}
                              </p>
                            )}
                          </div>
                        )}
                        {step.notes && <p className="text-xs text-primary-500 mt-1 italic">{step.notes}</p>}
                      </div>
                      {step.completedAt && (
                        <span className="text-xs text-primary-400 shrink-0">{formatDate(step.completedAt)}</span>
                      )}
                    </div>

                    {/* Approvals history */}
                    {step.approvals && step.approvals.length > 0 && (
                      <div className="mt-2 space-y-1">
                        {step.approvals.map((approval) => (
                          <div key={approval.id} className="flex items-start gap-2 text-xs text-primary-500">
                            {approval.decision === 'approved' && <CheckCircle className="w-3.5 h-3.5 text-green-500 mt-0.5 shrink-0" />}
                            {approval.decision === 'rejected' && <XCircle className="w-3.5 h-3.5 text-red-500 mt-0.5 shrink-0" />}
                            <span>
                              {approval.userName ? <strong>{approval.userName}: </strong> : null}
                              {approval.comments || (approval.decision === 'approved' ? 'Aprovado' : 'Rejeitado')}
                            </span>
                          </div>
                        ))}
                      </div>
                    )}

                    {/* Action buttons for current step */}
                    {step.isCurrent && canAct && workflow.status !== 'approved' && workflow.status !== 'rejected' && (
                      <div className="flex gap-2 mt-3">
                        <Button
                          variant="success"
                          size="sm"
                          icon={<CheckCircle className="w-4 h-4" />}
                          onClick={() => setApprovalModal({ type: 'approve', stepId: step.id })}
                        >
                          Aprovar
                        </Button>
                        <Button
                          variant="danger"
                          size="sm"
                          icon={<XCircle className="w-4 h-4" />}
                          onClick={() => setApprovalModal({ type: 'reject', stepId: step.id })}
                        >
                          Rejeitar
                        </Button>
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </Card>
      )}

      {/* Cancel button */}
      {(workflow.status === 'pending' || workflow.status === 'in_progress') && (
        <div className="flex justify-end">
          <Button
            variant="danger"
            size="sm"
            onClick={() => {
              const reason = window.prompt('Motivo do cancelamento:');
              if (reason) {
                cancelWorkflow.mutate({ id: workflow.id, reason }, {
                  onSuccess: () => navigate('/approvals'),
                });
              }
            }}
            loading={cancelWorkflow.isPending}
          >
            Cancelar workflow
          </Button>
        </div>
      )}

      {approvalModal && (
        <ApprovalModal
          type={approvalModal.type}
          onConfirm={handleConfirmAction}
          onClose={() => setApprovalModal(null)}
          isLoading={approveStep.isPending || rejectStep.isPending}
        />
      )}
    </div>
  );
}
