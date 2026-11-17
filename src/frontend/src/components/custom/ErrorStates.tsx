import { CircleAlert, RotateCw, WifiOff } from "lucide-react";
import { cn } from "cn";

import { toApiError } from "@/api/errors";
import { Button } from "@/components/ui/button";

// Errores de sección y de página. Los de campo van bajo el input (FormField) y los de una
// acción, en un banner sobre el formulario con el texto exacto del servidor.

// Sección: tarjeta con ícono, texto breve y "Reintentar". La página sigue funcionando.
export function SectionError({
  error,
  onRetry,
  className,
}: {
  error: unknown;
  onRetry?: () => void;
  className?: string;
}) {
  const e = toApiError(error);
  return (
    <div
      role="alert"
      className={cn(
        "flex flex-col items-start gap-3 rounded-[18px] border border-border p-5 sm:flex-row sm:items-center",
        className,
      )}
    >
      <span className="grid size-10 shrink-0 place-items-center rounded-full bg-destructive-soft text-destructive">
        {e.offline ? (
          <WifiOff className="size-5" strokeWidth={1.75} />
        ) : (
          <CircleAlert className="size-5" strokeWidth={1.75} />
        )}
      </span>
      <div className="min-w-0 flex-1 text-[14px]">
        <p className="font-semibold">
          {e.offline ? "Sin conexión" : "No pudimos cargar esta sección"}
        </p>
        <p className="text-muted-foreground">{e.message}</p>
        {e.traceId && <SupportCode traceId={e.traceId} />}
      </div>
      {onRetry && (
        <Button
          variant="outline"
          size="sm"
          className="rounded-full"
          onClick={onRetry}
        >
          <RotateCw /> Reintentar
        </Button>
      )}
    </div>
  );
}

// Página: error del servidor o sin conexión, a pantalla completa (dentro del layout).
export function PageError({
  error,
  onRetry,
}: {
  error: unknown;
  onRetry?: () => void;
}) {
  const e = toApiError(error);
  const offline =
    e.offline || (typeof navigator !== "undefined" && !navigator.onLine);
  return (
    <main className="grid min-h-[70vh] place-items-center px-gutter py-16">
      <div
        role="alert"
        className="grid max-w-110 justify-items-center gap-4 text-center"
      >
        <span className="grid size-16 place-items-center rounded-full bg-secondary text-primary">
          {offline ? (
            <WifiOff className="size-7" strokeWidth={1.5} />
          ) : (
            <CircleAlert className="size-7" strokeWidth={1.5} />
          )}
        </span>
        <h1 className="text-2xl font-bold tracking-tight">
          {offline ? "Sin conexión" : "Algo salió mal de nuestro lado"}
        </h1>
        <p className="text-[15px] text-muted-foreground">
          {offline
            ? "Revisa tu conexión a internet y vuelve a intentarlo."
            : "No pudimos cargar esta página. Inténtalo de nuevo en unos minutos."}
        </p>
        {onRetry && (
          <Button size="lg" className="rounded-full px-8" onClick={onRetry}>
            <RotateCw /> Reintentar
          </Button>
        )}
        {e.traceId && <SupportCode traceId={e.traceId} className="mt-4" />}
      </div>
    </main>
  );
}

// Pie discreto para reportar el error: el identificador de la traza del servidor.
function SupportCode({
  traceId,
  className,
}: {
  traceId: string;
  className?: string;
}) {
  return (
    <p className={cn("mt-1 text-[12px] text-muted-foreground", className)}>
      Código de soporte:{" "}
      <code className="rounded bg-secondary px-1.5 py-0.5 font-mono text-[11.5px] break-all select-all">
        {traceId}
      </code>
    </p>
  );
}
