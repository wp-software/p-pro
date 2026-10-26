import { Avatar, Card } from '@/components/ui';
import { ShareholderTypeBadge, ShareholderStatusBadge } from './ShareholderBadge';
import type { Shareholder } from '@/types';

interface ShareholderListProps {
  shareholders: Shareholder[];
  onView?: (shareholder: Shareholder) => void;
  onEdit?: (shareholder: Shareholder) => void;
}

export function ShareholderList({ shareholders, onView, onEdit }: ShareholderListProps) {
  return (
    <Card className="p-0 overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="text-left text-primary-500 border-b border-primary-100">
            <th className="px-4 py-3 font-medium">Nome</th>
            <th className="px-4 py-3 font-medium">Empresa</th>
            <th className="px-4 py-3 font-medium">Tipo</th>
            <th className="px-4 py-3 font-medium">Status</th>
            <th className="px-4 py-3 font-medium">Documento</th>
            <th className="px-4 py-3 font-medium">E-mail</th>
            <th className="px-4 py-3 font-medium">Telefone</th>
            <th className="px-4 py-3 font-medium">Cadastro</th>
            {onEdit && <th className="px-4 py-3" />}
          </tr>
        </thead>
        <tbody>
          {shareholders.map((s) => (
            <tr
              key={s.id}
              className="border-b border-primary-100 last:border-0 hover:bg-primary-50 cursor-pointer"
              onClick={() => onView?.(s)}
            >
              <td className="px-4 py-3">
                <div className="flex items-center gap-3 min-w-[180px]">
                  <Avatar name={s.name} size="sm" />
                  <span className="font-medium text-primary">{s.name}</span>
                </div>
              </td>
              <td className="px-4 py-3 text-primary-600">
                {s.companies && s.companies.length > 1
                  ? s.companies.map((c) => c.name).join(', ')
                  : s.companyName}
              </td>
              <td className="px-4 py-3">
                <ShareholderTypeBadge type={s.type} />
              </td>
              <td className="px-4 py-3">
                <ShareholderStatusBadge status={s.status} />
              </td>
              <td className="px-4 py-3 font-mono text-primary">{s.documentFormatted || s.document}</td>
              <td className="px-4 py-3 text-primary-600">{s.email}</td>
              <td className="px-4 py-3 text-primary-600">{s.phone}</td>
              <td className="px-4 py-3 text-primary-600">
                {new Date(s.createdAt).toLocaleDateString('pt-BR')}
              </td>
              {onEdit && (
                <td className="px-4 py-3 text-right">
                  <button
                    className="text-primary-600 hover:underline"
                    onClick={(e) => {
                      e.stopPropagation();
                      onEdit(s);
                    }}
                  >
                    Editar
                  </button>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </Card>
  );
}
