import { ChevronLeft, ChevronRight } from "lucide-react";
import { Link, useLocation, useSearchParams } from "react-router";
import { cn } from "cn";

import { buttonVariants } from "@/components/ui/button";

interface PaginationProps {
  page: number;
  totalPages: number;
  // Parámetro de la URL que guarda la página.
  param?: string;
  className?: string;
}

// Paginación por URL (?pagina=2): se puede compartir, recargar y volver atrás.
export default function Pagination({
  page,
  totalPages,
  param = "pagina",
  className,
}: PaginationProps) {
  const [params] = useSearchParams();
  const { pathname } = useLocation();
  if (totalPages <= 1) return null;

  const hrefFor = (p: number) => {
    const next = new URLSearchParams(params);
    if (p <= 1) next.delete(param);
    else next.set(param, String(p));
    const search = next.toString();
    return search ? `${pathname}?${search}` : pathname;
  };

  const linkClass = (disabled: boolean) =>
    cn(
      buttonVariants({ variant: "outline", size: "sm" }),
      "gap-1",
      disabled && "pointer-events-none opacity-40",
    );

  return (
    <nav
      aria-label="Paginación"
      className={cn("flex items-center justify-center gap-3", className)}
    >
      <Link
        to={hrefFor(page - 1)}
        aria-disabled={page <= 1}
        tabIndex={page <= 1 ? -1 : undefined}
        className={linkClass(page <= 1)}
      >
        <ChevronLeft className="size-4" />
        Anterior
      </Link>
      <span className="text-[13.5px] text-muted-foreground tabular-nums">
        Página {page} de {totalPages}
      </span>
      <Link
        to={hrefFor(page + 1)}
        aria-disabled={page >= totalPages}
        tabIndex={page >= totalPages ? -1 : undefined}
        className={linkClass(page >= totalPages)}
      >
        Siguiente
        <ChevronRight className="size-4" />
      </Link>
    </nav>
  );
}
