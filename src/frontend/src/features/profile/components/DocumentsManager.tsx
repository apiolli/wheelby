import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AnimatePresence, m } from "motion/react";
import {
  CircleAlert,
  Download,
  FileText,
  LoaderCircle,
  Trash2,
  Upload,
} from "lucide-react";
import type { ReactNode } from "react";
import { useSearchParams } from "react-router";

import { errorMessage } from "@/api/errors";
import {
  deleteDocument,
  downloadDocument,
  listDocuments,
  uploadDocument,
} from "@/api/wheelby.api";
import ConfirmAction from "@/components/custom/ConfirmAction";
import EmptyState from "@/components/custom/EmptyState";
import { DocumentIllustration } from "@/components/custom/Illustrations";
import Pagination from "@/components/custom/Pagination";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { formatBytes, formatDate, shortId } from "@/lib/format";
import { queryKeys } from "@/lib/query-keys";
import {
  DOCUMENT_ACCEPT,
  documentSchema,
  pageParam,
  typedDocumentSchema,
} from "@/lib/schemas";
import { showToast, useSession } from "@/stores/session.store";
import type { DocumentSummary } from "@/types/types";

const PAGE_SIZE = 20;

interface DocumentsManagerProps {
  // Encabezado; recibe el botón de subir para colocarlo a su lado.
  heading: (uploadButton: ReactNode | null) => ReactNode;
  // Solo PDF e imágenes (licencia, matrícula).
  restrictTypes?: boolean;
  // Texto bajo la lista (privacidad, formatos).
  footnote?: ReactNode;
}

// Manejador de documentos: subir, listar, descargar y eliminar. El Administrador ve los de todos.
export default function DocumentsManager({
  heading,
  restrictTypes,
  footnote,
}: DocumentsManagerProps) {
  const [params] = useSearchParams();
  const page = pageParam(params);
  const user = useSession((s) => s.user);
  const queryClient = useQueryClient();
  const fileRef = useRef<HTMLInputElement>(null);
  const [downloading, setDownloading] = useState<string | null>(null);

  const documents = useQuery({
    queryKey: queryKeys.documents(page),
    queryFn: () => listDocuments(page, PAGE_SIZE),
  });

  const refresh = () =>
    queryClient.invalidateQueries({ queryKey: queryKeys.documents() });

  const upload = useMutation({
    mutationFn: (file: File) => uploadDocument(file),
    onSuccess: (_, file) => {
      showToast(`Subimos ${file.name}`);
      return refresh();
    },
    onError: (err) => showToast(errorMessage(err)),
  });

  const remove = useMutation({
    mutationFn: (doc: DocumentSummary) => deleteDocument(doc.id),
    onSuccess: (_, doc) => {
      showToast(`Eliminamos ${doc.fileName}`);
      return refresh();
    },
  });

  const pick = (file: File | undefined) => {
    if (fileRef.current) fileRef.current.value = "";
    const check = (
      restrictTypes ? typedDocumentSchema : documentSchema
    ).safeParse(file);
    if (!check.success) {
      showToast(check.error.issues[0].message);
      return;
    }
    upload.mutate(check.data);
  };

  const download = async (doc: DocumentSummary) => {
    setDownloading(doc.id);
    try {
      await downloadDocument(doc);
    } catch (err) {
      showToast(errorMessage(err));
    } finally {
      setDownloading(null);
    }
  };

  const items = documents.data?.items ?? [];
  const uploadButton = (
    <Button
      onClick={() => fileRef.current?.click()}
      disabled={upload.isPending}
      aria-busy={upload.isPending}
    >
      {upload.isPending ? (
        <LoaderCircle className="animate-spin" />
      ) : (
        <Upload />
      )}
      {upload.isPending ? "Subiendo…" : "Subir archivo"}
    </Button>
  );

  return (
    <>
      {heading(items.length > 0 ? uploadButton : null)}
      <input
        ref={fileRef}
        type="file"
        className="sr-only"
        tabIndex={-1}
        aria-hidden="true"
        accept={restrictTypes ? DOCUMENT_ACCEPT : undefined}
        onChange={(e) => pick(e.target.files?.[0])}
      />

      {documents.isPending ? (
        <div
          className="h-48 animate-pulse rounded-2xl bg-secondary"
          aria-busy="true"
        />
      ) : documents.isError ? (
        <Alert variant="destructive">
          <CircleAlert />
          <AlertDescription>{errorMessage(documents.error)}</AlertDescription>
        </Alert>
      ) : items.length === 0 ? (
        <EmptyState
          illustration={<DocumentIllustration className="w-37.5" />}
          title="Aún no tienes documentos"
          text="Sube tu licencia, la matrícula de tu vehículo o cualquier archivo que quieras tener a mano."
          action={uploadButton}
        />
      ) : (
        <>
          <ul className="divide-y divide-border-soft rounded-2xl border border-border">
            <AnimatePresence initial={false}>
              {items.map((doc) => (
                <m.li
                  key={doc.id}
                  layout
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  exit={{ opacity: 0 }}
                  className="flex items-center gap-3 p-4"
                >
                  <span className="grid size-10 shrink-0 place-items-center rounded-xl bg-primary-soft text-primary">
                    <FileText className="size-5" strokeWidth={1.7} />
                  </span>
                  <div className="min-w-0 flex-1 text-sm">
                    <p className="truncate font-semibold">{doc.fileName}</p>
                    <p className="truncate text-[12.5px] text-muted-foreground">
                      {formatBytes(doc.sizeBytes)} ·{" "}
                      {formatDate(doc.uploadedAt)}
                      {doc.ownerId !== user?.id &&
                        ` · Cuenta #${shortId(doc.ownerId)}`}
                    </p>
                  </div>
                  <Button
                    variant="ghost"
                    size="icon-sm"
                    aria-label={`Descargar ${doc.fileName}`}
                    onClick={() => download(doc)}
                    disabled={downloading === doc.id}
                  >
                    {downloading === doc.id ? (
                      <LoaderCircle className="animate-spin" />
                    ) : (
                      <Download />
                    )}
                  </Button>
                  <ConfirmAction
                    trigger={
                      <Button
                        variant="ghost"
                        size="icon-sm"
                        aria-label={`Eliminar ${doc.fileName}`}
                        className="text-destructive hover:bg-destructive-soft"
                      >
                        <Trash2 />
                      </Button>
                    }
                    title="¿Eliminar este documento?"
                    description={`Ya no podrás descargar ${doc.fileName}.`}
                    confirmLabel="Eliminar"
                    destructive
                    onConfirm={() => remove.mutateAsync(doc)}
                  />
                </m.li>
              ))}
            </AnimatePresence>
          </ul>
          <Pagination
            page={documents.data.page}
            totalPages={documents.data.totalPages}
            className="mt-6"
          />
        </>
      )}
      {footnote && (
        <p className="mt-3 text-[12.5px] text-muted-foreground">{footnote}</p>
      )}
    </>
  );
}
