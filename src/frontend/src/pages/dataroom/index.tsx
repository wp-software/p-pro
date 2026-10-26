import { useState } from 'react';
import { FolderOpen, Folder, Plus, Trash2, FileText, Link, Unlink, Shield, History, UploadCloud } from 'lucide-react';
import { Button, Card, ConfirmDialog, Spinner } from '@/components/ui';
import {
  useDataRoomFolders,
  useDocumentsInFolder,
  useCreateFolder,
  useDeleteFolder,
  useAddDocumentToFolder,
  useUploadDocumentToFolder,
  useRemoveDocumentFromFolder,
  useSetFolderRoles,
  useDataRoomActivityLogs,
} from '@/hooks/useDataRoom';
import type { DataRoomFolder } from '@/types/phase6';
import { useConfirm } from '@/components/ui';
import { cn } from '@/utils/cn';
import { sanitizeUrl } from '@/utils/sanitizeHtml';
import { dataRoomService } from '@/services/dataRoomService';
import { useAuthStore } from '@/stores/authStore';

const ROLE_OPTIONS = [
  'Admin', 'Founder', 'BoardMember', 'Legal', 'Finance', 'HR', 'Employee', 'Investor', 'Viewer',
];

const ACTION_LABELS: Record<string, string> = {
  DocumentAdded: 'Documento adicionado',
  DocumentRemoved: 'Documento excluído',
  FolderCreated: 'Pasta criada',
  FolderDeleted: 'Pasta excluída',
  PermissionsChanged: 'Permissões alteradas',
};

function FolderRolesPanel({ folder, onClose }: { folder: DataRoomFolder; onClose: () => void }) {
  const [roles, setRoles] = useState<string[]>(folder.allowedRoles ?? []);
  const setFolderRoles = useSetFolderRoles();

  const toggle = (role: string) =>
    setRoles((prev) => (prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role]));

  return (
    <div className="mb-4 p-3 bg-primary-50 rounded-lg space-y-3">
      <p className="text-xs text-primary-500">
        Selecione os perfis com acesso a esta pasta (e subpastas). Sem nenhum perfil selecionado, todos os perfis têm
        acesso. Administradores sempre têm acesso.
      </p>
      <div className="flex flex-wrap gap-3">
        {ROLE_OPTIONS.map((role) => (
          <label key={role} className="flex items-center gap-1.5 text-sm text-primary-700">
            <input type="checkbox" checked={roles.includes(role)} onChange={() => toggle(role)} />
            {role}
          </label>
        ))}
      </div>
      <div className="flex gap-2">
        <Button
          size="sm"
          loading={setFolderRoles.isPending}
          onClick={() => setFolderRoles.mutate({ folderId: folder.id, roles }, { onSuccess: onClose })}
        >
          Salvar
        </Button>
        <Button size="sm" variant="secondary" onClick={onClose}>Cancelar</Button>
      </div>
    </div>
  );
}

function ActivityLogPanel() {
  const { data: logs = [], isLoading } = useDataRoomActivityLogs(true);
  if (isLoading) return <div className="flex justify-center py-8"><Spinner /></div>;
  if (logs.length === 0) {
    return <p className="text-sm text-primary-400 text-center py-8">Nenhuma atividade registrada</p>;
  }
  return (
    <div className="space-y-2">
      {logs.map((log) => (
        <div key={log.id} className="p-3 rounded-lg border border-primary-100 text-sm">
          <div className="flex justify-between gap-2">
            <span className="font-medium text-primary">{ACTION_LABELS[log.action] ?? log.action}</span>
            <span className="text-xs text-primary-400">{new Date(log.createdAt).toLocaleString('pt-BR')}</span>
          </div>
          <p className="text-xs text-primary-500">
            {log.documentName ? `${log.documentName} · ` : ''}Pasta: {log.folderName ?? '-'} · Por: {log.userName ?? log.userId}
            {log.details ? ` · ${log.details}` : ''}
          </p>
        </div>
      ))}
    </div>
  );
}

interface NewFolderFormProps {
  onSubmit: (name: string) => void;
  onCancel: () => void;
  isLoading: boolean;
}

