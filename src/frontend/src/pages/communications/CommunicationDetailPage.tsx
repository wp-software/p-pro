import { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Eye, Calendar, Pin, Globe } from 'lucide-react';
import { Card, Spinner } from '@/components/ui';
import { useCommunication } from '@/hooks/useCommunications';
import { communicationService } from '@/services/communicationService';
import { formatDate } from '@/utils/format';
import { cn } from '@/utils/cn';
import { sanitizeHtml } from '@/utils/sanitizeHtml';

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

const VISIBILITY_LABELS: Record<string, string> = {
  all: 'Todos',
  investors: 'Investidores',
  founders: 'Fundadores',
  employees: 'Colaboradores',
  specific: 'Específico',
};

export default function CommunicationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: comm, isLoading } = useCommunication(id!);

  useEffect(() => {
    if (id) {
      communicationService.trackView(id).catch(() => {});
    }
  }, [id]);

  if (isLoading) {
    return (
      <div className="flex justify-center py-20">
        <Spinner />
      </div>
    );
  }

  if (!comm) {
    return (
      <div className="flex flex-col items-center justify-center py-20 text-primary-400 gap-3">
        <p className="text-sm">Comunicação não encontrada</p>
        <button onClick={() => navigate('/communications')} className="text-accent text-sm hover:underline">
          Voltar para comunicações
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6 animate-fade-in max-w-3xl">
      {/* Back button */}
      <button
        onClick={() => navigate('/communications')}
        className="flex items-center gap-2 text-sm text-primary-500 hover:text-primary transition-colors"
      >
        <ArrowLeft className="w-4 h-4" />
        Voltar para comunicações
      </button>

      {/* Header */}
      <div>
        <div className="flex items-center gap-2 mb-2">
          {comm.isPinned && <Pin className="w-4 h-4 text-amber-500" />}
          <span className={cn('px-2.5 py-1 rounded-full text-xs font-medium', TYPE_COLORS[comm.commType] || 'bg-gray-100 text-gray-600')}>
            {TYPE_LABELS[comm.commType] || comm.commType}
          </span>
        </div>
        <h1 className="page-title">{comm.title}</h1>
        {comm.summary && <p className="text-primary-500 mt-1">{comm.summary}</p>}
      </div>

      {/* Meta */}
      <div className="flex flex-wrap gap-4 text-sm text-primary-500">
        {comm.publishedAt && (
          <span className="flex items-center gap-1.5">
            <Calendar className="w-4 h-4" />
            Publicado em {formatDate(comm.publishedAt)}
          </span>
        )}
        {comm.expiresAt && (
          <span className="flex items-center gap-1.5">
            <Calendar className="w-4 h-4 text-red-400" />
            Expira em {formatDate(comm.expiresAt)}
          </span>
        )}
        <span className="flex items-center gap-1.5">
          <Globe className="w-4 h-4" />
          {VISIBILITY_LABELS[comm.visibility] || comm.visibility}
        </span>
        <span className="flex items-center gap-1.5">
          <Eye className="w-4 h-4" />
          {comm.viewsCount} visualizações
        </span>
      </div>

      {/* Content */}
      <Card>
        <div className="p-6">
          {comm.contentHtml ? (
            <div
              className="prose prose-primary max-w-none"
              dangerouslySetInnerHTML={{ __html: sanitizeHtml(comm.contentHtml) }}
            />
          ) : (
            <p className="text-primary-700 whitespace-pre-wrap leading-relaxed">{comm.content}</p>
          )}
        </div>
      </Card>
    </div>
  );
}
