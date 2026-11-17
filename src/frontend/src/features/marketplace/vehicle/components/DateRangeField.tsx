import { forwardRef, type ComponentProps } from "react";
import { cn } from "cn";

import { formatDayShort } from "@/lib/format";

interface DateRangeFieldProps extends ComponentProps<"button"> {
  from: string | null;
  to: string | null;
  invalid?: boolean;
}

// Campo de dos mitades (Recogida | Devolución) que abre el calendario. Es un solo botón: el
// calendario elige las dos fechas.
const DateRangeField = forwardRef<HTMLButtonElement, DateRangeFieldProps>(
  function DateRangeField({ from, to, invalid, className, ...props }, ref) {
    return (
      <button
        ref={ref}
        type="button"
        aria-invalid={invalid || undefined}
        aria-label={
          from && to
            ? `Fechas: recogida ${formatDayShort(from)}, devolución ${formatDayShort(to)}. Cambiar`
            : "Elegir fechas"
        }
        {...props}
        className={cn(
          "grid w-full grid-cols-2 rounded-2xl border text-left outline-none focus-visible:ring-2 focus-visible:ring-ring",
          invalid
            ? "border-destructive ring-1 ring-destructive"
            : "border-input hover:border-foreground",
          className,
        )}
      >
        <Half label="Recogida" value={from} />
        <Half
          label="Devolución"
          value={to}
          className="border-l border-inherit"
        />
      </button>
    );
  },
);

function Half({
  label,
  value,
  className,
}: {
  label: string;
  value: string | null;
  className?: string;
}) {
  return (
    <span className={cn("px-4 py-2.5", className)}>
      <span className="block text-[10.5px] font-bold tracking-wide uppercase">
        {label}
      </span>
      <span
        className={cn("block text-[14px]", !value && "text-muted-foreground")}
      >
        {value ? formatDayShort(value) : "Agregar fecha"}
      </span>
    </span>
  );
}

export default DateRangeField;
