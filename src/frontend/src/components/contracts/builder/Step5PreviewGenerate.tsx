// F3-BLD-FE-005: Contract Builder Step 5 - Preview & Generate
// File: src/frontend/src/components/contracts/builder/Step5PreviewGenerate.tsx
// Author: GitHub Copilot
// Date: 25/02/2026

import React, { useEffect, useState } from 'react';
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle,
  Download,
  Eye,
  FileText,
  Loader2,
} from 'lucide-react';
import { Button, Card } from '@/components/ui';
import { sanitizeHtml } from '@/utils/sanitizeHtml';
import { contractBuilderService } from '@/services/contractService';

interface Step5PreviewGenerateProps {
  sessionId: string;
  onContractGenerated: (contractId: string) => void;
  onGoBack: () => void;
}

export const Step5PreviewGenerate: React.FC<Step5PreviewGenerateProps> = ({
  sessionId,
  onContractGenerated,
  onGoBack,
}) => {
  const [htmlContent, setHtmlContent] = useState<string | null>(null);
  const [missingVariables, setMissingVariables] = useState<string[]>([]);
  const [isLoadingPreview, setIsLoadingPreview] = useState(true);
  const [isGenerating, setIsGenerating] = useState(false);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [generateError, setGenerateError] = useState<string | null>(null);

  useEffect(() => {
    const loadPreview = async () => {
      try {
        setIsLoadingPreview(true);
        setPreviewError(null);
        const result = await contractBuilderService.previewContract({ sessionId });
        setHtmlContent(result.htmlPreview);
        setMissingVariables(result.validationErrors ?? []);
      } catch (err) {
        console.error('Erro ao carregar preview:', err);
        setPreviewError('Não foi possível carregar o preview do contrato. Verifique os dados das etapas anteriores.');
      } finally {
        setIsLoadingPreview(false);
      }
    };

    loadPreview();
  }, [sessionId]);

  const handleGenerate = async () => {
    try {
      setIsGenerating(true);
      setGenerateError(null);
      const contract = await contractBuilderService.generateContract({ sessionId, sendForSignature: false });
      onContractGenerated(contract.contractId);
    } catch (err) {
      console.error('Erro ao gerar contrato:', err);
      setGenerateError('Erro ao gerar o contrato. Tente novamente.');
      setIsGenerating(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header - same pattern as all other steps */}
      <div className="text-center mb-8">
        <Eye className="w-16 h-16 text-cyan-600 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-gray-900 mb-2">
          Revise e gere seu contrato
        </h3>
        <p className="text-sm text-gray-500">
          Confira o preview abaixo e clique em <strong>Gerar Contrato</strong> para finalizar.
        </p>
      </div>

      {/* Status banners */}
      {missingVariables.length > 0 && (
        <div className="flex items-start gap-3 p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
          <AlertCircle className="w-5 h-5 text-yellow-600 mt-0.5 shrink-0" />
          <div>
            <p className="text-sm font-medium text-yellow-800">Variáveis não preenchidas</p>
            <p className="text-sm text-yellow-700 mt-1">
              As seguintes variáveis não foram preenchidas e aparecerão com o placeholder no documento:
            </p>
            <ul className="mt-2 flex flex-wrap gap-2">
              {missingVariables.map((v) => (
                <li
                  key={v}
                  className="px-2 py-0.5 text-xs font-mono bg-yellow-100 text-yellow-900 border border-yellow-300 rounded"
                >
                  {`{{${v}}}`}
                </li>
              ))}
            </ul>
          </div>
        </div>
      )}

      {!isLoadingPreview && !previewError && missingVariables.length === 0 && (
        <div className="flex items-center gap-3 p-4 bg-green-50 border border-green-200 rounded-lg">
          <CheckCircle className="w-5 h-5 text-green-600 shrink-0" />
          <p className="text-sm text-green-800 font-medium">
            Todas as variáveis estão preenchidas. O contrato está pronto para geração.
          </p>
        </div>
      )}

      {generateError && (
        <div className="flex items-start gap-3 p-4 bg-red-50 border border-red-200 rounded-lg">
          <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 shrink-0" />
          <p className="text-sm text-red-700">{generateError}</p>
        </div>
      )}

      {/* Preview card - same Card component as other steps use for their content */}
      <Card className="p-6">
        <h4 className="text-md font-semibold text-gray-900 mb-4 flex items-center gap-2">
          <FileText className="w-5 h-5" />
          Preview do Contrato
        </h4>

        <div className="min-h-[300px]">
          {isLoadingPreview ? (
            <div className="flex flex-col items-center justify-center py-16 text-gray-500">
              <Loader2 className="w-8 h-8 animate-spin mb-3 text-cyan-600" />
              <p className="text-sm">Carregando preview…</p>
            </div>
          ) : previewError ? (
            <div className="flex flex-col items-center justify-center py-16 text-center">
              <AlertCircle className="w-10 h-10 text-red-400 mb-3" />
              <p className="text-sm text-red-600">{previewError}</p>
              <button
                onClick={() => window.location.reload()}
                className="mt-4 text-sm text-cyan-600 hover:underline"
              >
                Tentar novamente
              </button>
            </div>
          ) : htmlContent ? (
            <div
              className="text-sm text-gray-800 leading-relaxed [&_h1]:text-2xl [&_h1]:font-bold [&_h1]:mb-4 [&_h2]:text-xl [&_h2]:font-semibold [&_h2]:mb-3 [&_p]:mb-3 [&_ul]:list-disc [&_ul]:pl-6 [&_ul]:mb-3 [&_ol]:list-decimal [&_ol]:pl-6 [&_ol]:mb-3"
              dangerouslySetInnerHTML={{ __html: sanitizeHtml(htmlContent) }}
            />
          ) : (
            <div className="flex flex-col items-center justify-center py-16 text-gray-400">
              <FileText className="w-10 h-10 mb-3" />
              <p className="text-sm">Nenhum conteúdo disponível para preview.</p>
            </div>
          )}
        </div>
      </Card>

      {/* Action buttons */}
      <div className="flex items-center justify-between pt-2">
        <Button
          variant="secondary"
          onClick={onGoBack}
          disabled={isGenerating}
          icon={<ArrowLeft className="w-4 h-4" />}
        >
          Voltar
        </Button>

        <Button
          variant="primary"
          onClick={handleGenerate}
          disabled={isLoadingPreview || isGenerating || !!previewError}
          icon={
            isGenerating ? (
              <Loader2 className="w-4 h-4 animate-spin" />
            ) : (
              <Download className="w-4 h-4" />
            )
          }
        >
          {isGenerating ? 'Gerando contrato…' : 'Gerar Contrato'}
        </Button>
      </div>
    </div>
  );
};

export default Step5PreviewGenerate;
