import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Plus, Filter, Clock, CheckCircle, XCircle, AlertCircle,
  List, ChevronRight, Eye, Search, Users, LayoutTemplate,
} from 'lucide-react';
import { Button, Card, Spinner } from '@/components/ui';
import { useWorkflows, useWorkflow, useCreateWorkflow } from '@/hooks/useWorkflows';
import { useUsers } from '@/hooks/useUsers';
import { useApprovalGroups, useWorkflowTemplates, useSaveWorkflowTemplate } from '@/hooks/useApprovalConfig';
import type { Workflow, WorkflowStep, CreateWorkflowRequest, ApprovalMode, WorkflowTemplate } from '@/types/phase6';
import { formatDate } from '@/utils/format';
import { cn } from '@/utils/cn';

const WORKFLOW_TYPE_VALUES = [
  'contract_approval',
  'shareholder_change',
  'communication_approval',
  'document_verification',
  'vesting_approval',
];

const STATUS_COLORS: Record<string, string> = {
  pending: 'bg-amber-100 text-amber-700',
  in_progress: 'bg-blue-100 text-blue-700',
  approved: 'bg-green-100 text-green-700',
  rejected: 'bg-red-100 text-red-700',
  cancelled: 'bg-gray-100 text-gray-500',
};

const PRIORITY_COLORS: Record<string, string> = {
  low: 'bg-gray-100 text-gray-600',
  medium: 'bg-blue-100 text-blue-700',
  high: 'bg-amber-100 text-amber-700',
  urgent: 'bg-red-100 text-red-700',
};

const STATUS_ICONS: Record<string, React.ReactNode> = {
  pending: <Clock className="w-4 h-4 text-amber-500" />,
  in_progress: <AlertCircle className="w-4 h-4 text-blue-500" />,
  approved: <CheckCircle className="w-4 h-4 text-green-500" />,
  rejected: <XCircle className="w-4 h-4 text-red-500" />,
  cancelled: <XCircle className="w-4 h-4 text-gray-400" />,
};

const ROLES = [
  'Admin', 'Founder', 'BoardMember', 'Legal', 'Finance', 'HR', 'Employee', 'Investor',
];

// ─── Create Workflow Modal ────────────────────────────────────────────────────

// ─── Due Date Helpers ─────────────────────────────────────────────────────────

type StepDueDateMode = 'none' | 'calendar_days' | 'business_days';

function addCalendarDays(base: Date, days: number): Date {
  const d = new Date(base);
  d.setDate(d.getDate() + days);
  return d;
}

function addBusinessDays(base: Date, days: number): Date {
  const d = new Date(base);
  let added = 0;
  while (added < days) {
    d.setDate(d.getDate() + 1);
    const day = d.getDay();
    if (day !== 0 && day !== 6) added++;
  }
  return d;
}

function toDateInputValue(d: Date): string {
  return d.toISOString().split('T')[0];
}

function formatPreviewDate(iso: string): string {
  if (!iso) return '';
  const [year, month, day] = iso.split('-');
  return `${day}/${month}/${year}`;
}

/** Avança `base` de acordo com o modo e quantidade de dias de uma etapa */
function advanceByStep(base: Date, mode: StepDueDateMode, days: number): Date {
  if (mode === 'calendar_days' && days > 0) return addCalendarDays(base, days);
  if (mode === 'business_days' && days > 0) return addBusinessDays(base, days);
  return base;
}

/** Dado um índice de etapa, calcula quando ela deve terminar (base = hoje + soma das anteriores) */
function computeStepDueDate(
  steps: StepDraft[],
  targetIdx: number,
): string {
  let cursor = new Date();
  for (let i = 0; i <= targetIdx; i++) {
    cursor = advanceByStep(cursor, steps[i].dueDateMode, steps[i].dueDateDays);
  }
  const mode = steps[targetIdx].dueDateMode;
  return mode !== 'none' && steps[targetIdx].dueDateDays > 0 ? toDateInputValue(cursor) : '';
}

/** Prazo total = hoje + soma acumulada de todos os steps com prazo */
function computeTotalDueDate(steps: StepDraft[]): string {
  let cursor = new Date();
  let hasAny = false;
  for (const s of steps) {
    if (s.dueDateMode !== 'none' && s.dueDateDays > 0) {
      cursor = advanceByStep(cursor, s.dueDateMode, s.dueDateDays);
      hasAny = true;
    }
  }
  return hasAny ? toDateInputValue(cursor) : '';
}

// ─── Step Draft ───────────────────────────────────────────────────────────────

