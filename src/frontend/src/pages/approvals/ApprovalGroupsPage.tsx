import { useState } from 'react';
import { Plus, Users, Pencil, Trash2, X } from 'lucide-react';
import { Button, Card, Spinner } from '@/components/ui';
import { useUsers } from '@/hooks/useUsers';
import {
  useApprovalGroups,
  useSaveApprovalGroup,
  useDeleteApprovalGroup,
} from '@/hooks/useApprovalConfig';
import type { ApprovalGroup, ApprovalMode } from '@/types/phase6';
import { cn } from '@/utils/cn';

export const APPROVAL_MODE_LABELS: Record<ApprovalMode, string> = {
  any: 'Qualquer um dos membros',
  all: 'Todos os membros',
};

const APPROVAL_MODE_HINTS: Record<ApprovalMode, string> = {
  any: 'A etapa avança assim que um membro aprovar.',
  all: 'A etapa só avança quando todos os membros aprovarem.',
};

interface GroupModalProps {
  group?: ApprovalGroup;
  onClose: () => void;
}

function GroupModal({ group, onClose }: GroupModalProps) {
  const save = useSaveApprovalGroup();
  const { data: usersData, isLoading: loadingUsers } = useUsers({ page: 1, pageSize: 200 });
  const [name, setName] = useState(group?.name ?? '');
  const [description, setDescription] = useState(group?.description ?? '');
  const [mode, setMode] = useState<ApprovalMode>(group?.approvalMode ?? 'any');
  const [isActive, setIsActive] = useState(group?.isActive ?? true);
  const [memberIds, setMemberIds] = useState<string[]>(group?.members.map((m) => m.userId) ?? []);
  const [search, setSearch] = useState('');

  const users = (usersData?.items ?? []).filter(
    (u) =>
      u.status?.toLowerCase() === 'active' &&
      (u.name.toLowerCase().includes(search.toLowerCase()) ||
        u.email.toLowerCase().includes(search.toLowerCase())),
  );

  function toggle(id: string) {
    setMemberIds((ids) => (ids.includes(id) ? ids.filter((x) => x !== id) : [...ids, id]));
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!name.trim() || memberIds.length === 0) return;
    save.mutate(
      {
        id: group?.id,
        data: { name: name.trim(), description: description.trim() || undefined, approvalMode: mode, isActive, memberIds },
      },
      { onSuccess: onClose },
    );
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/40 overflow-y-auto">
      <div className="bg-white rounded-2xl shadow-xl w-full max-w-lg my-4">
        <div className="flex items-center justify-between px-6 py-4 border-b border-primary-100">
          <h3 className="font-semibold text-primary text-lg">
            {group ? 'Editar grupo de aprovação' : 'Novo grupo de aprovação'}
          </h3>
          <button onClick={onClose} className="text-primary-400 hover:text-primary" aria-label="Fechar">
            <X className="w-5 h-5" />
          </button>
        </div>
        <form onSubmit={handleSubmit}>
          <div className="px-6 py-4 space-y-4 max-h-[70vh] overflow-y-auto">
            <div>
              <label className="input-label">Nome <span className="text-red-500">*</span></label>
              <input className="input" value={name} onChange={(e) => setName(e.target.value)} placeholder="Ex.: Conselho, Jurídico + Financeiro" required />
            </div>
            <div>
              <label className="input-label">Descrição</label>
              <input className="input" value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Opcional" />
            </div>
            <div>
              <label className="input-label">Regra de aprovação</label>
              <div className="grid grid-cols-2 gap-2">
                {(['any', 'all'] as ApprovalMode[]).map((m) => (
                  <button
                    key={m}
                    type="button"
                    onClick={() => setMode(m)}
                    className={cn(
                      'text-left border rounded-xl p-3 transition-colors',
                      mode === m ? 'border-accent bg-accent/5' : 'border-primary-200 hover:border-accent',
                    )}
                  >
                    <p className="text-sm font-medium text-primary">{APPROVAL_MODE_LABELS[m]}</p>
                    <p className="text-xs text-primary-500 mt-0.5">{APPROVAL_MODE_HINTS[m]}</p>
                  </button>
                ))}
              </div>
              <p className="text-xs text-primary-400 mt-1.5">Em qualquer regra, uma rejeição de um membro encerra o fluxo.</p>
            </div>
            <label className="flex items-center gap-2 text-sm text-primary-600">
              <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
              Grupo ativo (disponível para novos fluxos)
            </label>
            <div>
              <label className="input-label">
                Membros <span className="text-red-500">*</span>
                <span className="ml-2 text-xs font-normal text-primary-400">{memberIds.length} selecionado(s)</span>
              </label>
              <input className="input text-sm mb-2" placeholder="Buscar usuário..." value={search} onChange={(e) => setSearch(e.target.value)} />
              <div className="border border-primary-100 rounded-xl max-h-52 overflow-y-auto divide-y divide-primary-50">
                {loadingUsers ? (
                  <div className="flex justify-center py-6"><Spinner /></div>
                ) : users.length === 0 ? (
                  <p className="text-xs text-primary-400 p-3">Nenhum usuário encontrado.</p>
                ) : (
                  users.map((u) => (
                    <label key={u.id} className="flex items-center gap-3 px-3 py-2 cursor-pointer hover:bg-primary-50">
                      <input type="checkbox" checked={memberIds.includes(u.id)} onChange={() => toggle(u.id)} />
                      <span className="text-sm text-primary">{u.name}</span>
                      <span className="text-xs text-primary-400 truncate">{u.email}</span>
                    </label>
                  ))
                )}
              </div>
            </div>
          </div>
          <div className="flex justify-end gap-3 px-6 py-4 border-t border-primary-100">
            <Button variant="secondary" type="button" onClick={onClose} disabled={save.isPending}>Cancelar</Button>
            <Button type="submit" loading={save.isPending} disabled={!name.trim() || memberIds.length === 0}>
              Salvar grupo
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default function ApprovalGroupsPage() {
  const { data: groups = [], isLoading } = useApprovalGroups();
  const deleteGroup = useDeleteApprovalGroup();
  const [editing, setEditing] = useState<ApprovalGroup | 'new' | null>(null);

  function handleDelete(g: ApprovalGroup) {
    if (window.confirm(`Remover o grupo "${g.name}"? Etapas de modelos que o usam ficarão sem aprovador.`)) {
      deleteGroup.mutate(g.id);
    }
  }

  return (
    <div className="space-y-6 animate-fade-in">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <div>
          <h1 className="page-title">Grupos de aprovação</h1>
          <p className="page-subtitle">Conjuntos de aprovadores reutilizáveis nas etapas dos fluxos.</p>
        </div>
        <Button icon={<Plus className="w-4 h-4" />} onClick={() => setEditing('new')}>Novo grupo</Button>
      </div>

      {isLoading ? (
        <div className="flex justify-center py-12"><Spinner /></div>
      ) : groups.length === 0 ? (
        <Card>
          <div className="flex flex-col items-center py-16 text-primary-400 gap-3">
            <Users className="w-10 h-10 opacity-40" />
            <p className="text-sm font-medium">Nenhum grupo criado</p>
            <p className="text-xs">Crie um grupo para exigir aprovação coletiva em uma etapa.</p>
          </div>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {groups.map((g) => (
            <Card key={g.id}>
              <div className="p-4 space-y-3">
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <h3 className="font-semibold text-primary text-sm truncate">{g.name}</h3>
                    {g.description && <p className="text-xs text-primary-500 mt-0.5">{g.description}</p>}
                  </div>
                  <div className="flex gap-1 shrink-0">
                    <button onClick={() => setEditing(g)} className="p-1.5 text-primary-400 hover:text-accent" aria-label="Editar">
                      <Pencil className="w-4 h-4" />
                    </button>
                    <button onClick={() => handleDelete(g)} className="p-1.5 text-primary-400 hover:text-red-500" aria-label="Remover">
                      <Trash2 className="w-4 h-4" />
                    </button>
                  </div>
                </div>
                <div className="flex flex-wrap gap-1.5">
                  <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium', g.approvalMode === 'all' ? 'bg-purple-100 text-purple-700' : 'bg-blue-100 text-blue-700')}>
                    {APPROVAL_MODE_LABELS[g.approvalMode]}
                  </span>
                  {!g.isActive && <span className="px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-500">Inativo</span>}
                </div>
                <div className="flex flex-wrap gap-1.5">
                  {g.members.map((m) => (
                    <span key={m.userId} className="px-2 py-0.5 rounded-full text-xs bg-primary-50 text-primary-600">{m.name}</span>
                  ))}
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      {editing && (
        <GroupModal group={editing === 'new' ? undefined : editing} onClose={() => setEditing(null)} />
      )}
    </div>
  );
}
