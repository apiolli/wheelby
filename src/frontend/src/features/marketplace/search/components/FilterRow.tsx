import { forwardRef, type ComponentProps } from "react";
import { m, AnimatePresence } from "motion/react";
import { CalendarDays, ChevronDown, X } from "lucide-react";
import { cn } from "cn";

import { formatRangeWords } from "@/lib/format";
import { CURRENCIES, type Currency } from "@/lib/schemas";
import type { SearchState } from "../params";
import DatesPopover from "./DatesPopover";

// Un chip de filtro activo. Para sumar ciudad, tipo o precio basta con agregar su entrada a
// la lista que arma FilterRow; la fila no cambia.
export interface ActiveFilter {
  id: string;
  label: string;
  onRemove: () => void;
}

interface FilterRowProps {
  search: SearchState;
  onDates: (range: { from: string; to: string }) => void;
  onCurrency: (currency: Currency) => void;
  onClearDates: () => void;
  datesOpen: boolean;
  onDatesOpenChange: (open: boolean) => void;
}

export default function FilterRow({
  search,
  onDates,
  onCurrency,
  onClearDates,
  datesOpen,
  onDatesOpenChange,
}: FilterRowProps) {
  const active: ActiveFilter[] = [
    ...(search.hasDates
      ? [
          {
            id: "dates",
            label: formatRangeWords(search.from, search.to),
            onRemove: onClearDates,
          },
        ]
      : []),
    ...(search.currency !== "DOP"
      ? [
          {
            id: "currency",
            label: `Precios en ${search.currency}`,
            onRemove: () => onCurrency("DOP"),
          },
        ]
      : []),
  ];

  return (
    <div className="flex flex-wrap items-center gap-2">
      <DatesPopover
        value={{ from: search.from, to: search.to }}
        onApply={onDates}
        open={datesOpen}
        onOpenChange={onDatesOpenChange}
        trigger={
          <FilterButton active={search.hasDates} aria-label="Elegir fechas">
            <CalendarDays className="size-4" strokeWidth={1.75} />
            Fechas
            <ChevronDown className="size-3.5 opacity-60" strokeWidth={2} />
          </FilterButton>
        }
      />

      <div
        role="radiogroup"
        aria-label="Moneda de los precios"
        className="inline-flex h-10 items-center rounded-full border border-border bg-background p-1"
      >
        {CURRENCIES.map((c) => {
          const checked = search.currency === c;
          return (
            <button
              key={c}
              type="button"
              role="radio"
              aria-checked={checked}
              onClick={() => onCurrency(c)}
              className={cn(
                "relative h-8 rounded-full px-3.5 text-[13px] font-semibold outline-none focus-visible:ring-2 focus-visible:ring-ring",
                checked
                  ? "text-primary-foreground"
                  : "text-muted-foreground hover:text-foreground",
              )}
            >
              {checked && (
                <m.span
                  layoutId="currency-pill"
                  aria-hidden="true"
                  className="absolute inset-0 rounded-full bg-primary"
                  transition={{ type: "spring", bounce: 0.15, duration: 0.35 }}
                />
              )}
              <span className="relative">{c}</span>
            </button>
          );
        })}
      </div>

      {active.length > 0 && (
        <span
          aria-hidden="true"
          className="mx-1 hidden h-6 w-px bg-border sm:block"
        />
      )}
      <ul aria-label="Filtros activos" className="flex flex-wrap gap-2">
        <AnimatePresence initial={false}>
          {active.map((f) => (
            <m.li
              key={f.id}
              layout
              initial={{ opacity: 0, scale: 0.9 }}
              animate={{ opacity: 1, scale: 1 }}
              exit={{ opacity: 0, scale: 0.9 }}
              transition={{ duration: 0.18 }}
            >
              <span className="inline-flex h-8 items-center gap-1 rounded-full bg-primary-soft py-0 pr-1 pl-3 text-[13px] font-semibold text-primary">
                {f.label}
                <button
                  type="button"
                  onClick={f.onRemove}
                  aria-label={`Quitar filtro: ${f.label}`}
                  className="grid size-6 place-items-center rounded-full outline-none hover:bg-primary/10 focus-visible:ring-2 focus-visible:ring-ring"
                >
                  <X className="size-3.5" strokeWidth={2.25} />
                </button>
              </span>
            </m.li>
          ))}
        </AnimatePresence>
      </ul>
    </div>
  );
}

const FilterButton = forwardRef<
  HTMLButtonElement,
  ComponentProps<"button"> & { active?: boolean }
>(function FilterButton({ active, className, ...props }, ref) {
  return (
    <button
      ref={ref}
      type="button"
      {...props}
      className={cn(
        "inline-flex h-10 items-center gap-2 rounded-full border px-4 text-[13.5px] font-semibold outline-none hover:border-foreground focus-visible:ring-2 focus-visible:ring-ring",
        active
          ? "border-foreground bg-secondary"
          : "border-border bg-background",
        className,
      )}
    />
  );
});