type AssigneeKind = 'role' | 'user' | 'group';

interface StepDraft {
  name: string;
  description: string;
  stepType: string;
  assigneeKind: AssigneeKind;
  assignedRole: string;
  assignedUserId: string;
  assignedGroupId: string;
  /** '' = usar a regra padrão do grupo */
  approvalMode: '' | ApprovalMode;
  dueDateMode: StepDueDateMode;
  dueDateDays: number;
}

const newStepDraft = (): StepDraft => ({
  name: '', description: '', stepType: 'approval',
  assigneeKind: 'role', assignedRole: 'Admin', assignedUserId: '', assignedGroupId: '', approvalMode: '',
  dueDateMode: 'none', dueDateDays: 3,
});

function stepFromTemplate(s: WorkflowTemplate['steps'][number]): StepDraft {
  const kind: AssigneeKind = s.assignedGroupId ? 'group' : s.assignedUserId ? 'user' : 'role';
  return {
    name: s.name,
    description: s.description ?? '',
    stepType: s.stepType,
    assigneeKind: kind,
    assignedRole: s.assignedRole ?? '',
    assignedUserId: s.assignedUserId ?? '',
    assignedGroupId: s.assignedGroupId ?? '',
    approvalMode: s.approvalMode ?? '',
    dueDateMode: s.dueMode,
    dueDateDays: s.dueDays > 0 ? s.dueDays : 3,
  };
}

function isStepValid(s: StepDraft): boolean {
  if (!s.name) return false;
  if (s.assigneeKind === 'group') return !!s.assignedGroupId;
  if (s.assigneeKind === 'user') return !!s.assignedUserId;
  return true;
}

interface CreateModalProps {
  initialTemplateId?: string;
  onClose: () => void;
}

