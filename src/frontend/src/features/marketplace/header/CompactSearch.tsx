import { Fragment } from "react";
import { m } from "motion/react";
import { Search } from "lucide-react";
import { cn } from "cn";

import { formatDate } from "@/lib/format";
import { CONTENT_FADE, HEADER_SPRING } from "@/lib/motion";
import { SEARCH_FIELDS, type SearchField, type SearchQuery } from "./search";

interface CompactSearchProps {
  value: SearchQuery;
  onPick: (field: SearchField) => void;
  // Comparte forma con la barra grande para transformarse una en la otra.
  layoutId?: string;
  className?: string;
}

const display = (value: SearchQuery, field: SearchField) =>
  field === "currency" ? value.currency : formatDate(value[field]);

// Píldora compacta que ocupa el centro del header al hacer scroll (escritorio).
export function CompactSearch({
  value,
  onPick,
  layoutId,
  className,
}: CompactSearchProps) {
  return (
    <m.div
      role="group"
      aria-label="Búsqueda"
      layoutId={layoutId}
      transition={HEADER_SPRING}
      style={{ borderRadius: 24 }}
      className={cn(
        "h-12 border border-border bg-background shadow-float transition-shadow hover:shadow-float-lg",
        className,
      )}
    >
      <m.div layout {...CONTENT_FADE} className="flex h-full items-center">
        {SEARCH_FIELDS.map((f, i) => (
          <Fragment key={f.id}>
            {i > 0 && (
              <span
                aria-hidden="true"
                className="h-6 w-px shrink-0 bg-border"
              />
            )}
            <button
              type="button"
              onClick={() => onPick(f.id)}
              aria-label={`${f.label}: ${display(value, f.id)}`}
              className="h-full max-w-40 truncate px-3.5 text-sm font-semibold text-foreground first:pl-5 lg:max-w-45 lg:px-4 lg:first:pl-6"
            >
              {display(value, f.id)}
            </button>
          </Fragment>
        ))}
        <button
          type="button"
          aria-label="Buscar"
          onClick={() => onPick("from")}
          className="mr-2 grid size-8 shrink-0 place-items-center rounded-full bg-primary text-primary-foreground transition-colors hover:bg-primary-hover"
        >
          <Search className="size-3.5" strokeWidth={3} />
        </button>
      </m.div>
    </m.div>
  );
}
