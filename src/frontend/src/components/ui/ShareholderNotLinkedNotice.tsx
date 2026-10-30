import { UserX } from 'lucide-react';
import { Card } from '@/components/ui';

export function ShareholderNotLinkedNotice() {
  return (
    <div className="flex items-center justify-center py-20">
      <Card className="p-8 text-center max-w-md">
        <UserX className="w-12 h-12 text-gray-300 mx-auto mb-3" />
        <h3 className="font-semibold text-gray-700 mb-1">Usuário não vinculado a nenhum sócio</h3>
        <p className="text-gray-400 text-sm">
          Seu usuário ainda não está vinculado a um cadastro de sócio. Peça a um administrador para
          fazer o vínculo na edição do usuário.
        </p>
      </Card>
    </div>
  );
}