function CreateWorkflowModal({ initialTemplateId, onClose }: CreateModalProps) {
  const { t } = useTranslation();
  const createWorkflow = useCreateWorkflow();
  const saveTemplate = useSaveWorkflowTemplate();
  const { data: groups = [] } = useApprovalGroups();
  const { data: templates = [] } = useWorkflowTemplates();
  const { data: usersData } = useUsers({ page: 1, pageSize: 200 });
  const users = usersData?.items ?? [];
  const activeGroups = groups.filter((g) => g.isActive);
  const [templateId, setTemplateId] = useState('');
  const [saveAsTemplate, setSaveAsTemplate] = useState(false);
  const [templateName, setTemplateName] = useState('');

  const STEP_TYPES = [
    { value: 'approval', label: t('approvals.stepTypeApproval') },
    { value: 'review', label: t('approvals.stepTypeReview') },
    { value: 'notification', label: t('approvals.stepTypeNotification') },
    { value: 'automated', label: t('approvals.stepTypeAutomated') },
  ];

  const STEP_DUE_MODE_LABELS: Record<StepDueDateMode, string> = {
    none: t('approvals.dueDateNone'),
    calendar_days: t('approvals.calendarDays'),
    business_days: t('approvals.businessDays'),
  };

  const WORKFLOW_TYPE_LABELS: Record<string, string> = {
    contract_approval: t('approvals.workflowTypes.contract_approval'),
    shareholder_change: t('approvals.workflowTypes.shareholder_change'),
    communication_approval: t('approvals.workflowTypes.communication_approval'),
    document_verification: t('approvals.workflowTypes.document_verification'),
    vesting_approval: t('approvals.workflowTypes.vesting_approval'),
  };
  const [form, setForm] = useState({
    title: '',
    description: '',
    workflowType: 'contract_approval',
    referenceType: 'manual',
    referenceId: crypto.randomUUID(),
    priority: 'medium',
  });
  const [steps, setSteps] = useState<StepDraft[]>([newStepDraft()]);

  function applyTemplate(id: string) {
    setTemplateId(id);
    const tpl = templates.find((x) => x.id === id);
    if (!tpl) return;
    setForm((f) => ({
      ...f,
      title: f.title || tpl.name,
      description: f.description || tpl.description || '',
      workflowType: tpl.workflowType,
      priority: tpl.priority,
    }));
    setSteps(tpl.steps.map(stepFromTemplate));
  }

  // Abre já com o modelo escolhido na tela de modelos (?template=id)
  useEffect(() => {
    if (initialTemplateId && templates.some((x) => x.id === initialTemplateId) && !templateId) {
      applyTemplate(initialTemplateId);
    }
  }, [initialTemplateId, templates]);

  function addStep() {
    setSteps((s) => [...s, newStepDraft()]);
  }

  function removeStep(idx: number) {
    setSteps((s) => s.filter((_, i) => i !== idx));
  }

  function updateStep(idx: number, field: keyof StepDraft, value: string) {
    setSteps((s) => s.map((step, i) => {
      if (i !== idx) return step;
      if (field === 'dueDateDays') return { ...step, dueDateDays: Number(value) };
      return { ...step, [field]: value };
    }));
  }

  const canSubmit = !!form.title && steps.every(isStepValid) && (!saveAsTemplate || !!templateName.trim());

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!canSubmit) return;
    const totalDueDate = computeTotalDueDate(steps);
    const payload: CreateWorkflowRequest = {
      title: form.title,
      description: form.description || undefined,
      workflowType: form.workflowType,
      referenceType: form.referenceType,
      referenceId: form.referenceId,
      priority: form.priority,
      dueDate: totalDueDate || undefined,
      steps: steps.map((s, idx) => ({
        name: s.name,
        description: s.description || undefined,
        stepType: s.stepType,
        assignedRole: s.assigneeKind === 'role' ? s.assignedRole || undefined : undefined,
        assignedUserId: s.assigneeKind === 'user' ? s.assignedUserId : undefined,
        assignedGroupId: s.assigneeKind === 'group' ? s.assignedGroupId : undefined,
        approvalMode: s.assigneeKind === 'group' && s.approvalMode ? s.approvalMode : undefined,
        dueDate: computeStepDueDate(steps, idx) || undefined,
      })),
    };
    if (saveAsTemplate) {
      saveTemplate.mutate({
        data: {
          name: templateName.trim(),
          description: form.description || undefined,
          workflowType: form.workflowType,
          priority: form.priority,
          isActive: true,
          steps: steps.map((s) => ({
            name: s.name,
            description: s.description || undefined,
            stepType: s.stepType,
            assignedRole: s.assigneeKind === 'role' ? s.assignedRole || undefined : undefined,
            assignedUserId: s.assigneeKind === 'user' ? s.assignedUserId : undefined,
            assignedGroupId: s.assigneeKind === 'group' ? s.assignedGroupId : undefined,
            approvalMode: s.assigneeKind === 'group' && s.approvalMode ? s.approvalMode : undefined,
            dueMode: s.dueDateMode,
            dueDays: s.dueDateMode === 'none' ? 0 : s.dueDateDays,
          })),
        },
      });
    }
    createWorkflow.mutate(payload, { onSuccess: onClose });
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 overflow-y-auto">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-5xl my-4">
        <div className="flex items-center justify-between px-6 py-4 border-b border-primary-100">
          <h3 className="font-semibold text-primary text-lg">{t('approvals.newApprovalFlow')}</h3>
          <button onClick={onClose} className="text-primary-400 hover:text-primary text-xl leading-none">&times;</button>
        </div>
        <form onSubmit={handleSubmit}>
          <div className="grid grid-cols-1 lg:grid-cols-[340px_1fr] max-h-[70vh] overflow-y-auto lg:overflow-hidden">
          <div className="px-6 py-4 space-y-4 lg:overflow-y-auto lg:max-h-[70vh] lg:border-r border-primary-100">
            {templates.length > 0 && (
              <div>
                <label className="input-label">Modelo de fluxo</label>
                <select className="input" value={templateId} onChange={(e) => applyTemplate(e.target.value)}>
                  <option value="">Fluxo em branco</option>
                  {templates.filter((x) => x.isActive).map((x) => (
                    <option key={x.id} value={x.id}>{x.name} ({x.steps.length} etapas)</option>
                  ))}
                </select>
              </div>
            )}
            {/* Título */}
            <div>
              <label className="input-label">{t('common.title')} <span className="text-red-500">*</span></label>
              <input
                className="input"
                value={form.title}
                onChange={(e) => setForm((f) => ({ ...f, title: e.target.value }))}
                placeholder={t('approvals.titlePlaceholder')}
                required
              />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="input-label">{t('approvals.flowType')} <span className="text-red-500">*</span></label>
                <select className="input" value={form.workflowType} onChange={(e) => setForm((f) => ({ ...f, workflowType: e.target.value }))}>
                  {WORKFLOW_TYPE_VALUES.map((v) => (
                    <option key={v} value={v}>{WORKFLOW_TYPE_LABELS[v]}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="input-label">{t('common.priority')}</label>
                <select className="input" value={form.priority} onChange={(e) => setForm((f) => ({ ...f, priority: e.target.value }))}>
                  <option value="low">{t('approvals.priorityLow')}</option>
                  <option value="medium">{t('approvals.priorityMedium')}</option>
                  <option value="high">{t('approvals.priorityHigh')}</option>
                  <option value="urgent">{t('approvals.priorityUrgent')}</option>
                </select>
              </div>
            </div>
            <div>
              <label className="input-label">{t('common.description')}</label>
              <textarea
                className="input resize-none"
                rows={2}
                value={form.description}
                onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                placeholder={t('approvals.descriptionPlaceholder')}
              />
            </div>
            <div className="pt-3 border-t border-primary-100 space-y-2">
              <label className="flex items-center gap-2 text-sm text-primary-600">
                <input type="checkbox" checked={saveAsTemplate} onChange={(e) => setSaveAsTemplate(e.target.checked)} />
                Salvar este fluxo como modelo reutilizável
              </label>
              {saveAsTemplate && (
                <input className="input text-sm" placeholder="Nome do modelo" value={templateName} onChange={(e) => setTemplateName(e.target.value)} />
              )}
            </div>
          </div>
          <div className="px-6 py-4 space-y-4 lg:overflow-y-auto lg:max-h-[70vh]">
            {/* Steps */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <label className="input-label mb-0">{t('approvals.steps')} <span className="text-red-500">*</span></label>
                <button type="button" onClick={addStep} className="text-sm text-accent hover:underline">{t('approvals.addStep')}</button>
              </div>
              <div className="space-y-3">
                {steps.map((step, idx) => (
                  <div key={idx} className="border border-primary-100 rounded-xl p-3 bg-primary-50 relative">
                    <div className="flex items-center gap-2 mb-2">
                      <span className="text-xs font-semibold text-primary-500">{t('approvals.stepLabel', { n: idx + 1 })}</span>
                      {steps.length > 1 && (
                        <button type="button" onClick={() => removeStep(idx)} className="ml-auto text-red-400 hover:text-red-600 text-xs">{t('approvals.removeStep')}</button>
                      )}
                    </div>
                    <div className="grid grid-cols-2 gap-2">
                      <div>
                        <label className="input-label text-xs">{t('common.name')} <span className="text-red-500">*</span></label>
                        <input className="input text-sm py-1.5" value={step.name} onChange={(e) => updateStep(idx, 'name', e.target.value)} placeholder={t('approvals.stepNamePlaceholder')} required />
                      </div>
                      <div>
                        <label className="input-label text-xs">{t('common.type')}</label>
                        <select className="input text-sm py-1.5" value={step.stepType} onChange={(e) => updateStep(idx, 'stepType', e.target.value)}>
                          {STEP_TYPES.map((t) => <option key={t.value} value={t.value}>{t.label}</option>)}
                        </select>
                      </div>
                      <div>
                        <label className="input-label text-xs">Aprovador</label>
                        <select className="input text-sm py-1.5" value={step.assigneeKind} onChange={(e) => updateStep(idx, 'assigneeKind', e.target.value)}>
                          <option value="role">Por perfil</option>
                          <option value="user">Usuário específico</option>
                          <option value="group">Grupo de aprovação</option>
                        </select>
                      </div>
                      <div>
                        {step.assigneeKind === 'role' && (
                          <>
                            <label className="input-label text-xs">{t('approvals.assignedRole')}</label>
                            <select className="input text-sm py-1.5" value={step.assignedRole} onChange={(e) => updateStep(idx, 'assignedRole', e.target.value)}>
                              <option value="">{t('approvals.anyRole')}</option>
                              {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
                            </select>
                          </>
                        )}
                        {step.assigneeKind === 'user' && (
                          <>
                            <label className="input-label text-xs">Usuário <span className="text-red-500">*</span></label>
                            <select className="input text-sm py-1.5" value={step.assignedUserId} onChange={(e) => updateStep(idx, 'assignedUserId', e.target.value)}>
                              <option value="">Selecione...</option>
                              {users.map((u) => <option key={u.id} value={u.id}>{u.name}</option>)}
                            </select>
                          </>
                        )}
                        {step.assigneeKind === 'group' && (
                          <>
                            <label className="input-label text-xs">Grupo <span className="text-red-500">*</span></label>
                            <select className="input text-sm py-1.5" value={step.assignedGroupId} onChange={(e) => updateStep(idx, 'assignedGroupId', e.target.value)}>
                              <option value="">Selecione...</option>
                              {activeGroups.map((g) => <option key={g.id} value={g.id}>{g.name} ({g.members.length})</option>)}
                            </select>
                          </>
                        )}
                      </div>
                      {step.assigneeKind === 'group' && (
                        <div className="col-span-2">
                          <label className="input-label text-xs">Regra de aprovação do grupo</label>
                          <select className="input text-sm py-1.5" value={step.approvalMode} onChange={(e) => updateStep(idx, 'approvalMode', e.target.value)}>
                            <option value="">Padrão do grupo{(() => {
                              const g = activeGroups.find((x) => x.id === step.assignedGroupId);
                              return g ? ` (${g.approvalMode === 'all' ? 'todos aprovam' : 'um aprova'})` : '';
                            })()}</option>
                            <option value="any">Basta um membro aprovar</option>
                            <option value="all">Todos os membros devem aprovar</option>
                          </select>
                        </div>
                      )}
                      <div>
                        <label className="input-label text-xs">{t('common.description')}</label>
                        <input className="input text-sm py-1.5" value={step.description} onChange={(e) => updateStep(idx, 'description', e.target.value)} placeholder={t('common.optional')} />
                      </div>
                    </div>

                    {/* Prazo da etapa */}
                    <div className="mt-2 pt-2 border-t border-primary-100">
                      <label className="input-label text-xs mb-1.5">{t('approvals.stepDueDate')}</label>
                      <div className="flex flex-wrap gap-1.5 mb-1.5">
                        {(Object.keys(STEP_DUE_MODE_LABELS) as StepDueDateMode[]).map((mode) => (
                          <button
                            key={mode}
                            type="button"
                            onClick={() => updateStep(idx, 'dueDateMode', mode)}
                            className={cn(
                              'px-2.5 py-0.5 rounded-full text-xs font-medium border transition-colors',
                              step.dueDateMode === mode
                                ? 'bg-accent text-white border-accent'
                                : 'bg-white text-primary-500 border-primary-200 hover:border-accent hover:text-accent',
                            )}
                          >
                            {STEP_DUE_MODE_LABELS[mode]}
                          </button>
                        ))}
                      </div>
                      {step.dueDateMode !== 'none' && (
                        <div className="flex items-center gap-2">
                          <input
                            type="number"
                            className="input text-sm py-1 w-20"
                            min={1}
                            max={365}
                            value={step.dueDateDays}
                            onChange={(e) => updateStep(idx, 'dueDateDays', String(Math.max(1, Number(e.target.value))))}
                          />
                          <span className="text-xs text-primary-500">
                            {step.dueDateMode === 'business_days' ? t('approvals.businessDaysUnit') : t('approvals.calendarDaysUnit')}
                          </span>
                          {(() => {
                            const preview = computeStepDueDate(steps, idx);
                            return preview ? (
                              <span className="text-xs text-primary-400 ml-1">
                                → <span className="font-semibold text-primary-600">{formatPreviewDate(preview)}</span>
                              </span>
                            ) : null;
                          })()}
                        </div>
                      )}
                    </div>
                  </div>
                ))}
              </div>

              {/* Prazo total do fluxo */}
              {(() => {
                const total = computeTotalDueDate(steps);
                return total ? (
                  <div className="mt-3 flex items-center gap-2 px-3 py-2 bg-accent/5 border border-accent/20 rounded-lg">
                    <Clock className="w-4 h-4 text-accent flex-shrink-0" />
                    <span className="text-sm text-primary-600">
                      {t('approvals.totalDueDate')}{' '}
                      <span className="font-semibold text-accent">{formatPreviewDate(total)}</span>
                      <span className="text-xs text-primary-400 ml-1">{t('approvals.cumulativeSteps')}</span>
                    </span>
                  </div>
                ) : null;
              })()}
            </div>
          </div>
          </div>
          <div className="flex justify-end gap-3 px-6 py-4 border-t border-primary-100">
            <Button variant="secondary" type="button" onClick={onClose} disabled={createWorkflow.isPending}>{t('common.cancel')}</Button>
            <Button type="submit" loading={createWorkflow.isPending} disabled={!canSubmit}>
              {t('approvals.createFlow')}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ─── Status Modal ─────────────────────────────────────────────────────────────

const STEP_STATUS_LABELS: Record<WorkflowStep['status'], string> = {
  pending: 'Pendente',
  in_progress: 'Em andamento',
  completed: 'Concluída',
  skipped: 'Ignorada',
};

const STEP_STATUS_COLORS: Record<WorkflowStep['status'], string> = {
  pending: 'bg-gray-100 text-gray-600',
  in_progress: 'bg-blue-100 text-blue-700',
  completed: 'bg-green-100 text-green-700',
  skipped: 'bg-gray-100 text-gray-500',
};

const DECISION_LABELS: Record<string, { label: string; color: string }> = {
  approved: { label: 'Aprovou', color: 'text-green-600' },
  rejected: { label: 'Rejeitou', color: 'text-red-600' },
  requested_changes: { label: 'Solicitou alterações', color: 'text-amber-600' },
};

function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('pt-BR');
}

function stepAssigneeLabel(step: WorkflowStep): string {
  if (step.assignedGroupName) return `Grupo: ${step.assignedGroupName}`;
  if (step.approvers?.length) return step.approvers.map((a) => a.userName).join(', ');
  if (step.assignedRole) return `Perfil: ${step.assignedRole}`;
  return 'Qualquer perfil';
}

function WorkflowStatusModal({ workflowId, onClose }: { workflowId: string; onClose: () => void }) {
  const { data: workflow, isLoading } = useWorkflow(workflowId);
  const steps = [...(workflow?.steps ?? [])].sort((a, b) => a.stepOrder - b.stepOrder);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 overflow-y-auto" onClick={onClose}>
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-3xl my-4" onClick={(e) => e.stopPropagation()}>
        <div className="flex items-center justify-between px-6 py-4 border-b border-primary-100">
          <h3 className="font-semibold text-primary text-lg">Status do fluxo</h3>
          <button onClick={onClose} className="text-primary-400 hover:text-primary text-xl leading-none">&times;</button>
        </div>
        <div className="px-6 py-4 max-h-[75vh] overflow-y-auto space-y-4">
          {isLoading || !workflow ? (
            <div className="flex justify-center py-12"><Spinner /></div>
          ) : (
            <>
              <div>
                <div className="flex items-center gap-2 mb-1">
                  {STATUS_ICONS[workflow.status]}
                  <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium', STATUS_COLORS[workflow.status])}>
                    {workflow.status}
                  </span>
                  <span className="text-xs text-primary-500">{workflow.workflowTypeLabel}</span>
                </div>
                <h4 className="font-semibold text-primary">{workflow.title}</h4>
                {workflow.description && <p className="text-sm text-primary-500 mt-1">{workflow.description}</p>}
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 mt-3 text-xs text-primary-500">
                  <div><span className="block text-primary-400">Solicitante</span>{workflow.requestedByName}</div>
                  <div><span className="block text-primary-400">Criado em</span>{formatDateTime(workflow.requestedAt)}</div>
                  <div><span className="block text-primary-400">Prazo</span>{workflow.dueDate ? formatDate(workflow.dueDate) : '—'}</div>
                  <div><span className="block text-primary-400">Progresso</span>{workflow.currentStep}/{workflow.totalSteps} etapas</div>
                </div>
                {workflow.cancellationReason && (
                  <p className="text-xs text-red-500 mt-2">Motivo do cancelamento: {workflow.cancellationReason}</p>
                )}
              </div>

              <ol className="space-y-3">
                {steps.map((step) => (
                  <li key={step.id} className={cn('border rounded-xl p-3', step.isCurrent ? 'border-accent bg-accent/5' : 'border-primary-100 bg-primary-50')}>
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-xs font-semibold text-primary-500">Etapa {step.stepOrder}</span>
                      <span className="text-sm font-medium text-primary">{step.name}</span>
                      <span className={cn('ml-auto px-2 py-0.5 rounded-full text-xs font-medium', STEP_STATUS_COLORS[step.status])}>
                        {STEP_STATUS_LABELS[step.status]}
                      </span>
                    </div>
                    <div className="mt-1 text-xs text-primary-500 flex flex-wrap gap-x-4 gap-y-1">
                      <span>{stepAssigneeLabel(step)}</span>
                      {step.approvalMode && <span>{step.approvalMode === 'all' ? 'Todos aprovam' : 'Um aprova'}</span>}
                      {step.dueDate && <span>Prazo: {formatDate(step.dueDate)}</span>}
                      {step.completedAt && <span>Concluída em {formatDateTime(step.completedAt)}</span>}
                    </div>
                    {step.description && <p className="text-xs text-primary-400 mt-1">{step.description}</p>}
                    {step.approvals && step.approvals.length > 0 ? (
                      <ul className="mt-2 space-y-1.5 border-t border-primary-100 pt-2">
                        {step.approvals.map((a) => {
                          const d = DECISION_LABELS[a.decision] ?? { label: a.decision, color: 'text-primary-600' };
                          return (
                            <li key={a.id} className="text-xs">
                              <span className="font-medium text-primary">{a.userName ?? a.userId}</span>{' '}
                              <span className={d.color}>{d.label}</span>{' '}
                              <span className="text-primary-400">em {formatDateTime(a.decidedAt)}</span>
                              {a.comments && <p className="text-primary-500 mt-0.5 italic">“{a.comments}”</p>}
                            </li>
                          );
                        })}
                      </ul>
                    ) : (
                      step.status !== 'skipped' && <p className="mt-2 text-xs text-primary-400">Nenhuma decisão registrada.</p>
                    )}
                  </li>
                ))}
              </ol>
            </>
          )}
        </div>
        <div className="flex justify-end px-6 py-4 border-t border-primary-100">
          <Button variant="secondary" onClick={onClose}>{'Fechar'}</Button>
        </div>
      </div>
    </div>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function FlowsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const WORKFLOW_TYPES = [
    { value: '', label: t('approvals.allTypes') },
    { value: 'contract_approval', label: t('approvals.workflowTypes.contract_approval') },
    { value: 'shareholder_change', label: t('approvals.workflowTypes.shareholder_change') },
    { value: 'communication_approval', label: t('approvals.workflowTypes.communication_approval') },
    { value: 'document_verification', label: t('approvals.workflowTypes.document_verification') },
    { value: 'vesting_approval', label: t('approvals.workflowTypes.vesting_approval') },
  ];

  const WORKFLOW_TYPE_LABELS: Record<string, string> = {
    contract_approval: t('approvals.workflowTypes.contract_approval'),
    shareholder_change: t('approvals.workflowTypes.shareholder_change'),
    communication_approval: t('approvals.workflowTypes.communication_approval'),
    document_verification: t('approvals.workflowTypes.document_verification'),
    vesting_approval: t('approvals.workflowTypes.vesting_approval'),
  };

  const STATUS_LABELS: Record<string, string> = {
    pending: t('approvals.pending'),
    in_progress: t('approvals.inProgress'),
    approved: t('approvals.approved'),
    rejected: t('approvals.rejected'),
    cancelled: t('approvals.cancelled'),
  };

  const PRIORITY_LABELS: Record<string, string> = {
    low: t('approvals.priorityLow'),
    medium: t('approvals.priorityMedium'),
    high: t('approvals.priorityHigh'),
    urgent: t('approvals.priorityUrgent'),
  };
  const [typeFilter, setTypeFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [search, setSearch] = useState('');
  const [searchParams, setSearchParams] = useSearchParams();
  const templateParam = searchParams.get('template') ?? undefined;
  const [showCreate, setShowCreate] = useState(!!templateParam);
  const [page, setPage] = useState(1);
  const [statusWorkflowId, setStatusWorkflowId] = useState<string | null>(null);

  const { data, isLoading } = useWorkflows({
    workflowType: typeFilter || undefined,
    status: statusFilter || undefined,
    page,
    pageSize: 12,
  });

  const items: Workflow[] = data?.items ?? [];
  const filtered = search
    ? items.filter(
        (w) =>
          w.title.toLowerCase().includes(search.toLowerCase()) ||
          (w.description ?? '').toLowerCase().includes(search.toLowerCase())
      )
    : items;

  return (
    <div className="space-y-6 animate-fade-in">
      {/* Header */}
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="page-title">{t('approvals.flowsTitle')}</h1>
          <p className="page-subtitle">{t('approvals.flowsSubtitle')}</p>
        </div>
        <div className="flex items-center gap-2 flex-wrap">
          <Button variant="secondary" icon={<LayoutTemplate className="w-4 h-4" />} onClick={() => navigate('/approvals/templates')}>
            Modelos
          </Button>
          <Button variant="secondary" icon={<Users className="w-4 h-4" />} onClick={() => navigate('/approvals/groups')}>
            Grupos
          </Button>
          <Button icon={<Plus className="w-4 h-4" />} onClick={() => setShowCreate(true)}>
            {t('approvals.newWorkflow')}
          </Button>
        </div>
      </div>

      {/* Filters */}
      <Card>
        <div className="p-4 flex flex-wrap gap-3 items-center">
          <div className="relative flex-1 min-w-[200px]">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-primary-400" />
            <input
              className="input pl-9 text-sm"
              placeholder={t('approvals.searchByTitle')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <div className="flex items-center gap-2">
            <Filter className="w-4 h-4 text-primary-400 shrink-0" />
            <select
              className="input text-sm py-2"
              value={typeFilter}
              onChange={(e) => { setTypeFilter(e.target.value); setPage(1); }}
            >
              {WORKFLOW_TYPES.map((t) => (
                <option key={t.value} value={t.value}>{t.label}</option>
              ))}
            </select>
          </div>
          <select
            className="input text-sm py-2 min-w-[150px]"
            value={statusFilter}
            onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
          >
            <option value="">{t('approvals.allStatuses')}</option>
            {Object.entries(STATUS_LABELS).map(([v, l]) => (
              <option key={v} value={v}>{l}</option>
            ))}
          </select>
        </div>
      </Card>

      {/* Content */}
      {isLoading ? (
        <div className="flex justify-center py-12"><Spinner /></div>
      ) : filtered.length === 0 ? (
        <Card>
          <div className="flex flex-col items-center justify-center py-16 text-primary-400 gap-3">
            <List className="w-10 h-10 opacity-40" />
            <p className="text-sm font-medium">{t('approvals.noFlowsFound')}</p>
            <p className="text-xs text-primary-400">{t('approvals.createFlowHint')}</p>
          </div>
        </Card>
      ) : (
        <>
          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
            {filtered.map((workflow) => (
              <Card
                key={workflow.id}
                className="cursor-pointer hover:shadow-md transition-shadow"
                onClick={() => navigate(`/approvals/${workflow.id}`)}
              >
                <div className="p-4">
                  {/* Header row */}
                  <div className="flex items-start justify-between gap-2 mb-3">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-1.5 mb-1">
                        {STATUS_ICONS[workflow.status]}
                        <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium', STATUS_COLORS[workflow.status])}>
                          {STATUS_LABELS[workflow.status] ?? workflow.status}
                        </span>
                      </div>
                      <h3 className="font-semibold text-primary text-sm truncate">{workflow.title}</h3>
                    </div>
                    <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium shrink-0', PRIORITY_COLORS[workflow.priority])}>
                      {PRIORITY_LABELS[workflow.priority] ?? workflow.priority}
                    </span>
                  </div>

                  {/* Type */}
                  <p className="text-xs text-primary-500 mb-3">
                    {WORKFLOW_TYPE_LABELS[workflow.workflowType] ?? workflow.workflowType}
                  </p>

                  {/* Progress + Due date */}
                  <div className="flex items-center gap-3 text-xs text-primary-500">
                    <div className="flex items-center gap-1.5 flex-1">
                      <div className="flex-1 bg-primary-100 rounded-full h-1.5">
                        <div
                          className="bg-accent rounded-full h-1.5 transition-all"
                          style={{ width: `${Math.round((workflow.currentStep / workflow.totalSteps) * 100)}%` }}
                        />
                      </div>
                      <span className="shrink-0">{workflow.currentStep}/{workflow.totalSteps}</span>
                    </div>
                    {workflow.dueDate && (
                      <span className={cn('flex items-center gap-1 shrink-0', new Date(workflow.dueDate) < new Date() ? 'text-red-500' : '')}>
                        <Clock className="w-3 h-3" />
                        {formatDate(workflow.dueDate)}
                      </span>
                    )}
                    <button
                      type="button"
                      title="Consultar status"
                      className="p-1 rounded hover:bg-primary-100 text-primary-400 hover:text-accent shrink-0"
                      onClick={(e) => { e.stopPropagation(); setStatusWorkflowId(workflow.id); }}
                    >
                      <Eye className="w-3.5 h-3.5" />
                    </button>
                    <ChevronRight className="w-3.5 h-3.5 shrink-0 text-primary-300" />
                  </div>
                </div>
              </Card>
            ))}
          </div>

          {/* Pagination */}
          {data && data.totalPages > 1 && (
            <div className="flex items-center justify-between">
              <p className="text-sm text-primary-500">
                {t('approvals.flowsFound', { count: data.total })}
              </p>
              <div className="flex gap-2">
                <Button variant="secondary" size="sm" disabled={page === 1} onClick={() => setPage((p) => p - 1)}>{t('common.previous')}</Button>
                <Button variant="secondary" size="sm" disabled={page === data.totalPages} onClick={() => setPage((p) => p + 1)}>{t('common.next')}</Button>
              </div>
            </div>
          )}
        </>
      )}

      {statusWorkflowId && (
        <WorkflowStatusModal workflowId={statusWorkflowId} onClose={() => setStatusWorkflowId(null)} />
      )}

      {showCreate && (
        <CreateWorkflowModal
          initialTemplateId={templateParam}
          onClose={() => {
            setShowCreate(false);
            if (templateParam) setSearchParams({}, { replace: true });
          }}
        />
      )}
    </div>
  );
}
