import { useEffect, useMemo, useState } from "react";
import { Layers, Plus, X } from "lucide-react";
import { Button, Card } from "@/components/ui";
import { useIssueSharesBatch } from "@/hooks/useCapTable";
import { useAllShareholders } from "@/hooks/useShareholders";
import { useShareClassesByCompany } from "@/hooks/useShareClasses";
import { shareholderTypeLabels, type IssueSharesBatchRequest } from "@/types";
import toast from "react-hot-toast";

interface IssueSharesBatchModalProps {
  isOpen: boolean;
  onClose: () => void;
  companyId: string;
  onSuccess?: () => void;
}

const getToday = () => new Date().toISOString().split("T")[0];

export default function IssueSharesBatchModal({
  isOpen,
  onClose,
  companyId,
  onSuccess,
}: IssueSharesBatchModalProps) {
  const [referenceDate, setReferenceDate] = useState(getToday);
  const [totalQuantity, setTotalQuantity] = useState("");
  const [shareClassId, setShareClassId] = useState("");
  const [pricePerShare, setPricePerShare] = useState("0");
  const [allocations, setAllocations] = useState<Record<string, number>>({});

  const {
    data: rawShareholders,
    isLoading: loadingShareholders,
    isError: shareholdersError,
    refetch: refetchShareholders,
  } = useAllShareholders(isOpen ? companyId : undefined);
  const shareholders = useMemo(
    () =>
      rawShareholders
        ? [...rawShareholders].sort((a, b) =>
            a.name.localeCompare(b.name, "pt-BR", { sensitivity: "base" }),
          )
        : rawShareholders,
    [rawShareholders],
  );
  const {
    data: shareClasses,
    isLoading: loadingClasses,
    isError: shareClassesError,
    refetch: refetchShareClasses,
  } = useShareClassesByCompany(isOpen ? companyId : undefined);
  const issueBatch = useIssueSharesBatch();

  useEffect(() => {
    if (isOpen && shareholders) {
      setAllocations((current) =>
        Object.fromEntries(
          shareholders.map((shareholder) => [
            shareholder.id,
            current[shareholder.id] ?? 0,
          ]),
        ),
      );
    }
  }, [isOpen, shareholders]);

  const distributedQuantity = useMemo(
    () =>
      Object.values(allocations).reduce(
        (total, quantity) => total + quantity,
        0,
      ),
    [allocations],
  );
  const targetQuantity = Number(totalQuantity) || 0;
  const remainingQuantity = targetQuantity - distributedQuantity;

  const resetForm = () => {
    setReferenceDate(getToday());
    setTotalQuantity("");
    setShareClassId("");
    setPricePerShare("0");
    setAllocations({});
  };

  const handleClose = () => {
    if (issueBatch.isPending) return;
    resetForm();
    onClose();
  };

  const handleSubmit = () => {
    const quantity = Number(totalQuantity);
    const price = Number(pricePerShare);
    const allocationList = Object.entries(allocations)
      .filter(([, allocated]) => allocated > 0)
      .map(([shareholderId, allocated]) => ({
        shareholderId,
        quantity: allocated,
      }));

    if (!referenceDate || !shareClassId) {
      toast.error("Informe a data e o tipo de ação");
      return;
    }
    if (!Number.isInteger(quantity) || quantity <= 0) {
      toast.error("O total de ações deve ser um número inteiro maior que zero");
      return;
    }
    if (!Number.isFinite(price) || price < 0) {
      toast.error("Informe um preço por ação válido");
      return;
    }
    if (distributedQuantity !== quantity) {
      toast.error("A distribuição deve somar exatamente o total da emissão");
      return;
    }
    if (
      allocationList.some(
        (allocation) => !Number.isInteger(allocation.quantity),
      )
    ) {
      toast.error("As quantidades distribuídas devem ser números inteiros");
      return;
    }

    const request: IssueSharesBatchRequest = {
      companyId,
      shareClassId,
      totalQuantity: quantity,
      pricePerShare: price,
      referenceDate,
      allocations: allocationList,
      reason: "Emissão em lote",
    };

    issueBatch.mutate(request, {
      onSuccess: () => {
        resetForm();
        onClose();
        onSuccess?.();
      },
    });
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex min-h-screen items-center justify-center p-4">
        <div
          className="fixed inset-0 bg-black/50 transition-opacity"
          onClick={handleClose}
        />

        <div className="relative w-full max-w-5xl rounded-xl bg-white shadow-xl">
          <div className="flex items-center justify-between border-b p-6">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-green-100">
                <Layers className="h-5 w-5 text-green-600" />
              </div>
              <div>
                <h2 className="text-lg font-semibold text-gray-900">
                  Emissão em Lote
                </h2>
                <p className="text-sm text-gray-500">
                  Defina a emissão e distribua as ações entre os sócios
                </p>
              </div>
            </div>
            <button
              type="button"
              onClick={handleClose}
              disabled={issueBatch.isPending}
              aria-label="Fechar emissão em lote"
              className="text-gray-400 hover:text-gray-600 disabled:opacity-50"
            >
              <X className="h-5 w-5" />
            </button>
          </div>

          <div className="max-h-[calc(100vh-12rem)] space-y-6 overflow-y-auto p-6">
            <div className="grid grid-cols-1 gap-4 md:grid-cols-4">
              <div>
                <label htmlFor="batch-reference-date" className="input-label">
                  Data da emissão *
                </label>
                <input
                  id="batch-reference-date"
                  type="date"
                  value={referenceDate}
                  onChange={(event) => setReferenceDate(event.target.value)}
                  className="input"
                  required
                />
              </div>
              <div>
                <label htmlFor="batch-total-quantity" className="input-label">
                  Total de ações *
                </label>
                <input
                  id="batch-total-quantity"
                  type="number"
                  min="1"
                  step="1"
                  value={totalQuantity}
                  onChange={(event) => setTotalQuantity(event.target.value)}
                  className="input"
                  placeholder="Ex.: 10000"
                  required
                />
              </div>
              <div>
                <label htmlFor="batch-share-class" className="input-label">
                  Tipo / classe *
                </label>
                <select
                  id="batch-share-class"
                  value={shareClassId}
                  onChange={(event) => setShareClassId(event.target.value)}
                  className="input"
                  disabled={loadingClasses || !shareClasses?.length}
                  required
                >
                  <option value="">
                    {loadingClasses
                      ? "Carregando..."
                      : !shareClasses?.length
                        ? "Nenhuma classe cadastrada"
                        : "Selecione uma classe"}
                  </option>
                  {shareClasses?.map((shareClass) => (
                    <option key={shareClass.id} value={shareClass.id}>
                      {shareClass.code} - {shareClass.name}
                    </option>
                  ))}
                </select>
                {shareClassesError && (
                  <button
                    type="button"
                    onClick={() => refetchShareClasses()}
                    className="mt-1 text-left text-xs text-red-600 underline"
                  >
                    Erro ao carregar classes. Tentar novamente
                  </button>
                )}
                {!loadingClasses &&
                  !shareClassesError &&
                  !shareClasses?.length && (
                    <a
                      href="/share-classes"
                      target="_blank"
                      rel="noreferrer"
                      className="mt-1 inline-block text-xs text-primary-700 underline"
                    >
                      Cadastrar classe de ações
                    </a>
                  )}
              </div>
              <div>
                <label htmlFor="batch-price-per-share" className="input-label">
                  Preço por ação (R$)
                </label>
                <input
                  id="batch-price-per-share"
                  type="number"
                  min="0"
                  step="0.01"
                  value={pricePerShare}
                  onChange={(event) => setPricePerShare(event.target.value)}
                  className="input"
                />
              </div>
            </div>

            <Card className="overflow-hidden">
              <div className="flex flex-wrap items-center justify-between gap-2 border-b bg-gray-50 px-4 py-3">
                <h3 className="font-medium text-gray-900">
                  Distribuição por sócio
                </h3>
                <p
                  className={`text-sm font-medium ${
                    remainingQuantity === 0
                      ? "text-green-700"
                      : "text-amber-700"
                  }`}
                  aria-live="polite"
                >
                  Distribuídas: {distributedQuantity.toLocaleString("pt-BR")} /{" "}
                  {targetQuantity.toLocaleString("pt-BR")} ações
                  {targetQuantity > 0 && remainingQuantity !== 0
                    ? ` · ${Math.abs(remainingQuantity).toLocaleString("pt-BR")} ${
                        remainingQuantity > 0 ? "pendentes" : "excedentes"
                      }`
                    : ""}
                </p>
              </div>

              {shareholdersError ? (
                <div className="p-8 text-center">
                  <p className="mb-3 text-sm text-red-600">
                    Erro ao carregar os sócios.
                  </p>
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => refetchShareholders()}
                  >
                    Tentar novamente
                  </Button>
                </div>
              ) : (
                <div className="max-h-80 overflow-auto">
                  <table className="w-full">
                    <thead className="sticky top-0 border-b bg-white">
                      <tr>
                        <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                          Sócio
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium uppercase tracking-wider text-gray-500">
                          Tipo de sócio
                        </th>
                        <th className="w-48 px-4 py-3 text-right text-xs font-medium uppercase tracking-wider text-gray-500">
                          Ações a distribuir
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-200">
                      {loadingShareholders ? (
                        <tr>
                          <td
                            colSpan={3}
                            className="px-4 py-8 text-center text-sm text-gray-500"
                          >
                            Carregando sócios...
                          </td>
                        </tr>
                      ) : shareholders?.length ? (
                        shareholders.map((shareholder) => (
                          <tr key={shareholder.id} className="hover:bg-gray-50">
                            <td className="px-4 py-3 text-sm font-medium text-gray-900">
                              {shareholder.name}
                            </td>
                            <td className="px-4 py-3 text-sm text-gray-500">
                              {shareholderTypeLabels[shareholder.type] ??
                                "Outro"}
                            </td>
                            <td className="px-4 py-2">
                              <input
                                type="number"
                                min="0"
                                max={targetQuantity || undefined}
                                step="1"
                                value={allocations[shareholder.id] || ""}
                                onChange={(event) => {
                                  const value = event.target.value;
                                  setAllocations((current) => ({
                                    ...current,
                                    [shareholder.id]:
                                      value === "" ? 0 : Number(value),
                                  }));
                                }}
                                className="input text-right"
                                aria-label={`Ações para ${shareholder.name}`}
                              />
                            </td>
                          </tr>
                        ))
                      ) : (
                        <tr>
                          <td
                            colSpan={3}
                            className="px-4 py-8 text-center text-sm text-gray-500"
                          >
                            Nenhum sócio cadastrado para esta empresa.
                          </td>
                        </tr>
                      )}
                    </tbody>
                  </table>
                </div>
              )}
            </Card>
          </div>

          <div className="flex justify-end gap-3 border-t px-6 py-4">
            <Button
              type="button"
              variant="secondary"
              onClick={handleClose}
              disabled={issueBatch.isPending}
            >
              Cancelar
            </Button>
            <Button
              type="button"
              variant="success"
              loading={issueBatch.isPending}
              disabled={
                !shareholders?.length ||
                loadingShareholders ||
                shareholdersError ||
                loadingClasses ||
                shareClassesError ||
                !shareClasses?.length
              }
              icon={<Plus className="h-4 w-4" />}
              onClick={handleSubmit}
            >
              Emitir ações
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
