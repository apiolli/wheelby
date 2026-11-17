import AnimatedAmount from "@/components/custom/AnimatedAmount";
import { formatPrice } from "@/lib/format";
import { daysLabel } from "../../catalog/vehicle";

// Desglose de costo: precio × días = total estimado (en DOP, lo que se cobra). Los importes
// cuentan hasta el nuevo valor al cambiar las fechas. No es una cifra contractual.
export default function PriceBreakdown({
  pricePerDay,
  days,
  approxTotal,
}: {
  pricePerDay: number;
  days: number;
  // "≈ US$135" si la búsqueda trajo una tasa.
  approxTotal?: string | null;
}) {
  const total = pricePerDay * days;
  return (
    <dl className="grid gap-2.5 text-[14.5px]">
      <div className="flex justify-between gap-4">
        <dt className="text-muted-foreground underline decoration-border underline-offset-4">
          {formatPrice(pricePerDay)} × {daysLabel(days)}
        </dt>
        <dd className="tabular-nums">
          <AnimatedAmount value={total} />
        </dd>
      </div>
      <div className="flex items-baseline justify-between gap-4 border-t border-border-soft pt-3 text-base font-bold">
        <dt>Total estimado</dt>
        <dd className="text-right tabular-nums">
          <AnimatedAmount value={total} />
          {approxTotal && (
            <span className="block text-[12.5px] font-normal text-muted-foreground">
              {approxTotal}
            </span>
          )}
        </dd>
      </div>
    </dl>
  );
}
