import { useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { AnimatePresence, m } from "motion/react";
import {
  Ban,
  CalendarCheck,
  CalendarX2,
  ChevronLeft,
  CircleAlert,
  HandCoins,
  LoaderCircle,
  RotateCw,
  UserCheck,
} from "lucide-react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router";

import { ERROR_CODES, toApiError } from "@/api/errors";
import { requestReservation } from "@/api/wheelby.api";
import EmptyState from "@/components/custom/EmptyState";
import { CarIllustration } from "@/components/custom/Illustrations";
import VehicleArt from "@/components/custom/VehicleArt";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import { features } from "@/lib/features";
import { ENTER } from "@/lib/motion";
import { queryKeys } from "@/lib/query-keys";
import { dayStartIso, daysBetween } from "@/lib/schemas";
import { useSession } from "@/stores/session.store";
import { approxPrice, vehicleName } from "../catalog/vehicle";
import DatesPopover from "../search/components/DatesPopover";
import { readSearch, searchQueryString } from "../search/params";
import { useBlockedDays } from "../vehicle/availability";
import DateRangeField from "../vehicle/components/DateRangeField";
import PriceBreakdown from "../vehicle/components/PriceBreakdown";
import { useVehicle } from "../vehicle/useVehicle";

// Estado que la página de destino lee para mostrar el aviso de éxito.
export interface RequestedState {
  requested: true;
}

type Problem =
  | { kind: "overlap"; message: string }
  | { kind: "own"; message: string }
  | { kind: "banner"; message: string; retry: boolean };

// Flujo de reserva: revisar fechas, entender cómo funciona y enviar la solicitud. Las fechas
// viven en la URL (?desde=&hasta=); el resto de reglas las informa el servidor.
export default function ReservePage() {
  const { vehiculoId } = useParams();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [params, setParams] = useSearchParams();
  const search = readSearch(params);
  const user = useSession((s) => s.user)!;
  const { vehicle, isPending, unreachable, rate } = useVehicle(vehiculoId);
  const { blocked } = useBlockedDays(vehicle?.id);

  const [datesOpen, setDatesOpen] = useState(false);
  const [triedSubmit, setTriedSubmit] = useState(false);
  const [problem, setProblem] = useState<Problem | null>(null);
  const [fieldError, setFieldError] = useState<string | null>(null);

  const range = search.hasDates ? { from: search.from, to: search.to } : null;
  const days = range ? daysBetween(range.from, range.to) : 0;

  const setRange = (r: { from: string; to: string }) => {
    const next = new URLSearchParams(params);
    next.set("desde", r.from);
    next.set("hasta", r.to);
    setParams(next, { replace: true, state: { rate, vehicle } });
    setProblem((p) => (p?.kind === "overlap" ? null : p));
    setFieldError(null);
  };

  const send = useMutation({
    mutationFn: (r: { from: string; to: string }) =>
      requestReservation(vehicle!.id, dayStartIso(r.from), dayStartIso(r.to)),
    onSuccess: ({ id }) => {
      queryClient.invalidateQueries({
        queryKey: queryKeys.myReservations("renter"),
      });
      const state: RequestedState = { requested: true };
      navigate(
        features.reservationDetail ? `/reservas/${id}` : "/perfil/reservas",
        { state },
      );
    },
    onError: (error) => {
      const e = toApiError(error);
      if (e.code === ERROR_CODES.overlap) {
        setProblem({ kind: "overlap", message: e.message });
        // Las fechas ocupadas cambiaron: el calendario debe volver a leerlas.
        queryClient.invalidateQueries({
          queryKey: ["vehicles", vehicle!.id, "availability"],
        });
      } else if (e.code === ERROR_CODES.ownVehicle)
        setProblem({ kind: "own", message: e.message });
      else if (e.status === 400 && (e.fields.start || e.fields.end))
        setFieldError(e.fields.start ?? e.fields.end);
      else
        setProblem({
          kind: "banner",
          message: e.message,
          retry: e.status === null || e.status >= 500,
        });
    },
  });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    setTriedSubmit(true);
    setProblem(null);
    setFieldError(null);
    if (!range) return;
    send.mutate(range);
  };

  if (unreachable)
    return (
      <Centered>
        <EmptyState
          illustration={<CarIllustration className="w-37.5" />}
          title="Busca fechas para reservar"
          text="Abre la reserva desde la ficha de un vehículo."
          action={
            <Link
              to="/buscar"
              className={buttonVariants({
                size: "lg",
                className: "rounded-full",
              })}
            >
              Ir al catálogo
            </Link>
          }
        />
      </Centered>
    );
  if (!vehicle)
    return isPending ? (
      <main className="mx-auto max-w-280 px-gutter py-12" aria-busy="true">
        <div className="grid animate-pulse gap-10 lg:grid-cols-[1fr_400px]">
          <div className="h-96 rounded-[20px] bg-secondary" />
          <div className="h-80 rounded-[20px] bg-secondary" />
        </div>
      </main>
    ) : (
      <Centered>
        <EmptyState
          illustration={<CarIllustration className="w-37.5" />}
          title="Este vehículo no está disponible"
          text="Puede que ya no exista. Busca otro en el catálogo."
          action={
            <Link
              to="/buscar"
              className={buttonVariants({
                size: "lg",
                className: "rounded-full",
              })}
            >
              Explorar vehículos
            </Link>
          }
        />
      </Centered>
    );

  const back = `/vehiculos/${vehicle.id}${searchQueryString(search)}`;

  // Vehículo propio: se bloquea antes de enviar (y también si el servidor responde 403).
  if (vehicle.ownerId === user.id || problem?.kind === "own")
    return (
      <Centered>
        <m.div
          {...ENTER}
          className="grid justify-items-center gap-4 rounded-[20px] border border-border p-10 text-center"
        >
          <span className="grid size-14 place-items-center rounded-full bg-destructive-soft text-destructive">
            <Ban className="size-6" strokeWidth={1.75} />
          </span>
          <h1 className="text-xl font-bold tracking-tight">
            No puedes reservar tu propio vehículo
          </h1>
          <p className="max-w-90 text-[14.5px] text-muted-foreground">
            {problem?.kind === "own"
              ? problem.message
              : "Este vehículo lo publicaste tú. Las reservas las hacen otros usuarios."}
          </p>
          <Link
            to={back}
            className={buttonVariants({
              variant: "outline",
              size: "lg",
              className: "rounded-full",
            })}
          >
            <ChevronLeft /> Volver a la ficha
          </Link>
        </m.div>
      </Centered>
    );

  const missingDates = triedSubmit && !range;
  const datesInvalid =
    missingDates || problem?.kind === "overlap" || Boolean(fieldError);

  return (
    <main className="px-gutter pt-4 pb-16">
      <div className="mx-auto max-w-280">
        <Link
          to={back}
          className="mb-4 inline-flex items-center gap-1 rounded-full py-1 pr-2 text-[13.5px] font-semibold text-muted-foreground outline-none hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring"
        >
          <ChevronLeft className="size-4" /> Volver a la ficha
        </Link>
        <h1 className="mb-8 text-[26px] leading-tight font-bold tracking-tight md:text-[30px]">
          Solicitar reserva
        </h1>

        <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_400px] lg:gap-16">
          <form
            onSubmit={submit}
            noValidate
            className="grid content-start gap-9"
          >
            <section aria-labelledby="trip-title" className="grid gap-4">
              <h2 id="trip-title" className="text-xl font-bold tracking-tight">
                Revisa tu viaje
              </h2>
              <DatesPopover
                value={range}
                onApply={setRange}
                open={datesOpen}
                onOpenChange={setDatesOpen}
                blocked={blocked}
                showLegend={features.availability}
                trigger={
                  <DateRangeField
                    from={range?.from ?? null}
                    to={range?.to ?? null}
                    invalid={datesInvalid}
                    aria-describedby="dates-hint"
                    className="max-w-120"
                  />
                }
              />
              <p
                id="dates-hint"
                className={
                  missingDates || fieldError
                    ? "-mt-2 text-[13px] text-destructive"
                    : "-mt-2 text-[13px] text-muted-foreground"
                }
              >
                {missingDates
                  ? "Elige la recogida y la devolución."
                  : (fieldError ??
                    "La recogida empieza a partir de mañana. Toca para cambiar las fechas.")}
              </p>

              <AnimatePresence initial={false}>
                {problem?.kind === "overlap" && (
                  <m.div
                    key="overlap"
                    initial={{ opacity: 0, y: -6 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0 }}
                  >
                    <Alert variant="destructive" className="max-w-120">
                      <CalendarX2 />
                      <AlertDescription>
                        {problem.message}
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          className="mt-3 rounded-full"
                          onClick={() => setDatesOpen(true)}
                        >
                          Cambiar fechas
                        </Button>
                      </AlertDescription>
                    </Alert>
                  </m.div>
                )}
              </AnimatePresence>
            </section>

            <section
              aria-labelledby="how-title"
              className="grid gap-4 border-t border-border-soft pt-8"
            >
              <h2 id="how-title" className="text-xl font-bold tracking-tight">
                Cómo funciona
              </h2>
              <ul className="grid gap-4 text-[14.5px]">
                <Step icon={UserCheck}>
                  El anfitrión confirma o rechaza tu solicitud.
                </Step>
                <Step icon={CalendarCheck}>
                  Puedes cancelar antes de la fecha de inicio.
                </Step>
                <Step icon={HandCoins}>
                  No se cobra nada en línea. El precio se acuerda en DOP.
                </Step>
              </ul>
            </section>

            <div className="grid gap-4 border-t border-border-soft pt-8">
              <AnimatePresence initial={false}>
                {problem?.kind === "banner" && (
                  <m.div
                    key="banner"
                    initial={{ opacity: 0, y: -6 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0 }}
                  >
                    <Alert variant="destructive" className="max-w-120">
                      <CircleAlert />
                      <AlertDescription>
                        {problem.message}
                        {problem.retry && (
                          <Button
                            type="submit"
                            variant="outline"
                            size="sm"
                            className="mt-3 rounded-full"
                          >
                            <RotateCw /> Reintentar
                          </Button>
                        )}
                      </AlertDescription>
                    </Alert>
                  </m.div>
                )}
              </AnimatePresence>
              <SendButton pending={send.isPending} />
            </div>
          </form>

          <aside className="self-start lg:sticky lg:top-28">
            <div className="grid gap-5 rounded-[20px] border border-border p-6 shadow-float">
              <div className="flex items-center gap-4">
                <div className="aspect-4/3 w-28 shrink-0 overflow-hidden rounded-xl">
                  <VehicleArt brand={vehicle.brand} />
                </div>
                <div className="min-w-0">
                  <p className="truncate text-[16px] font-bold">
                    {vehicleName(vehicle)}
                  </p>
                  <p className="text-[14px] text-muted-foreground">
                    {vehicle.year}
                  </p>
                </div>
              </div>
              <div className="border-t border-border-soft pt-5">
                {range ? (
                  <PriceBreakdown
                    pricePerDay={vehicle.pricePerDay}
                    days={days}
                    approxTotal={approxPrice(vehicle.pricePerDay * days, rate)}
                  />
                ) : (
                  <p className="text-[14.5px] text-muted-foreground">
                    Elige tus fechas para ver el total estimado.
                  </p>
                )}
              </div>
              <p className="text-[12.5px] text-muted-foreground">
                Se acuerda en DOP. El total es una estimación hasta que el
                anfitrión confirme.
              </p>
            </div>
          </aside>
        </div>
      </div>
    </main>
  );
}

