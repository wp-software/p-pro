import { useRef, useState, type DragEvent } from 'react';
import { X, UploadCloud, FileText, Download, Trash2 } from 'lucide-react';
import { Button, ConfirmDialog, Spinner, useConfirm } from '@/components/ui';
import {
  useFinancialDocuments,
  useUploadFinancialDocument,
  useRemoveFinancialDocument,
} from '@/hooks/useDataRoom';
import { dataRoomService } from '@/services/dataRoomService';

const MAX_MB = 50;

const DOC_TYPES = [
  { value: 'other', label: 'Outro' },
  { value: 'balance_sheet', label: 'Balanço Patrimonial' },
  { value: 'income_statement', label: 'DRE' },
  { value: 'cash_flow', label: 'Fluxo de Caixa' },
  { value: 'audit_report', label: 'Relatório de Auditoria' },
];

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

interface Props {
  periodId: string;
  year: number;
  month: number;
  /** Indicador alvo; null = documentos gerais do período */
  metricKey: string | null;
  title: string;
  canUpload: boolean;
  onClose: () => void;
}

export default function MetricDocumentsDrawer({ periodId, year, month, metricKey, title, canUpload, onClose }: Props) {
  const { confirm, confirmState, handleCancel } = useConfirm();
  const inputRef = useRef<HTMLInputElement>(null);
  const [file, setFile] = useState<File | null>(null);
  const [documentType, setDocumentType] = useState('other');
  const [dragOver, setDragOver] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const { data: all = [], isLoading } = useFinancialDocuments(periodId);
  const upload = useUploadFinancialDocument();
  const remove = useRemoveFinancialDocument();
  const docs = all.filter((d) => (d.metricKey ?? null) === metricKey);

  function pick(f: File | undefined) {
    if (!f) return;
    if (f.size > MAX_MB * 1024 * 1024) {
      setError(`Arquivo excede o limite de ${MAX_MB} MB`);
      return;
    }
    setError(null);
    setFile(f);
  }

  function onDrop(e: DragEvent<HTMLDivElement>) {
    e.preventDefault();
    setDragOver(false);
    pick(e.dataTransfer.files[0]);
  }

  function send() {
    if (!file) return;
    upload.mutate(
      { file, year, month, periodId, metricKey: metricKey ?? undefined, documentType },
      {
        onSuccess: () => {
          setFile(null);
          if (inputRef.current) inputRef.current.value = '';
        },
      }
    );
  }

  async function handleRemove(documentId: string, name: string) {
    const ok = await confirm({
      title: 'Remover documento',
      message: `Remover "${name}" do indicador e do Data Room?`,
      confirmText: 'Remover',
      confirmVariant: 'danger',
    });
    if (ok) remove.mutate(documentId);
  }

  return (
    <>
      <div className="fixed inset-0 z-50 flex justify-end" role="dialog" aria-modal="true" aria-label={`Documentos — ${title}`}>
        <div className="absolute inset-0 bg-black/40" onClick={onClose} />
        <aside className="relative w-full max-w-md bg-white shadow-xl flex flex-col animate-fade-in">
          <header className="flex items-start justify-between gap-3 p-5 border-b border-gray-100">
            <div>
              <h2 className="text-base font-semibold text-gray-900">Documentos</h2>
              <p className="text-sm text-gray-500">
                {title} · {String(month).padStart(2, '0')}/{year}
              </p>
            </div>
            <button onClick={onClose} className="p-1.5 rounded-lg text-gray-400 hover:bg-gray-100" aria-label="Fechar">
              <X size={18} />
            </button>
          </header>

          <div className="flex-1 overflow-y-auto p-5 space-y-5">
          {canUpload && (
            <div className="space-y-3">
              {!file ? (
                <div
                  onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
                  onDragLeave={() => setDragOver(false)}
                  onDrop={onDrop}
                  onClick={() => inputRef.current?.click()}
                  className={`border-2 border-dashed rounded-xl p-6 text-center cursor-pointer transition-colors ${
                    dragOver ? 'border-blue-400 bg-blue-50' : 'border-gray-200 hover:border-blue-300 hover:bg-gray-50'
                  }`}
                >
                  <UploadCloud className="w-7 h-7 mx-auto mb-2 text-gray-400" />
                  <p className="text-sm text-gray-600">
                    Arraste um arquivo ou <span className="text-blue-600 font-medium">clique para selecionar</span>
                  </p>
                  <p className="text-xs text-gray-400 mt-1">Máx. {MAX_MB} MB · salvo em Data Room / Financeiro / {String(month).padStart(2, '0')}-{year}</p>
                </div>
              ) : (
                <div className="flex items-center gap-3 p-3 bg-blue-50 border border-blue-200 rounded-xl">
                  <FileText className="w-5 h-5 text-blue-500 shrink-0" />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-blue-900 truncate">{file.name}</p>
                    <p className="text-xs text-blue-500">{formatSize(file.size)}</p>
                  </div>
                  <button onClick={() => setFile(null)} className="text-blue-400 hover:text-blue-700" aria-label="Descartar">
                    <X size={16} />
                  </button>
                </div>
              )}
              <input ref={inputRef} type="file" className="hidden" onChange={(e) => pick(e.target.files?.[0])} />
              {error && <p className="text-xs text-red-600">{error}</p>}
              {file && (
                <div className="flex items-center gap-2">
                  <select
                    value={documentType}
                    onChange={(e) => setDocumentType(e.target.value)}
                    className="flex-1 px-3 py-2 text-sm border border-gray-200 rounded-lg"
                    aria-label="Tipo do documento"
                  >
                    {DOC_TYPES.map((t) => (
                      <option key={t.value} value={t.value}>{t.label}</option>
                    ))}
                  </select>
                  <Button size="sm" onClick={send} loading={upload.isPending}>Enviar</Button>
                </div>
              )}
            </div>
          )}

          {!canUpload && (
            <p className="text-xs text-gray-500 bg-gray-50 rounded-lg p-3">
              Período aprovado/bloqueado: novos anexos não são permitidos.
            </p>
          )}

          <div>
            <h3 className="text-xs font-semibold uppercase tracking-wide text-gray-500 mb-2">
              Anexados ({docs.length})
            </h3>
            {isLoading ? (
              <div className="flex justify-center py-6"><Spinner /></div>
            ) : docs.length === 0 ? (
              <p className="text-sm text-gray-400 text-center py-6">Nenhum documento anexado</p>
            ) : (
              <ul className="space-y-2">
                {docs.map((d) => (
                  <li key={d.documentId} className="flex items-center gap-3 p-3 rounded-lg border border-gray-100">
                    <FileText className="w-4 h-4 text-blue-500 shrink-0" />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-gray-900 truncate">{d.name}</p>
                      <p className="text-xs text-gray-400">
                        {formatSize(d.fileSizeBytes)} · {new Date(d.createdAt).toLocaleDateString('pt-BR')}
                      </p>
                    </div>
                    <button
                      onClick={() => dataRoomService.downloadDocument(d.folderId, d.documentId, d.fileName || d.name)}
                      className="p-1.5 rounded text-gray-400 hover:text-blue-600 hover:bg-gray-50"
                      title="Baixar"
                      aria-label={`Baixar ${d.name}`}
                    >
                      <Download size={15} />
                    </button>
                    {canUpload && (
                      <button
                        onClick={() => handleRemove(d.documentId, d.name)}
                        className="p-1.5 rounded text-gray-400 hover:text-red-600 hover:bg-gray-50"
                        title="Remover"
                        aria-label={`Remover ${d.name}`}
                      >
                        <Trash2 size={15} />
                      </button>
                    )}
                  </li>
                ))}
              </ul>
            )}
          </div>
          </div>
        </aside>
      </div>
      <ConfirmDialog {...confirmState} onCancel={handleCancel} />
    </>
  );
}
