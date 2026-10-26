import { useState } from 'react';
import { FolderOpen, Folder, Plus, Trash2, FileText, Link, Unlink } from 'lucide-react';
import { Button, Card, Spinner } from '@/components/ui';
import {
  useDataRoomFolders,
  useDocumentsInFolder,
  useCreateFolder,
  useDeleteFolder,
  useAddDocumentToFolder,
  useRemoveDocumentFromFolder,
} from '@/hooks/useDataRoom';
import type { DataRoomFolder } from '@/types/phase6';
import { useConfirm } from '@/components/ui';
import { cn } from '@/utils/cn';
import { sanitizeUrl } from '@/utils/sanitizeHtml';

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

export default function DataRoomPage() {
  const { confirm } = useConfirm();
  const [selectedFolder, setSelectedFolder] = useState<DataRoomFolder | null>(null);
  const [showNewFolder, setShowNewFolder] = useState(false);
  const [showAddDoc, setShowAddDoc] = useState(false);

  const { data: folders = [], isLoading: foldersLoading } = useDataRoomFolders();
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
      <div>
        <h1 className="page-title">Data Room</h1>
        <p className="page-subtitle">Organize e compartilhe documentos com investidores</p>
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
                  onSelect={setSelectedFolder}
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
              <button
                onClick={() => setShowAddDoc(true)}
                className="flex items-center gap-1.5 text-xs text-accent hover:text-accent/80 font-medium transition-colors"
              >
                <Link className="w-3.5 h-3.5" />
                Adicionar documento
              </button>
            )}
          </div>

          <div className="flex-1 overflow-y-auto p-4">
            {!selectedFolder ? (
              <div className="flex flex-col items-center justify-center h-full text-primary-400 gap-2">
                <FolderOpen className="w-12 h-12 opacity-30" />
                <p className="text-sm">Selecione uma pasta para ver os documentos</p>
              </div>
            ) : docsLoading ? (
              <div className="flex justify-center py-8"><Spinner /></div>
            ) : (
              <>
                {showAddDoc && (
                  <div className="mb-4">
                    <AddDocumentForm folderId={selectedFolder.id} onClose={() => setShowAddDoc(false)} />
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
                          className="opacity-0 group-hover:opacity-100 p-1 rounded text-red-400 hover:text-red-600 transition-all"
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
