import { X, AlertTriangle } from 'lucide-react';
import { Button } from './index';

export interface ConfirmDialogProps {
  isOpen: boolean;
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  confirmVariant?: 'primary' | 'danger';
  onConfirm: () => void;
  onCancel: () => void;
  isLoading?: boolean;
}

export function ConfirmDialog({
  isOpen,
  title,
  message,
  confirmText = 'Confirmar',
  cancelText = 'Cancelar',
  confirmVariant = 'primary',
  onConfirm,
  onCancel,
  isLoading = false,
}: ConfirmDialogProps) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div className="bg-white rounded-lg max-w-md w-full shadow-xl animate-slide-in-top">
        <div className="p-6">
          <div className="flex items-start gap-4">
            <div className={`flex-shrink-0 w-12 h-12 rounded-full flex items-center justify-center ${
              confirmVariant === 'danger' ? 'bg-red-100' : 'bg-blue-100'
            }`}>
              <AlertTriangle className={`w-6 h-6 ${
                confirmVariant === 'danger' ? 'text-red-600' : 'text-blue-600'
              }`} />
            </div>
            <div className="flex-1">
              <h3 className="text-lg font-semibold text-gray-900 mb-2">
                {title}
              </h3>
              <p className="text-sm text-gray-600">
                {message}
              </p>
            </div>
            <button
              onClick={onCancel}
              disabled={isLoading}
              className="flex-shrink-0 text-gray-400 hover:text-gray-600 disabled:opacity-50"
            >
              <X className="w-5 h-5" />
            </button>
          </div>
        </div>

        <div className="bg-gray-50 px-6 py-4 flex gap-3 justify-end rounded-b-lg">
          <Button
            onClick={onCancel}
            disabled={isLoading}
            variant="secondary"
          >
            {cancelText}
          </Button>
          <Button
            onClick={onConfirm}
            disabled={isLoading}
            className={
              confirmVariant === 'danger'
                ? 'bg-red-600 hover:bg-red-700 text-white'
                : 'bg-blue-600 hover:bg-blue-700 text-white'
            }
            loading={isLoading}
          >
            {confirmText}
          </Button>
        </div>
      </div>
    </div>
  );
}

// Hook para gerenciar confirmações
export interface ConfirmOptions {
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  confirmVariant?: 'primary' | 'danger';
}

export interface ConfirmState extends ConfirmOptions {
  isOpen: boolean;
  isLoading: boolean;
  onConfirm: () => void | Promise<void>;
}

export function useConfirm() {
  const [confirmState, setConfirmState] = React.useState<ConfirmState>({
    isOpen: false,
    isLoading: false,
    title: '',
    message: '',
    onConfirm: () => {},
  });
  const resolveConfirmation = React.useRef<((confirmed: boolean) => void) | null>(null);

  const confirm = (options: ConfirmOptions): Promise<boolean> => {
    return new Promise((resolve) => {
      resolveConfirmation.current = resolve;
      setConfirmState({
        ...options,
        isOpen: true,
        isLoading: false,
        onConfirm: async () => {
          resolveConfirmation.current?.(true);
          resolveConfirmation.current = null;
          setConfirmState((prev) => ({ ...prev, isOpen: false }));
        },
      });
    });
  };

  const handleCancel = () => {
    resolveConfirmation.current?.(false);
    resolveConfirmation.current = null;
    setConfirmState((prev) => ({ ...prev, isOpen: false }));
  };

  return {
    confirmState,
    confirm,
    handleCancel,
  };
}

// Importação do React para o hook
import * as React from 'react';