function Step({
  icon: Icon,
  children,
}: {
  icon: typeof UserCheck;
  children: React.ReactNode;
}) {
  return (
    <li className="flex items-center gap-4">
      <span className="grid size-10 shrink-0 place-items-center rounded-full bg-primary-soft text-primary">
        <Icon className="size-5" strokeWidth={1.5} />
      </span>
      {children}
    </li>
  );
}

// Al enviar, el botón se encoge a un círculo con el indicador de carga (layout de Motion).
function SendButton({ pending }: { pending: boolean }) {
  return (
    <div className="flex">
      <m.button
        layout
        type="submit"
        disabled={pending}
        aria-busy={pending}
        transition={{ type: "spring", bounce: 0, duration: 0.35 }}
        style={{ borderRadius: 999 }}
        className="grid h-13 place-items-center bg-primary text-[15px] font-semibold text-primary-foreground outline-none hover:bg-primary-hover focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-wait"
      >
        <AnimatePresence mode="popLayout" initial={false}>
          {pending ? (
            <m.span
              key="spin"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="grid size-13 place-items-center"
            >
              <LoaderCircle
                className="size-5 animate-spin"
                aria-label="Enviando"
              />
            </m.span>
          ) : (
            <m.span
              key="label"
              layout="position"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="px-8"
            >
              Enviar solicitud
            </m.span>
          )}
        </AnimatePresence>
      </m.button>
    </div>
  );
}

function Centered({ children }: { children: React.ReactNode }) {
  return <main className="mx-auto max-w-160 px-gutter py-12">{children}</main>;
}
