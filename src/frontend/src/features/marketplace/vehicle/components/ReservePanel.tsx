import { useState } from "react";
import { CalendarX2 } from "lucide-react";

import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { hasBlockedBetween } from "@/lib/day-range";
import { features } from "@/lib/features";
import { formatPrice } from "@/lib/format";
import { daysBetween } from "@/lib/schemas";
import type { Vehicle } from "@/types/types";
import { approxPrice, type RateHint } from "../../catalog/vehicle";
import DatesPopover from "../../search/components/DatesPopover";
import DateRangeField from "./DateRangeField";
import PriceBreakdown from "./PriceBreakdown";

export interface Range {
  from: string;
  to: string;
}

interface ReservePanelProps {
  vehicle: Vehicle;
  range: Range | null;
  onRange: (range: Range) => void;
  rate: RateHint | null;
  blocked: ReadonlySet<string>;
  onRequest: (range: Range) => void;
}

// Panel de reserva de la ficha: precio, fechas, desglose y "Solicitar reserva". El mismo
// contenido va en la columna sticky (escritorio) y en el sheet (móvil).
export default function ReservePanel({
  vehicle,
  range,
  onRange,
  rate,
  blocked,
  onRequest,
}: ReservePanelProps) {
  const [open, setOpen] = useState(false);
  const days = range ? daysBetween(range.from, range.to) : 0;
  const clash = range
    ? hasBlockedBetween(blocked, range.from, range.to)
    : false;
  const approx = approxPrice(vehicle.pricePerDay, rate);

  return (
    <div className="grid gap-5">
      <p>
        <span className="text-[22px] font-bold tracking-tight">
          {formatPrice(vehicle.pricePerDay)}
        </span>{" "}
        <span className="text-muted-foreground">por día</span>
        {approx && (
          <span className="block text-[13px] text-muted-foreground">
            {approx} · tasa de hoy
          </span>
        )}
      </p>

      <DatesPopover
        value={range}
        onApply={onRange}
        open={open}
        onOpenChange={setOpen}
        blocked={blocked}
        showLegend={features.availability}
        trigger={
          <DateRangeField
            from={range?.from ?? null}
            to={range?.to ?? null}
            invalid={clash}
          />
        }
      />

      {clash && (
        <Alert variant="destructive" role="status">
          <CalendarX2 />
          <AlertDescription>
            Esas fechas incluyen días ocupados. Elige otro rango.
          </AlertDescription>
        </Alert>
      )}

      {range && !clash ? (
        <>
          <PriceBreakdown
            pricePerDay={vehicle.pricePerDay}
            days={days}
            approxTotal={approxPrice(vehicle.pricePerDay * days, rate)}
          />
          <Button
            size="lg"
            className="h-13 rounded-full text-[15px]"
            onClick={() => onRequest(range)}
          >
            Solicitar reserva
          </Button>
        </>
      ) : (
        <Button
          size="lg"
          variant={clash ? "outline" : "default"}
          className="h-13 rounded-full text-[15px]"
          onClick={() => setOpen(true)}
        >
          {clash ? "Cambiar fechas" : "Elige tus fechas"}
        </Button>
      )}

      <p className="text-center text-[12.5px] leading-relaxed text-muted-foreground">
        No se cobra nada en línea. El anfitrión debe confirmar tu solicitud.
      </p>
    </div>
  );
}
