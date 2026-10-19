import { Mail, Phone, MoreVertical, Building2 } from 'lucide-react';
import { Card, Button, Avatar } from '@/components/ui';
import { ShareholderTypeBadge, ShareholderStatusBadge } from './ShareholderBadge';
import type { Shareholder } from '@/types';

interface ShareholderCardProps {
  shareholder: Shareholder;
  onView?: (shareholder: Shareholder) => void;
  onEdit?: (shareholder: Shareholder) => void;
  onDelete?: (shareholder: Shareholder) => void;
}

export function ShareholderCard({
  shareholder,
  onView,
  onEdit: _onEdit,
  onDelete: _onDelete,
}: ShareholderCardProps) {
  return (
    <Card
      className="relative group hover:shadow-md transition-shadow cursor-pointer"
      onClick={() => onView?.(shareholder)}
    >
      {/* Menu Button */}
      <div className="absolute top-4 right-4">
        <div className="relative">
          <button
            className="p-1.5 rounded-lg opacity-0 group-hover:opacity-100 hover:bg-primary-100 transition-all"
            onClick={(e) => {
              e.stopPropagation();
              // Toggle dropdown menu
            }}
          >
            <MoreVertical className="w-5 h-5 text-primary-400" />
          </button>
          {/* Dropdown menu would go here */}
        </div>
      </div>

      {/* Header */}
      <div className="flex items-start gap-4 mb-4">
        <Avatar name={shareholder.name} size="lg" />
        <div className="flex-1 min-w-0">
          <h3 className="font-semibold text-primary truncate">
            {shareholder.name}
          </h3>
          <p className="text-sm text-primary-500 truncate flex items-center gap-1">
            <Building2 className="w-3.5 h-3.5" />
            {shareholder.companies && shareholder.companies.length > 1
              ? shareholder.companies.map((c) => c.name).join(', ')
              : shareholder.companyName}
          </p>
          <div className="flex items-center gap-2 mt-2 flex-wrap">
            <ShareholderTypeBadge type={shareholder.type} />
            <ShareholderStatusBadge status={shareholder.status} />
          </div>
        </div>
      </div>

      {/* Details */}
      <div className="space-y-3 py-4 border-t border-primary-100">
        <div className="flex justify-between text-sm">
          <span className="text-primary-500">Documento</span>
          <span className="font-medium text-primary font-mono">
            {shareholder.documentFormatted || shareholder.document}
          </span>
        </div>
        {shareholder.email && (
          <div className="flex justify-between text-sm">
            <span className="text-primary-500">E-mail</span>
            <span className="font-medium text-primary truncate max-w-[180px]">
              {shareholder.email}
            </span>
          </div>
        )}
        {shareholder.phone && (
          <div className="flex justify-between text-sm">
            <span className="text-primary-500">Telefone</span>
            <span className="font-medium text-primary">
              {shareholder.phone}
            </span>
          </div>
        )}
        <div className="flex justify-between text-sm">
          <span className="text-primary-500">Cadastro</span>
          <span className="font-medium text-primary">
            {new Date(shareholder.createdAt).toLocaleDateString('pt-BR')}
          </span>
        </div>
      </div>

      {/* Actions */}
      <div className="flex gap-2 pt-4 border-t border-primary-100">
        <Button
          variant="secondary"
          size="sm"
          className="flex-1"
          onClick={(e) => {
            e.stopPropagation();
            onView?.(shareholder);
          }}
        >
          Ver Detalhes
        </Button>
        {_onEdit && (
          <Button
            variant="ghost"
            size="sm"
            className="btn-icon"
            onClick={(e) => {
              e.stopPropagation();
              _onEdit(shareholder);
            }}
          >
            Editar
          </Button>
        )}
        {shareholder.email && (
          <Button
            variant="ghost"
            size="sm"
            className="btn-icon"
            onClick={(e) => {
              e.stopPropagation();
              window.location.href = `mailto:${shareholder.email}`;
            }}
            title="Enviar e-mail"
          >
            <Mail className="w-5 h-5" />
          </Button>
        )}
        {shareholder.phone && (
          <Button
            variant="ghost"
            size="sm"
            className="btn-icon"
            onClick={(e) => {
              e.stopPropagation();
              window.location.href = `tel:${shareholder.phone}`;
            }}
            title="Fazer chamada"
          >
            <Phone className="w-5 h-5" />
          </Button>
        )}
      </div>
    </Card>
  );
}
