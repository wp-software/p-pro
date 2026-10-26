import { FileText, FileImage, FileArchive, File, Trash2, ShieldCheck, Download, ExternalLink } from 'lucide-react';
import { Badge } from '@/components/ui';
import { documentVisibilityLabels, documentVisibilityColors, type Document } from '@/types';
import { formatDate } from '@/utils/format';
import { sanitizeUrl } from '@/utils/sanitizeHtml';

// ─── MIME → ícone ─────────────────────────────────────────────────────────────

function getMimeIcon(mimeType: string) {
  if (mimeType.startsWith('image/')) return <FileImage className="w-5 h-5 text-purple-500" />;
  if (mimeType === 'application/pdf') return <FileText className="w-5 h-5 text-red-500" />;
  if (
    mimeType.includes('zip') ||
    mimeType.includes('gzip') ||
    mimeType.includes('tar')
  )
    return <FileArchive className="w-5 h-5 text-yellow-500" />;
  if (mimeType.includes('spreadsheet') || mimeType.includes('excel'))
    return <FileText className="w-5 h-5 text-green-500" />;
  if (mimeType.includes('word') || mimeType.includes('document'))
    return <FileText className="w-5 h-5 text-blue-500" />;
  return <File className="w-5 h-5 text-gray-500" />;
}

// ─── Props ────────────────────────────────────────────────────────────────────

export interface DocumentCardProps {
  document: Document;
  readonly?: boolean;
  onVerify?: (id: string) => void;
  onDelete?: (id: string) => void;
  isVerifying?: boolean;
  isDeleting?: boolean;
}

// ─── Component ────────────────────────────────────────────────────────────────

export default function DocumentCard({
  document,
  readonly = false,
  onVerify,
  onDelete,
  isVerifying = false,
  isDeleting = false,
}: DocumentCardProps) {
  const downloadUrl = sanitizeUrl(document.downloadUrl);
  const storageUrl = sanitizeUrl(document.storagePath);

  return (
    <div className="flex items-start gap-3 p-3 bg-white border border-gray-100 rounded-xl hover:border-blue-200 hover:shadow-sm transition-all">
      {/* Icon */}
      <div className="flex-shrink-0 w-10 h-10 flex items-center justify-center bg-gray-50 rounded-lg border border-gray-100">
        {getMimeIcon(document.mimeType)}
      </div>

      {/* Info */}
      <div className="flex-1 min-w-0">
        <p className="text-sm font-medium text-gray-900 truncate">{document.name}</p>
        <p className="text-xs text-gray-400 truncate">{document.fileName}</p>
        <div className="flex items-center gap-2 mt-1 flex-wrap">
          <span className="text-xs text-gray-400">{document.fileSizeFormatted}</span>
          <span className="text-xs text-gray-300">·</span>
          <span className="text-xs text-gray-400">{formatDate(document.createdAt)}</span>
          <Badge
            className={`text-xs ${documentVisibilityColors[document.visibility] ?? 'bg-gray-100 text-gray-600'}`}
          >
            {documentVisibilityLabels[document.visibility] ?? document.visibility}
          </Badge>
          {document.isVerified && (
            <Badge className="text-xs bg-green-50 text-green-700 flex items-center gap-1">
              <ShieldCheck className="w-3 h-3" />
              Verificado
            </Badge>
          )}
        </div>
      </div>

      {/* Actions */}
      <div className="flex items-center gap-1 flex-shrink-0">
        {downloadUrl && (
          <a
            href={downloadUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="p-1.5 text-gray-400 hover:text-blue-600 rounded-lg hover:bg-blue-50 transition-colors"
            title="Baixar"
          >
            <Download className="w-4 h-4" />
          </a>
        )}
        {storageUrl && !downloadUrl && (
          <a
            href={storageUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="p-1.5 text-gray-400 hover:text-blue-600 rounded-lg hover:bg-blue-50 transition-colors"
            title="Abrir"
          >
            <ExternalLink className="w-4 h-4" />
          </a>
        )}
        {!readonly && !document.isVerified && onVerify && (
          <button
            onClick={() => onVerify(document.id)}
            disabled={isVerifying}
            className="p-1.5 text-gray-400 hover:text-green-600 rounded-lg hover:bg-green-50 transition-colors disabled:opacity-50"
            title="Verificar"
          >
            <ShieldCheck className="w-4 h-4" />
          </button>
        )}
        {!readonly && onDelete && (
          <button
            onClick={() => onDelete(document.id)}
            disabled={isDeleting}
            className="p-1.5 text-gray-400 hover:text-red-600 rounded-lg hover:bg-red-50 transition-colors disabled:opacity-50"
            title="Excluir"
          >
            <Trash2 className="w-4 h-4" />
          </button>
        )}
      </div>
    </div>
  );
}
