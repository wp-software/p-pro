import { useNavigate } from 'react-router-dom';
import { LayoutTemplate, Play, Trash2, Users, User, Shield } from 'lucide-react';
import { Button, Card, Spinner } from '@/components/ui';
import { useWorkflowTemplates, useDeleteWorkflowTemplate } from '@/hooks/useApprovalConfig';
import type { WorkflowTemplate, WorkflowTemplateStep } from '@/types/phase6';

function assigneeLabel(s: WorkflowTemplateStep): { icon: React.ReactNode; text: string } {
  if (s.assignedGroupId || s.assignedGroupName) {
    const rule = s.approvalMode === 'all' ? 'todos' : s.approvalMode === 'any' ? 'um aprova' : 'regra do grupo';
    return { icon: <Users className="w-3 h-3" />, text: `${s.assignedGroupName ?? 'Grupo removido'} · ${rule}` };
  }
  if (s.assignedUserId) return { icon: <User className="w-3 h-3" />, text: 'Usuário específico' };
  return { icon: <Shield className="w-3 h-3" />, text: s.assignedRole || 'Qualquer perfil' };
}

export default function TemplatesPage() {
  const navigate = useNavigate();
  const { data: templates = [], isLoading } = useWorkflowTemplates();
  const deleteTemplate = useDeleteWorkflowTemplate();

  function handleDelete(t: WorkflowTemplate) {
    if (window.confirm(`Remover o modelo "${t.name}"? Fluxos já criados não são afetados.`)) {
      deleteTemplate.mutate(t.id);
    }
  }

  return (
    <div className="space-y-6 animate-fade-in">
      <div>
        <h1 className="page-title">Modelos de fluxo</h1>
        <p className="page-subtitle">
          Modelos são criados ao salvar um fluxo como modelo em “Novo fluxo” e reutilizados com um clique.
        </p>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-12"><Spinner /></div>
      ) : templates.length === 0 ? (
        <Card>
          <div className="flex flex-col items-center py-16 text-primary-400 gap-3">
            <LayoutTemplate className="w-10 h-10 opacity-40" />
            <p className="text-sm font-medium">Nenhum modelo salvo</p>
            <Button variant="secondary" onClick={() => navigate('/approvals/flows')}>Criar fluxo e salvar como modelo</Button>
          </div>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {templates.map((tpl) => (
            <Card key={tpl.id}>
              <div className="p-4 space-y-3">
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <h3 className="font-semibold text-primary text-sm truncate">{tpl.name}</h3>
                    {tpl.description && <p className="text-xs text-primary-500 mt-0.5">{tpl.description}</p>}
                  </div>
                  <button onClick={() => handleDelete(tpl)} className="p-1.5 text-primary-400 hover:text-red-500 shrink-0" aria-label="Remover">
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
                <ol className="space-y-1.5">
                  {tpl.steps.map((s, i) => {
                    const a = assigneeLabel(s);
                    return (
                      <li key={i} className="text-xs text-primary-600 flex items-start gap-2">
                        <span className="w-5 h-5 shrink-0 rounded-full bg-primary-100 text-primary-500 flex items-center justify-center font-semibold">{i + 1}</span>
                        <span>
                          <span className="font-medium">{s.name}</span>
                          <span className="flex items-center gap-1 text-primary-400">{a.icon}{a.text}</span>
                        </span>
                      </li>
                    );
                  })}
                </ol>
                <Button size="sm" icon={<Play className="w-4 h-4" />} onClick={() => navigate(`/approvals/flows?template=${tpl.id}`)}>
                  Usar modelo
                </Button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