function NewFolderForm({ onSubmit, onCancel, isLoading }: NewFolderFormProps) {
  const [name, setName] = useState('');

  return (
    <div className="flex items-center gap-2 p-2">
      <input
        autoFocus
        className="input flex-1 py-1.5 text-sm"
        placeholder="Nome da pasta"
        value={name}
        onChange={(e) => setName(e.target.value)}
        onKeyDown={(e) => {
          if (e.key === 'Enter' && name.trim()) onSubmit(name.trim());
          if (e.key === 'Escape') onCancel();
        }}
      />
      <Button size="sm" onClick={() => name.trim() && onSubmit(name.trim())} loading={isLoading}>
        Criar
      </Button>
      <Button size="sm" variant="secondary" onClick={onCancel} disabled={isLoading}>
        Cancelar
      </Button>
    </div>
  );
}

interface FolderTreeItemProps {
  folder: DataRoomFolder;
  selected: boolean;
  onSelect: (folder: DataRoomFolder) => void;
  onDelete: (folder: DataRoomFolder) => void;
}

function FolderTreeItem({ folder, selected, onSelect, onDelete }: FolderTreeItemProps) {
  return (
    <div
      className={cn(
        'flex items-center gap-2 px-3 py-2 rounded-lg cursor-pointer group transition-colors',
        selected ? 'bg-accent/10 text-accent' : 'hover:bg-primary-50 text-primary-700'
      )}
      onClick={() => onSelect(folder)}
    >
      {selected ? (
        <FolderOpen className="w-4 h-4 shrink-0" />
      ) : (
        <Folder className="w-4 h-4 shrink-0" />
      )}
      <span className="flex-1 text-sm truncate">{folder.name}</span>
      <button
        onClick={(e) => { e.stopPropagation(); onDelete(folder); }}
        className="opacity-0 group-hover:opacity-100 p-0.5 rounded text-red-400 hover:text-red-600 transition-all"
      >
        <Trash2 className="w-3.5 h-3.5" />
      </button>
    </div>
  );
}

interface AddDocumentFormProps {
  folderId: string;
  onClose: () => void;
}

function AddDocumentForm({ folderId, onClose }: AddDocumentFormProps) {
  const [docId, setDocId] = useState('');
  const addDoc = useAddDocumentToFolder();

  return (
    <div className="flex items-center gap-2 p-3 bg-primary-50 rounded-lg">
      <input
        autoFocus
        className="input flex-1 py-1.5 text-sm"
        placeholder="ID do documento"
        value={docId}
        onChange={(e) => setDocId(e.target.value)}
      />
      <Button
        size="sm"
        onClick={() => {
          if (docId.trim()) {
            addDoc.mutate({ folderId, documentId: docId.trim() }, { onSuccess: onClose });
          }
        }}
        loading={addDoc.isPending}
      >
        Adicionar
      </Button>
      <Button size="sm" variant="secondary" onClick={onClose}>Cancelar</Button>
    </div>
  );
}

function UploadDocumentForm({ folderId, onClose }: AddDocumentFormProps) {
  const [file, setFile] = useState<File | null>(null);
  const upload = useUploadDocumentToFolder();

  return (
    <form
      className="flex flex-wrap items-center gap-3 p-3 bg-primary-50 rounded-lg"
      onSubmit={(event) => {
        event.preventDefault();
        if (file) upload.mutate({ folderId, file }, { onSuccess: onClose });
      }}
    >
      <label className="flex-1 min-w-48 text-sm text-primary-600">
        <span className="sr-only">Selecionar arquivo</span>
        <input
          autoFocus
          required
          type="file"
          className="block w-full text-sm file:mr-3 file:rounded-md file:border-0 file:bg-white file:px-3 file:py-1.5 file:text-sm file:font-medium file:text-primary-700"
          onChange={(event) => setFile(event.target.files?.[0] ?? null)}
          disabled={upload.isPending}
        />
      </label>
      <Button size="sm" type="submit" disabled={!file} loading={upload.isPending}>
        Enviar
      </Button>
      <Button size="sm" type="button" variant="secondary" onClick={onClose} disabled={upload.isPending}>
        Cancelar
      </Button>
    </form>
  );
}

