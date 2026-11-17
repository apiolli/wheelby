import { useState } from "react";
import { m, useReducedMotion } from "motion/react";
import { Search } from "lucide-react";
import { cn } from "cn";
import { useLocation, useNavigate, useSearchParams } from "react-router";

import RangeCalendar, {
  type DayRange,
} from "@/components/custom/RangeCalendar";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetTitle } from "@/components/ui/sheet";
import { useScrollHidden } from "@/hooks/useScrollHidden";
import { formatRangeWords } from "@/lib/format";
import { HEADER_SPRING } from "@/lib/motion";
import { addDays, CURRENCIES, today, type Currency } from "@/lib/schemas";
import { readSearch } from "../search/params";
import Logo from "./Logo";
import { CURRENCY_LABEL } from "./search";

// Header móvil: logo y píldora "¿Cuándo lo necesitas?". Se oculta al bajar y reaparece al subir.
// La píldora abre la búsqueda a pantalla completa (fechas, moneda y "Buscar").
export default function MobileHeader() {
  const hidden = useScrollHidden();
  const reduce = useReducedMotion();
  const [open, setOpen] = useState(false);
  const [params] = useSearchParams();
  const { pathname } = useLocation();
  const onSearch = pathname === "/" || pathname === "/buscar";
  const current = readSearch(onSearch ? params : new URLSearchParams());

  return (
    <>
      <m.header
        initial={false}
        animate={{ y: hidden && !open ? "-100%" : 0 }}
        transition={reduce ? { duration: 0 } : HEADER_SPRING}
        className="fixed inset-x-0 top-0 z-40 border-b border-border-soft bg-background pt-[env(safe-area-inset-top)]"
      >
        <div className="flex h-16 items-center gap-3 px-gutter">
          <Logo variant="symbol" />
          <button
            type="button"
            onClick={() => setOpen(true)}
            className="flex h-12 min-w-0 flex-1 items-center gap-3 rounded-full border border-border bg-background pr-5 pl-4 text-left shadow-float outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            <Search className="size-4.5 shrink-0" strokeWidth={2.6} />
            <span className="min-w-0">
              <span className="block truncate text-sm font-bold">
                ¿Cuándo lo necesitas?
              </span>
              <span className="block truncate text-xs text-muted-foreground">
                {current.hasDates
                  ? formatRangeWords(current.from, current.to)
                  : "Elige tus fechas"}{" "}
                · {current.currency}
              </span>
            </span>
          </button>
        </div>
      </m.header>
      <div aria-hidden="true" className="h-16" />
      <SearchScreen open={open} onOpenChange={setOpen} current={current} />
    </>
  );
}

function SearchScreen({
  open,
  onOpenChange,
  current,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  current: ReturnType<typeof readSearch>;
}) {
  const navigate = useNavigate();
  const [range, setRange] = useState<DayRange>({ from: null, to: null });
  const [currency, setCurrency] = useState<Currency>("DOP");
  const complete = range.from !== null && range.to !== null;

  const search = () => {
    const params = new URLSearchParams({
      desde: range.from!,
      hasta: range.to!,
    });
    if (currency !== "DOP") params.set("moneda", currency);
    onOpenChange(false);
    navigate(`/buscar?${params}`);
  };

  return (
    <Sheet
      open={open}
      onOpenChange={(next) => {
        if (next) {
          setRange(
            current.hasDates
              ? { from: current.from, to: current.to }
              : { from: null, to: null },
          );
          setCurrency(current.currency);
        }
        onOpenChange(next);
      }}
    >
      <SheetContent
        side="bottom"
        className="flex h-dvh max-h-none flex-col rounded-none"
      >
        <div className="flex h-16 shrink-0 items-center justify-center border-b border-border-soft px-14 pt-[env(safe-area-inset-top)]">
          <SheetTitle className="text-[16px] font-bold">
            ¿Cuándo lo necesitas?
          </SheetTitle>
        </div>
        <div className="flex-1 overflow-y-auto px-gutter py-5">
          <section
            aria-labelledby="ms-dates"
            className="rounded-[20px] border border-border p-4 shadow-float"
          >
            <h2 id="ms-dates" className="mb-3 text-[15px] font-bold">
              Fechas
            </h2>
            <RangeCalendar
              value={range}
              onChange={setRange}
              min={addDays(today(), 1)}
            />
          </section>
          <section
            aria-labelledby="ms-currency"
            className="mt-4 rounded-[20px] border border-border p-4"
          >
            <h2 id="ms-currency" className="mb-3 text-[15px] font-bold">
              Moneda
            </h2>
            <div
              role="radiogroup"
              aria-labelledby="ms-currency"
              className="grid gap-2"
            >
              {CURRENCIES.map((c) => (
                <button
                  key={c}
                  type="button"
                  role="radio"
                  aria-checked={currency === c}
                  onClick={() => setCurrency(c)}
                  className={cn(
                    "flex h-12 items-center justify-between rounded-xl border px-4 text-[14.5px] outline-none focus-visible:ring-2 focus-visible:ring-ring",
                    currency === c
                      ? "border-foreground font-semibold"
                      : "border-border",
                  )}
                >
                  {CURRENCY_LABEL[c]}
                  <span
                    aria-hidden="true"
                    className={cn(
                      "size-5 rounded-full border-2",
                      currency === c
                        ? "border-[6px] border-primary"
                        : "border-input",
                    )}
                  />
                </button>
              ))}
            </div>
          </section>
        </div>
        <div className="flex shrink-0 items-center justify-between gap-3 border-t border-border-soft px-gutter pt-3 pb-safe">
          <Button
            variant="ghost"
            className="h-11 rounded-full underline underline-offset-4"
            onClick={() => setRange({ from: null, to: null })}
          >
            Borrar
          </Button>
          <Button
            size="lg"
            className="h-12 rounded-full px-7"
            disabled={!complete}
            onClick={search}
          >
            <Search /> Buscar
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}
