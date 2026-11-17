import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { m } from "motion/react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { cn } from "cn";

import { Button } from "@/components/ui/button";
import { hasBlockedBetween } from "@/lib/day-range";
import { addDays } from "@/lib/schemas";

// Días como "YYYY-MM-DD" en hora local, igual que el resto de la interfaz.
export interface DayRange {
  from: string | null;
  to: string | null;
}

interface RangeCalendarProps {
  value: DayRange;
  onChange: (value: DayRange) => void;
  // Primer día que se puede elegir (el backend exige un inicio futuro).
  min: string;
  // Días con una reserva Confirmed o InProgress (P1). Sin el endpoint, vacío.
  blocked?: ReadonlySet<string>;
  // Meses visibles a la vez (2 en escritorio).
  months?: 1 | 2;
  // Para recibir el foco al abrir el popover.
  autoFocus?: boolean;
  className?: string;
}

const WEEKDAYS = ["L", "M", "X", "J", "V", "S", "D"];
const WEEKDAY_NAMES = [
  "lunes",
  "martes",
  "miércoles",
  "jueves",
  "viernes",
  "sábado",
  "domingo",
];
const MONTH = new Intl.DateTimeFormat("es-DO", {
  month: "long",
  year: "numeric",
});
const DAY_LABEL = new Intl.DateTimeFormat("es-DO", {
  weekday: "long",
  day: "numeric",
  month: "long",
  year: "numeric",
});

