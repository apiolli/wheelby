import { useState, type ReactElement } from "react";

import RangeCalendar, {
  type DayRange,
} from "@/components/custom/RangeCalendar";
import { Button } from "@/components/ui/button";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { useMediaQuery } from "@/hooks/useMediaQuery";
import { formatRangeWords } from "@/lib/format";
import { addDays, today } from "@/lib/schemas";

interface DatesPopoverProps {
  trigger: ReactElement;
  value: { from: string; to: string } | null;
  onApply: (range: { from: string; to: string }) => void;
  open?: boolean;
  onOpenChange?: (open: boolean) => void;
  // Días ocupados del vehículo (P1); en la búsqueda general no hay.
  blocked?: ReadonlySet<string>;
  // Muestra la leyenda de días ocupados.
  showLegend?: boolean;
}

// Popover con el calendario de rango. Las fechas se aplican al pulsar "Aplicar", para que la
// búsqueda no se lance a medio elegir.
export default function DatesPopover({
  trigger,
  value,
  onApply,
  open,
  onOpenChange,
  blocked,
  showLegend,
}: DatesPopoverProps) {
  const wide = useMediaQuery("(min-width: 640px)");
  const [draft, setDraft] = useState<DayRange>({
    from: value?.from ?? null,
    to: value?.to ?? null,
  });
  const complete = draft.from !== null && draft.to !== null;

  return (
    <Popover
      open={open}
      onOpenChange={(next) => {
        if (next)
          setDraft({ from: value?.from ?? null, to: value?.to ?? null });
        onOpenChange?.(next);
      }}
    >
      <PopoverTrigger render={trigger} />
      <PopoverContent className="w-[min(calc(100vw-2rem),40rem)] p-5">
        <RangeCalendar
          value={draft}
          onChange={setDraft}
          min={addDays(today(), 1)}
          months={wide ? 2 : 1}
          blocked={blocked}
          autoFocus
        />
        {showLegend && (
          <p className="mt-3 flex items-center gap-2 text-[12.5px] text-muted-foreground">
            <span
              aria-hidden="true"
              className="font-semibold text-muted-foreground/70 line-through"
            >
              14
            </span>
            Fechas ocupadas: no se pueden incluir en tu rango.
          </p>
        )}
        <div className="mt-4 flex items-center justify-between gap-3 border-t border-border-soft pt-4">
          <p className="text-[13.5px] text-muted-foreground" aria-live="polite">
            {complete
              ? formatRangeWords(draft.from!, draft.to!)
              : draft.from
                ? "Elige la devolución"
                : "Elige la recogida"}
          </p>
          <div className="flex gap-2">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="rounded-full underline underline-offset-4"
              onClick={() => setDraft({ from: null, to: null })}
            >
              Borrar
            </Button>
            <Button
              type="button"
              size="sm"
              className="rounded-full px-4"
              disabled={!complete}
              onClick={() => {
                onApply({ from: draft.from!, to: draft.to! });
                onOpenChange?.(false);
              }}
            >
              Aplicar
            </Button>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  );
}
