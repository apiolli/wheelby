import { useState } from "react";
import { Download, FileText, LoaderCircle } from "lucide-react";

import { errorMessage } from "@/api/errors";
import { downloadReservationDocument } from "@/api/wheelby.api";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { formatBytes, formatDate } from "@/lib/format";
import { showToast } from "@/stores/session.store";
import type { ReservationDocument } from "@/types/types";

const KIND_LABEL = { Delivery: "Entrega", Return: "Devolución" } as const;

// P3: documentos de entrega y devolución adjuntos a la reserva. Los ven las dos partes.
export default function DocumentsCard({
  reservationId,
  documents,
  isPending,
  admin,
}: {
  reservationId: string;
  documents: ReservationDocument[] | undefined;
  isPending: boolean;
  admin: boolean;
}) {
  const [busy, setBusy] = useState<string | null>(null);

  const download = async (doc: ReservationDocument) => {
    setBusy(doc.documentId);
    try {
      await downloadReservationDocument(reservationId, doc, admin);
    } catch (err) {
      showToast(errorMessage(err));
    } finally {
      setBusy(null);
    }
  };

  return (
    <section
      aria-labelledby="docs-title"
      className="grid gap-4 rounded-[20px] border border-border p-6"
    >
      <h2 id="docs-title" className="text-[16px] font-bold">
        Documentos de entrega y devolución
      </h2>
      {isPending ? (
        <div className="h-14 animate-pulse rounded-2xl bg-secondary" />
      ) : !documents || documents.length === 0 ? (
        <p className="text-[14px] text-muted-foreground">
          Aún no hay documentos. El anfitrión o el arrendatario pueden adjuntar
          el acta de entrega y la de devolución.
        </p>
      ) : (
        <ul className="grid gap-2">
          {documents.map((d) => (
            <li
              key={d.documentId}
              className="flex items-center gap-3 rounded-2xl bg-secondary px-3.5 py-3"
            >
              <FileText
                className="size-5 shrink-0 text-primary"
                strokeWidth={1.5}
              />
              <div className="min-w-0 flex-1 text-[13.5px]">
                <p className="truncate font-semibold">
                  {d.fileName ?? "Documento eliminado"}
                </p>
                <p className="flex items-center gap-1.5 text-muted-foreground">
                  <Badge variant="outline" className="py-0.5">
                    {KIND_LABEL[d.kind]}
                  </Badge>
                  {formatDate(d.attachedAt)}
                  {d.sizeBytes !== null && ` · ${formatBytes(d.sizeBytes)}`}
                </p>
              </div>
              {d.fileName && (
                <Button
                  variant="ghost"
                  size="icon-sm"
                  className="rounded-full"
                  aria-label={`Descargar ${d.fileName}`}
                  disabled={busy === d.documentId}
                  onClick={() => download(d)}
                >
                  {busy === d.documentId ? (
                    <LoaderCircle className="animate-spin" />
                  ) : (
                    <Download />
                  )}
                </Button>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
