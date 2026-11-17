import { useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { AnimatePresence, m } from "motion/react";
import {
  Download,
  Eye,
  FileText,
  Image as ImageIcon,
  LoaderCircle,
  RotateCw,
  Upload,
  X,
} from "lucide-react";
import { cn } from "cn";

import { errorMessage } from "@/api/errors";
import {
  attachReservationDocument,
  downloadReservationDocument,
  openBlob,
  reservationDocumentUrl,
  uploadDocument,
} from "@/api/wheelby.api";
import { Button } from "@/components/ui/button";
import { formatBytes, formatDate } from "@/lib/format";
import { queryKeys } from "@/lib/query-keys";
import {
  DOCUMENT_ACCEPT,
  DOCUMENT_FORMATS_LABEL,
  typedDocumentSchema,
} from "@/lib/schemas";
import { showToast } from "@/stores/session.store";
import {
  ReservationStatus,
  type ReservationDocument,
  type ReservationDocumentKind,
} from "@/types/types";
import type { Side } from "../rules";

const GROUPS: { kind: ReservationDocumentKind; title: string; hint: string }[] =
  [
    {
      kind: "Delivery",
      title: "Entrega",
      hint: "Fotos del vehículo y acta al entregarlo.",
    },
    {
      kind: "Return",
      title: "Devolución",
      hint: "Fotos del vehículo y acta al recibirlo de vuelta.",
    },
  ];

// Se puede adjuntar desde que la reserva se confirma (igual que el backend).
const ATTACHABLE: ReservationStatus[] = [
  ReservationStatus.Confirmed,
  ReservationStatus.InProgress,
  ReservationStatus.Completed,
];

// Un archivo que se está subiendo, o que falló. documentId queda guardado si la subida terminó y
// lo que falló fue adjuntarlo: el reintento no vuelve a subirlo.
interface PendingUpload {
  key: string;
  kind: ReservationDocumentKind;
  file: File;
  progress: number;
  error?: string;
  documentId?: string;
}

interface DocumentsCardProps {
  reservationId: string;
  status: ReservationStatus;
  side: Side;
  documents: ReservationDocument[] | undefined;
  isPending: boolean;
  admin: boolean;
}

// P3: documentos de entrega y devolución. El anfitrión sube fotos o PDF; el arrendatario (y el
// Administrador) solo los ven. No es una inspección: sin kilometraje, combustible ni daños.
export default function DocumentsCard({
  reservationId,
  status,
  side,
  documents,
  isPending,
  admin,
}: DocumentsCardProps) {
  const queryClient = useQueryClient();
  const [uploads, setUploads] = useState<PendingUpload[]>([]);
  const canUpload = side === "host" && ATTACHABLE.includes(status);

  const patch = (key: string, changes: Partial<PendingUpload>) =>
    setUploads((list) =>
      list.map((u) => (u.key === key ? { ...u, ...changes } : u)),
    );

  const send = async (item: PendingUpload) => {
    patch(item.key, { error: undefined, progress: item.documentId ? 1 : 0 });
    try {
      const documentId =
        item.documentId ??
        (
          await uploadDocument(item.file, (p) =>
            patch(item.key, { progress: p }),
          )
        ).id;
      patch(item.key, { documentId, progress: 1 });
      await attachReservationDocument(reservationId, documentId, item.kind);
      await queryClient.invalidateQueries({
        queryKey: queryKeys.reservationPart(reservationId, "documents"),
      });
      setUploads((list) => list.filter((u) => u.key !== item.key));
    } catch (err) {
      patch(item.key, { error: errorMessage(err) });
    }
  };

  const add = (kind: ReservationDocumentKind, files: FileList | null) => {
    for (const file of Array.from(files ?? [])) {
      const item: PendingUpload = {
        key: crypto.randomUUID(),
        kind,
        file,
        progress: 0,
      };
      const check = typedDocumentSchema.safeParse(file);
      if (!check.success) {
        // Error de formato o tamaño: se muestra en la fila, sin reintento.
        setUploads((list) => [
          ...list,
          { ...item, error: check.error.issues[0].message, progress: -1 },
        ]);
        continue;
      }
      setUploads((list) => [...list, item]);
      void send({ ...item, documentId: undefined });
    }
  };

  return (
    <section
      aria-labelledby="docs-title"
      className="grid gap-5 rounded-[20px] border border-border p-6"
    >
      <div>
        <h2 id="docs-title" className="text-[16px] font-bold">
          Documentos de entrega y devolución
        </h2>
        <p className="text-[13px] text-muted-foreground">
          {side === "host"
            ? canUpload
              ? `Fotos o PDF · ${DOCUMENT_FORMATS_LABEL}`
              : "Podrás adjuntarlos cuando la reserva esté confirmada."
            : "El anfitrión adjunta las fotos y actas de entrega y devolución."}
        </p>
      </div>

      {isPending ? (
        <div className="h-24 animate-pulse rounded-2xl bg-secondary" />
      ) : (
        GROUPS.map((g) => (
          <DocumentGroup
            key={g.kind}
            title={g.title}
            hint={g.hint}
            documents={(documents ?? []).filter((d) => d.kind === g.kind)}
            uploads={uploads.filter((u) => u.kind === g.kind)}
            canUpload={canUpload}
            onFiles={(files) => add(g.kind, files)}
            onRetry={send}
            onDismiss={(key) =>
              setUploads((list) => list.filter((u) => u.key !== key))
            }
            reservationId={reservationId}
            admin={admin}
          />
        ))
      )}
    </section>
  );
}

function DocumentGroup({
  title,
  hint,
  documents,
  uploads,
  canUpload,
  onFiles,
  onRetry,
  onDismiss,
  reservationId,
  admin,
}: {
  title: string;
  hint: string;
  documents: ReservationDocument[];
  uploads: PendingUpload[];
  canUpload: boolean;
  onFiles: (files: FileList | null) => void;
  onRetry: (item: PendingUpload) => void;
  onDismiss: (key: string) => void;
  reservationId: string;
  admin: boolean;
}) {
  const input = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const empty = documents.length === 0 && uploads.length === 0;

  const run = async (doc: ReservationDocument, action: "view" | "download") => {
    setBusy(`${action}-${doc.documentId}`);
    try {
      if (action === "view")
        await openBlob(
          reservationDocumentUrl(reservationId, doc.documentId, admin),
        );
      else await downloadReservationDocument(reservationId, doc, admin);
    } catch (err) {
      showToast(errorMessage(err));
    } finally {
      setBusy(null);
    }
  };

  return (
    <div className="grid gap-2.5 border-t border-border-soft pt-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h3 className="text-[14.5px] font-semibold">{title}</h3>
          {canUpload && (
            <p className="text-[12.5px] text-muted-foreground">{hint}</p>
          )}
        </div>
        {canUpload && (
          <>
            <Button
              variant="outline"
              size="sm"
              className="rounded-full"
              onClick={() => input.current?.click()}
            >
              <Upload /> Subir
            </Button>
            <input
              ref={input}
              type="file"
              multiple
              accept={DOCUMENT_ACCEPT}
              className="sr-only"
              tabIndex={-1}
              aria-label={`Subir documentos de ${title.toLowerCase()}`}
              onChange={(e) => {
                onFiles(e.target.files);
                e.target.value = "";
              }}
            />
          </>
        )}
      </div>

      {empty ? (
        <p className="rounded-xl bg-secondary px-3.5 py-3 text-[13px] text-muted-foreground">
          Sin documentos.
        </p>
      ) : (
        <ul className="grid gap-2">
          {documents.map((d) => {
            const image = d.contentType?.startsWith("image/");
            const Icon = image ? ImageIcon : FileText;
            return (
              <li
                key={d.documentId}
                className="flex items-center gap-3 rounded-xl bg-secondary px-3 py-2.5"
              >
                <Icon
                  className="size-5 shrink-0 text-primary"
                  strokeWidth={1.5}
                />
                <div className="min-w-0 flex-1 text-[13px]">
                  <p className="truncate font-semibold">
                    {d.fileName ?? "Documento eliminado"}
                  </p>
                  <p className="text-muted-foreground">
                    {formatDate(d.attachedAt)}
                    {d.sizeBytes !== null && ` · ${formatBytes(d.sizeBytes)}`}
                  </p>
                </div>
                {d.fileName && (
                  <>
                    <IconButton
                      label={`Ver ${d.fileName}`}
                      busy={busy === `view-${d.documentId}`}
                      onClick={() => run(d, "view")}
                    >
                      <Eye />
                    </IconButton>
                    <IconButton
                      label={`Descargar ${d.fileName}`}
                      busy={busy === `download-${d.documentId}`}
                      onClick={() => run(d, "download")}
                    >
                      <Download />
                    </IconButton>
                  </>
                )}
              </li>
            );
          })}
          <AnimatePresence initial={false}>
            {uploads.map((u) => (
              <m.li
                key={u.key}
                layout
                initial={{ opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0 }}
                className={cn(
                  "grid gap-2 rounded-xl px-3 py-2.5 text-[13px]",
                  u.error
                    ? "bg-destructive-soft"
                    : "border border-dashed border-border",
                )}
              >
                <div className="flex items-center gap-3">
                  {u.error ? (
                    <FileText
                      className="size-5 shrink-0 text-destructive"
                      strokeWidth={1.5}
                    />
                  ) : (
                    <LoaderCircle className="size-5 shrink-0 animate-spin text-primary" />
                  )}
                  <div className="min-w-0 flex-1">
                    <p className="truncate font-semibold">{u.file.name}</p>
                    <p
                      className={
                        u.error ? "text-destructive" : "text-muted-foreground"
                      }
                      aria-live="polite"
                    >
                      {u.error ??
                        (u.progress < 1
                          ? `Subiendo… ${Math.round(u.progress * 100)}%`
                          : "Adjuntando…")}
                    </p>
                  </div>
                  {u.error && u.progress >= 0 && (
                    <IconButton
                      label={`Reintentar ${u.file.name}`}
                      onClick={() => onRetry(u)}
                    >
                      <RotateCw />
                    </IconButton>
                  )}
                  {u.error && (
                    <IconButton
                      label={`Descartar ${u.file.name}`}
                      onClick={() => onDismiss(u.key)}
                    >
                      <X />
                    </IconButton>
                  )}
                </div>
                {!u.error && (
                  <div
                    role="progressbar"
                    aria-label={`Progreso de ${u.file.name}`}
                    aria-valuenow={Math.round(u.progress * 100)}
                    aria-valuemin={0}
                    aria-valuemax={100}
                    className="h-1 overflow-hidden rounded-full bg-border-soft"
                  >
                    <m.div
                      className="h-full origin-left rounded-full bg-primary"
                      initial={{ scaleX: 0 }}
                      animate={{ scaleX: Math.max(0.03, u.progress) }}
                      transition={{ duration: 0.2 }}
                    />
                  </div>
                )}
              </m.li>
            ))}
          </AnimatePresence>
        </ul>
      )}
    </div>
  );
}

function IconButton({
  label,
  busy,
  onClick,
  children,
}: {
  label: string;
  busy?: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <Button
      variant="ghost"
      size="icon-sm"
      className="shrink-0 rounded-full"
      aria-label={label}
      disabled={busy}
      aria-busy={busy}
      onClick={onClick}
    >
      {busy ? <LoaderCircle className="animate-spin" /> : children}
    </Button>
  );
}