export default function DataRoomPage() {
  const { confirm, confirmState, handleCancel } = useConfirm();
  const [selectedFolderState, setSelectedFolder] = useState<DataRoomFolder | null>(null);
  const [showNewFolder, setShowNewFolder] = useState(false);
  const [showAddDoc, setShowAddDoc] = useState(false);
  const [showUploadDoc, setShowUploadDoc] = useState(false);
  const [showRoles, setShowRoles] = useState(false);
  const [showLogs, setShowLogs] = useState(false);
  const isAdmin = useAuthStore((s) =>
    s.user?.roles?.some((r) => r === 'Admin' || r === 'SuperAdmin') ?? false);

  const { data: folders = [], isLoading: foldersLoading } = useDataRoomFolders();
  const selectedFolder = folders.find((f) => f.id === selectedFolderState?.id) ?? selectedFolderState;
  const { data: documents = [], isLoading: docsLoading } = useDocumentsInFolder(selectedFolder?.id ?? '');
  const createFolder = useCreateFolder();
  const deleteFolder = useDeleteFolder();
  const removeDoc = useRemoveDocumentFromFolder();

  async function handleDeleteFolder(folder: DataRoomFolder) {
    const ok = await confirm({
      title: 'Excluir pasta',
      message: `Tem certeza que deseja excluir "${folder.name}"?`,
      confirmText: 'Excluir',
      confirmVariant: 'danger',
    });
    if (ok) {
      deleteFolder.mutate(folder.id);
      if (selectedFolder?.id === folder.id) setSelectedFolder(null);
    }
  }

  async function handleRemoveDoc(docId: string) {
    if (!selectedFolder) return;
    const ok = await confirm({
      title: 'Remover documento',
      message: 'Tem certeza que deseja remover este documento da pasta?',
      confirmText: 'Remover',
      confirmVariant: 'danger',
    });
    if (ok) {
      removeDoc.mutate({ folderId: selectedFolder.id, documentId: docId });
    }
  }

  return (
    <div className="space-y-6 animate-fade-in">
      <ConfirmDialog {...confirmState} onCancel={handleCancel} />
      <div>
        <h1 className="page-title">Data Room</h1>
        <p className="page-subtitle">Organize e compartilhe documentos com investidores</p>
        {isAdmin && (
          <button
            onClick={() => setShowLogs((v) => !v)}
            className="mt-2 flex items-center gap-1.5 text-xs text-accent hover:text-accent/80 font-medium"
          >
            <History className="w-3.5 h-3.5" />
            {showLogs ? 'Ocultar log de atividades' : 'Log de atividades'}
          </button>
        )}
      </div>

      <div className="flex gap-4 h-[600px]">
        {/* Folder tree */}
        <Card className="w-72 shrink-0 flex flex-col">
          <div className="p-3 border-b border-primary-100 flex items-center justify-between">
            <span className="font-semibold text-sm text-primary">Pastas</span>
            <button
              onClick={() => setShowNewFolder(true)}
              className="p-1 rounded-lg text-primary-500 hover:bg-primary-50 hover:text-primary transition-colors"
              title="Nova pasta"
            >
              <Plus className="w-4 h-4" />
            </button>
          </div>

          <div className="flex-1 overflow-y-auto p-2 space-y-0.5">
            {foldersLoading ? (
              <div className="flex justify-center py-4"><Spinner /></div>
            ) : folders.length === 0 && !showNewFolder ? (
              <div className="flex flex-col items-center justify-center h-32 text-primary-400 gap-2">
                <Folder className="w-8 h-8 opacity-40" />
                <p className="text-xs text-center">Nenhuma pasta criada</p>
              </div>
            ) : (
              folders.map((folder) => (
                <FolderTreeItem
                  key={folder.id}
                  folder={folder}
                  selected={selectedFolder?.id === folder.id}
                  onSelect={(f) => { setSelectedFolder(f); setShowRoles(false); setShowLogs(false); }}
                  onDelete={handleDeleteFolder}
                />
              ))
            )}

            {showNewFolder && (
              <NewFolderForm
                onSubmit={(name) => {
                  createFolder.mutate({ name }, { onSuccess: () => setShowNewFolder(false) });
                }}
                onCancel={() => setShowNewFolder(false)}
                isLoading={createFolder.isPending}
              />
            )}
          </div>
        </Card>

        {/* Documents panel */}
        <Card className="flex-1 flex flex-col">
          <div className="p-3 border-b border-primary-100 flex items-center justify-between">
            <span className="font-semibold text-sm text-primary">
              {selectedFolder ? selectedFolder.name : 'Selecione uma pasta'}
            </span>
            {selectedFolder && (
              <div className="flex items-center gap-4">
                {isAdmin && (
                  <button
                    onClick={() => setShowRoles((v) => !v)}
                    className="flex items-center gap-1.5 text-xs text-accent hover:text-accent/80 font-medium transition-colors"
                  >
                    <Shield className="w-3.5 h-3.5" />
                    Acesso por perfil
                  </button>
                )}
                <button
                  onClick={() => setShowUploadDoc(true)}
                  className="flex items-center gap-1.5 text-xs text-accent hover:text-accent/80 font-medium transition-colors"
                >
                  <UploadCloud className="w-3.5 h-3.5" />
                  Enviar arquivo
                </button>
                <button
                  onClick={() => setShowAddDoc(true)}
                  className="flex items-center gap-1.5 text-xs text-accent hover:text-accent/80 font-medium transition-colors"
                >
                  <Link className="w-3.5 h-3.5" />
                  Vincular existente
                </button>
              </div>
            )}
          </div>

          <div className="flex-1 overflow-y-auto p-4">
            {showLogs && isAdmin ? (
              <ActivityLogPanel />
            ) : !selectedFolder ? (
              <div className="flex flex-col items-center justify-center h-full text-primary-400 gap-2">
                <FolderOpen className="w-12 h-12 opacity-30" />
                <p className="text-sm">Selecione uma pasta para ver os documentos</p>
              </div>
            ) : docsLoading ? (
              <div className="flex justify-center py-8"><Spinner /></div>
            ) : (
              <>
                {showRoles && isAdmin && (
                  <FolderRolesPanel key={selectedFolder.id} folder={selectedFolder} onClose={() => setShowRoles(false)} />
                )}
                {showAddDoc && (
                  <div className="mb-4">
                    <AddDocumentForm folderId={selectedFolder.id} onClose={() => setShowAddDoc(false)} />
                  </div>
                )}
                {showUploadDoc && (
                  <div className="mb-4">
                    <UploadDocumentForm folderId={selectedFolder.id} onClose={() => setShowUploadDoc(false)} />
                  </div>
                )}
                {documents.length === 0 ? (
                  <div className="flex flex-col items-center justify-center h-40 text-primary-400 gap-2">
                    <FileText className="w-10 h-10 opacity-30" />
                    <p className="text-sm">Nenhum documento nesta pasta</p>
                  </div>
                ) : (
                  <div className="space-y-2">
                    {documents.map((doc) => (
                      <div
                        key={doc.id}
                        className="flex items-center gap-3 p-3 rounded-lg border border-primary-100 hover:bg-primary-50 transition-colors group"
                      >
                        <FileText className="w-4 h-4 text-blue-500 shrink-0" />
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-primary truncate">{doc.name}</p>
                          <p className="text-xs text-primary-400">{doc.documentType}</p>
                        </div>
                        {(doc as { hasStoredFile?: boolean }).hasStoredFile && (
                          <button
                            className="text-xs text-accent hover:underline shrink-0"
                            onClick={() => dataRoomService.downloadDocument(selectedFolder.id, doc.id, doc.fileName ?? doc.name)}
                          >
                            Download
                          </button>
                        )}
                        {sanitizeUrl(doc.downloadUrl) && (
                          <a
                            href={sanitizeUrl(doc.downloadUrl) ?? undefined}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-xs text-accent hover:underline shrink-0"
                            onClick={(e) => e.stopPropagation()}
                          >
                            Download
                          </a>
                        )}
                        <button
                          onClick={() => handleRemoveDoc(doc.id)}
                          className="shrink-0 p-1 rounded text-red-500 hover:text-red-700 transition-colors"
                          aria-label={`Remover ${doc.name} da pasta`}
                          title="Remover da pasta"
                        >
                          <Unlink className="w-3.5 h-3.5" />
                        </button>
                      </div>
                    ))}
                  </div>
                )}
              </>
            )}
          </div>
        </Card>
      </div>
    </div>
  );
}
