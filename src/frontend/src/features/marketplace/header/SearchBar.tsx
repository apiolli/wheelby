import { useEffect, useRef, useState, type FormEvent } from "react";
import { m } from "motion/react";
import { Search } from "lucide-react";
import { cn } from "cn";

import { CONTENT_FADE, HEADER_SPRING, SEGMENT_SPRING } from "@/lib/motion";
import { addDays, CURRENCIES, today, type Currency } from "@/lib/schemas";
import {
  CURRENCY_LABEL,
  SEARCH_FIELDS,
  type SearchField,
  type SearchQuery,
} from "./search";

interface SearchBarProps {
  value: SearchQuery;
  onChange: (value: SearchQuery) => void;
  onSubmit: (value: SearchQuery) => void;
  // Segmento que recibe el foco al montarse (al desplegar desde la píldora compacta).
  autoFocusField?: SearchField | null;
  // Comparte forma con la píldora compacta para transformarse una en la otra.
  layoutId?: string;
  // Disposición: fila en escritorio, apilada en móvil.
  orientation?: "row" | "stack";
  // Prefijo de ids, para poder tener más de una instancia en la página.
  idPrefix?: string;
  className?: string;
}

type FieldElement = HTMLInputElement | HTMLSelectElement;

// Píldora de búsqueda en 3 segmentos: recogida, devolución y moneda. El segmento activo se
// "eleva" en blanco mientras el resto de la barra pasa a gris.
export default function SearchBar({
  value,
  onChange,
  onSubmit,
  autoFocusField = null,
  layoutId,
  orientation = "row",
  idPrefix = "q",
  className,
}: SearchBarProps) {
  const [active, setActive] = useState<SearchField | null>(null);
  const [hovered, setHovered] = useState<SearchField | null>(null);
  const fields = useRef<Partial<Record<SearchField, FieldElement | null>>>({});
  const row = orientation === "row";
  const tomorrow = addDays(today(), 1);

  useEffect(() => {
    if (autoFocusField)
      fields.current[autoFocusField]?.focus({ preventScroll: true });
  }, [autoFocusField]);

  const submit = (e: FormEvent) => {
    e.preventDefault();
    onSubmit(value);
  };

  // Al mover la recogida más allá de la devolución, la devolución se corre un día después.
  const setFrom = (from: string) =>
    onChange({
      ...value,
      from,
      to: from && value.to <= from ? addDays(from, 1) : value.to,
    });

  // El separador entre dos segmentos se oculta si alguno de ellos está resaltado.
  const highlighted = (f: SearchField) => f === active || f === hovered;

  const fieldClass =
    "relative mt-0.5 w-full min-w-0 cursor-pointer truncate bg-transparent p-0 text-sm text-foreground outline-none [&::-webkit-calendar-picker-indicator]:opacity-60";

  return (
    <m.form
      role="search"
      aria-label="Buscar vehículos disponibles"
      onSubmit={submit}
      layoutId={layoutId}
      transition={HEADER_SPRING}
      // El radio va en style para que Motion lo corrija durante la transformación.
      style={{ borderRadius: row ? 33 : 24 }}
      className={cn(
        "relative w-full border border-border bg-background shadow-float transition-colors",
        row ? "h-16.5 max-w-215" : "p-1.5",
        active && "border-transparent bg-secondary",
        className,
      )}
    >
      <m.div
        layout
        {...CONTENT_FADE}
        className={cn(
          "flex size-full",
          row ? "flex-row items-center" : "flex-col",
        )}
      >
        {SEARCH_FIELDS.map((f, i) => {
          const last = i === SEARCH_FIELDS.length - 1;
          const hideSeparator =
            i === 0 ||
            highlighted(f.id) ||
            highlighted(SEARCH_FIELDS[i - 1].id);
          const common = {
            id: `${idPrefix}-${f.id}`,
            ref: (el: FieldElement | null) => {
              fields.current[f.id] = el;
            },
            onFocus: () => setActive(f.id),
            onBlur: (e: React.FocusEvent<FieldElement>) => {
              // Si el foco pasa a otro segmento, la píldora se desliza en vez de desaparecer.
              const next = e.relatedTarget;
              const toSibling =
                (next instanceof HTMLInputElement ||
                  next instanceof HTMLSelectElement) &&
                e.currentTarget.form?.contains(next);
              if (!toSibling) setActive((a) => (a === f.id ? null : a));
            },
            className: fieldClass,
          };
          return (
            <div
              key={f.id}
              onMouseEnter={() => setHovered(f.id)}
              onMouseLeave={() => setHovered(null)}
              className={cn(
                "relative flex min-w-0 flex-col justify-center transition-colors",
                row
                  ? "h-full flex-1 rounded-full px-7"
                  : "h-14.5 rounded-[18px] px-5",
                active !== f.id && "hover:bg-border-soft",
                last && (row ? "pr-19" : "pr-18"),
              )}
            >
              {/* Una sola píldora blanca que se desliza al segmento enfocado */}
              {active === f.id && (
                <m.span
                  aria-hidden="true"
                  layoutId={`${idPrefix}-active-segment`}
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  transition={SEGMENT_SPRING}
                  style={{ borderRadius: row ? 32 : 18 }}
                  className="absolute inset-0 bg-background shadow-float-lg"
                />
              )}
              <span
                aria-hidden="true"
                className={cn(
                  "absolute bg-border transition-opacity",
                  row
                    ? "top-1/2 left-0 h-8 w-px -translate-y-1/2"
                    : "top-0 right-5 left-5 h-px",
                  hideSeparator && "opacity-0",
                )}
              />
              <label
                htmlFor={`${idPrefix}-${f.id}`}
                className="relative cursor-pointer text-xs font-bold"
              >
                {f.label}
              </label>
              {f.id === "currency" ? (
                <select
                  {...common}
                  value={value.currency}
                  onChange={(e) =>
                    onChange({ ...value, currency: e.target.value as Currency })
                  }
                >
                  {CURRENCIES.map((c) => (
                    <option key={c} value={c}>
                      {CURRENCY_LABEL[c]}
                    </option>
                  ))}
                </select>
              ) : (
                <input
                  {...common}
                  type="date"
                  required
                  min={
                    f.id === "from"
                      ? tomorrow
                      : addDays(value.from || tomorrow, 1)
                  }
                  value={value[f.id]}
                  onChange={(e) =>
                    f.id === "from"
                      ? setFrom(e.target.value)
                      : onChange({ ...value, to: e.target.value })
                  }
                />
              )}
              {last && (
                <button
                  type="submit"
                  aria-label="Buscar"
                  className="absolute top-1/2 right-2.25 grid size-12 -translate-y-1/2 place-items-center rounded-full bg-primary text-primary-foreground transition-colors hover:bg-primary-hover focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:outline-none"
                >
                  <Search className="size-4.25" strokeWidth={3} />
                </button>
              )}
            </div>
          );
        })}
      </m.div>
    </m.form>
  );
}