const toDate = (day: string) => new Date(`${day}T12:00:00`);
const monthOf = (day: string) => `${day.slice(0, 7)}-01`;
const addMonths = (month: string, n: number) => {
  const d = toDate(month);
  d.setMonth(d.getMonth() + n, 1);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-01`;
};
// Lunes = 0.
const weekday = (day: string) => (toDate(day).getDay() + 6) % 7;

function monthDays(month: string) {
  const days: string[] = [];
  for (let d = month; d.slice(0, 7) === month.slice(0, 7); d = addDays(d, 1))
    days.push(d);
  return days;
}

/**
 * Calendario de rango propio: recogida y devolución con un clic cada una. Los días ocupados se
 * ven tachados y no se pueden incluir en el rango. Teclado: flechas (día y semana), Inicio/Fin
 * (semana), RePág/AvPág (mes), Enter o Espacio para elegir.
 */
export default function RangeCalendar({
  value,
  onChange,
  min,
  blocked = new Set(),
  months = 1,
  autoFocus,
  className,
}: RangeCalendarProps) {
  const [visible, setVisible] = useState(() => monthOf(value.from ?? min));
  const [focused, setFocused] = useState(value.from ?? min);
  const [hovered, setHovered] = useState<string | null>(null);
  const cells = useRef(new Map<string, HTMLButtonElement>());
  const keyboard = useRef(Boolean(autoFocus));

  // Solo el foco del teclado se mueve con el estado; el del ratón lo pone el navegador.
  useEffect(() => {
    if (keyboard.current) cells.current.get(focused)?.focus();
  }, [focused, visible]);

  const { from, to } = value;
  const choosingEnd = from !== null && to === null;

  const isDisabled = (day: string) => {
    if (day < min) return true;
    if (!blocked.has(day)) return false;
    // Un día ocupado solo sirve como devolución, si todo lo anterior está libre.
    return !(
      choosingEnd &&
      day > from &&
      !hasBlockedBetween(blocked, from, day)
    );
  };

  const select = (day: string) => {
    if (isDisabled(day)) return;
    if (!choosingEnd || day <= from || hasBlockedBetween(blocked, from, day))
      onChange({ from: day, to: null });
    else onChange({ from, to: day });
  };

  const moveFocus = (day: string) => {
    if (day < min) day = min;
    keyboard.current = true;
    setFocused(day);
    const lastVisible = addMonths(visible, months - 1);
    if (day < visible) setVisible(monthOf(day));
    else if (monthOf(day) > lastVisible)
      setVisible(addMonths(monthOf(day), 1 - months));
  };

  const onKeyDown = (e: KeyboardEvent<HTMLButtonElement>, day: string) => {
    const moves: Record<string, () => string> = {
      ArrowLeft: () => addDays(day, -1),
      ArrowRight: () => addDays(day, 1),
      ArrowUp: () => addDays(day, -7),
      ArrowDown: () => addDays(day, 7),
      Home: () => addDays(day, -weekday(day)),
      End: () => addDays(day, 6 - weekday(day)),
      PageUp: () => addDays(day, -30),
      PageDown: () => addDays(day, 30),
    };
    const move = moves[e.key];
    if (move) {
      e.preventDefault();
      moveFocus(move());
    }
  };

  const shown = Array.from({ length: months }, (_, i) => addMonths(visible, i));
  const canGoBack = visible > monthOf(min);
  // Fin del rango para pintar: el elegido o, mientras se elige, el día bajo el cursor.
  const previewEnd =
    choosingEnd && hovered && hovered > from && !isDisabled(hovered)
      ? hovered
      : null;

  return (
    <div className={cn("relative select-none", className)}>
      <div className="pointer-events-none absolute inset-x-0 top-0 flex justify-between">
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className="pointer-events-auto rounded-full"
          aria-label="Mes anterior"
          disabled={!canGoBack}
          onClick={() => setVisible((v) => addMonths(v, -1))}
        >
          <ChevronLeft />
        </Button>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          className="pointer-events-auto rounded-full"
          aria-label="Mes siguiente"
          onClick={() => setVisible((v) => addMonths(v, 1))}
        >
          <ChevronRight />
        </Button>
      </div>

      <div className={cn("grid gap-8", months === 2 && "sm:grid-cols-2")}>
        {shown.map((month) => (
          <div
            key={month}
            className={cn(months === 2 && "max-sm:not-first:hidden")}
          >
            <p
              className="mb-3 text-center text-[15px] leading-8 font-bold first-letter:uppercase"
              aria-live="polite"
            >
              {MONTH.format(toDate(month))}
            </p>
            <div
              role="grid"
              aria-label={MONTH.format(toDate(month))}
              className="grid gap-y-1"
            >
              <div role="row" className="grid grid-cols-7">
                {WEEKDAYS.map((w, i) => (
                  <span
                    key={w}
                    role="columnheader"
                    aria-label={WEEKDAY_NAMES[i]}
                    className="pb-1 text-center text-[11.5px] font-semibold text-muted-foreground"
                  >
                    {w}
                  </span>
                ))}
              </div>
              {weeks(month).map((week, wi) => (
                <div key={wi} role="row" className="grid grid-cols-7">
                  {week.map((day, di) =>
                    day === null ? (
                      <span key={di} role="gridcell" />
                    ) : (
                      <DayCell
                        key={day}
                        day={day}
                        from={from}
                        to={to ?? previewEnd}
                        preview={to === null}
                        blocked={blocked.has(day)}
                        disabled={isDisabled(day)}
                        tabbable={day === focused}
                        cellRef={(el) => {
                          if (el) cells.current.set(day, el);
                          else cells.current.delete(day);
                        }}
                        onSelect={() => {
                          keyboard.current = false;
                          setFocused(day);
                          select(day);
                        }}
                        onKeyDown={(e) => onKeyDown(e, day)}
                        onHover={setHovered}
                      />
                    ),
                  )}
                </div>
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

function weeks(month: string) {
  const days: (string | null)[] = [
    ...Array<null>(weekday(month)).fill(null),
    ...monthDays(month),
  ];
  while (days.length % 7) days.push(null);
  return Array.from({ length: days.length / 7 }, (_, i) =>
    days.slice(i * 7, i * 7 + 7),
  );
}

interface DayCellProps {
  day: string;
  from: string | null;
  to: string | null;
  // El fin es solo una vista previa (cursor encima, aún sin elegir).
  preview: boolean;
  blocked: boolean;
  disabled: boolean;
  tabbable: boolean;
  cellRef: (el: HTMLButtonElement | null) => void;
  onSelect: () => void;
  onKeyDown: (e: KeyboardEvent<HTMLButtonElement>) => void;
  onHover: (day: string | null) => void;
}

function DayCell({
  day,
  from,
  to,
  preview,
  blocked,
  disabled,
  tabbable,
  cellRef,
  onSelect,
  onKeyDown,
  onHover,
}: DayCellProps) {
  const isStart = day === from;
  const isEnd = day === to;
  const inRange = from !== null && to !== null && day >= from && day <= to;
  const span = from !== null && to !== null && to > from;
  const today = day === new Date().toLocaleDateString("en-CA");

  // Orden del día dentro del rango: el relleno avanza de una fecha a la otra.
  const index =
    inRange && from
      ? Math.round(
          (toDate(day).getTime() - toDate(from).getTime()) / 86_400_000,
        )
      : 0;

  const label = [
    DAY_LABEL.format(toDate(day)),
    isStart && "recogida",
    isEnd && !preview && "devolución",
    blocked && "ocupado",
  ]
    .filter(Boolean)
    .join(", ");

  return (
    <div
      role="gridcell"
      className="relative flex h-11 items-center justify-center"
    >
      {inRange && span && (
        <m.span
          aria-hidden="true"
          // La key cambia con el rango: el relleno vuelve a dibujarse al elegir otras fechas.
          key={`${from}-${to}`}
          initial={preview ? false : { scaleX: 0 }}
          animate={{ scaleX: 1 }}
          transition={{
            duration: 0.16,
            delay: Math.min(index, 40) * 0.025,
            ease: "easeOut",
          }}
          className={cn(
            "absolute inset-y-1 origin-left",
            preview ? "bg-secondary" : "bg-primary-soft",
            isStart
              ? "left-1/2 right-0"
              : isEnd
                ? "left-0 right-1/2"
                : "inset-x-0",
          )}
        />
      )}
      <button
        ref={cellRef}
        type="button"
        tabIndex={tabbable ? 0 : -1}
        aria-label={label}
        aria-pressed={isStart || (isEnd && !preview)}
        aria-disabled={disabled || undefined}
        onClick={onSelect}
        onKeyDown={onKeyDown}
        onMouseEnter={() => onHover(day)}
        onMouseLeave={() => onHover(null)}
        className={cn(
          "relative z-10 grid size-10 place-items-center rounded-full text-[13.5px] font-semibold tabular-nums outline-none",
          "focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-1",
          disabled
            ? "cursor-default text-muted-foreground/50"
            : "hover:ring-1 hover:ring-foreground",
          blocked && "text-muted-foreground/60 line-through decoration-1",
          (isStart || (isEnd && !preview)) &&
            "bg-primary text-primary-foreground no-underline hover:ring-0",
          isEnd && preview && "ring-1 ring-foreground",
          today &&
            !isStart &&
            !isEnd &&
            "underline decoration-highlight decoration-2 underline-offset-4",
        )}
      >
        {Number(day.slice(8))}
      </button>
    </div>
  );
}
