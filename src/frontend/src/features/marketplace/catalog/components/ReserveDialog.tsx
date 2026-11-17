import { useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { CircleAlert, CircleCheck } from "lucide-react";
import { Link } from "react-router";

import { errorMessage } from "@/api/errors";
import { requestReservation } from "@/api/wheelby.api";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { FloatingField } from "@/features/auth/shared/components/FloatingField";
import { SubmitButton } from "@/features/auth/shared/components/SubmitButton";
import { formatDateRange, formatPrice } from "@/lib/format";
import { queryKeys } from "@/lib/query-keys";
import {
  addDays,
  dayStartIso,
  daysBetween,
  fieldErrors,
  rangeSchema,
  today,
} from "@/lib/schemas";
import type { Vehicle } from "@/types/types";
import { VEHICLE_ILLUSTRATION, vehicleName, vehiclePrice } from "../vehicle";

interface ReserveDialogProps {
  vehicle: Vehicle | null;
  from: string;
  to: string;
  onClose: () => void;
}

// Solicitud de reserva: el anfitrión la confirma después. Se abre desde la URL (?reservar=<id>).
export default function ReserveDialog({
  vehicle,
  from,
  to,
  onClose,
}: ReserveDialogProps) {
  return (
    <Dialog open={vehicle !== null} onOpenChange={(open) => !open && onClose()}>
      <DialogContent className="max-w-110">
        <DialogHeader>
          <DialogTitle>Solicitar reserva</DialogTitle>
        </DialogHeader>
        {vehicle && (
          <ReserveForm
            key={vehicle.id}
            vehicle={vehicle}
            from={from}
            to={to}
            onClose={onClose}
          />
        )}
      </DialogContent>
    </Dialog>
  );
}

function ReserveForm({
  vehicle,
  from: initialFrom,
  to: initialTo,
  onClose,
}: Required<Omit<ReserveDialogProps, "vehicle">> & { vehicle: Vehicle }) {
  const queryClient = useQueryClient();
  const [range, setRange] = useState({ from: initialFrom, to: initialTo });
  const [touched, setTouched] = useState(false);
  const invalid = touched ? fieldErrors(rangeSchema, range) : {};
  const valid = rangeSchema.safeParse(range).success;
  const days = valid ? daysBetween(range.from, range.to) : 0;
  const price = vehiclePrice(vehicle);

  const reserve = useMutation({
    mutationFn: () =>
      requestReservation(
        vehicle.id,
        dayStartIso(range.from),
        dayStartIso(range.to),
      ),
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: queryKeys.myReservations("renter"),
      }),
  });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    setTouched(true);
    if (valid) reserve.mutate();
  };

  if (reserve.isSuccess) {
    return (
      <div className="grid gap-5 p-6">
        <Alert variant="success" role="status">
          <CircleCheck />
          <AlertDescription>
            Enviamos tu solicitud para el{" "}
            {formatDateRange(range.from, range.to)}. El anfitrión la confirmará
            pronto.
          </AlertDescription>
        </Alert>
        <div className="flex flex-col-reverse gap-2 min-[480px]:flex-row min-[480px]:justify-end">
          <Button variant="outline" onClick={onClose}>
            Seguir explorando
          </Button>
          <Link to="/perfil/reservas" className={buttonVariants()}>
            Ver mis reservas
          </Link>
        </div>
      </div>
    );
  }

  return (
    <form onSubmit={submit} noValidate className="grid gap-5 p-6">
      <div className="flex items-center gap-4">
        <img
          src={VEHICLE_ILLUSTRATION}
          alt=""
          className="aspect-4/3 w-24 shrink-0 rounded-xl object-cover"
        />
        <div className="min-w-0">
          <p className="truncate text-[15px] font-bold">
            {vehicleName(vehicle)}
          </p>
          <p className="text-sm text-muted-foreground">
            {vehicle.year} · Placa {vehicle.plate}
          </p>
          <p className="mt-1 text-sm">
            <strong>{price.main}</strong> por día
          </p>
        </div>
      </div>

      {reserve.isError && (
        <Alert variant="destructive">
          <CircleAlert />
          <AlertDescription>{errorMessage(reserve.error)}</AlertDescription>
        </Alert>
      )}

      <div className="grid grid-cols-2 gap-3 [&>div]:mb-0">
        <FloatingField
          id="reserve-from"
          label="Recogida"
          type="date"
          min={addDays(today(), 1)}
          value={range.from}
          onChange={(e) => {
            const f = e.target.value;
            setRange((r) => ({
              from: f,
              to: f && r.to <= f ? addDays(f, 1) : r.to,
            }));
          }}
          invalid={Boolean(invalid.from)}
          hint={invalid.from}
        />
        <FloatingField
          id="reserve-to"
          label="Devolución"
          type="date"
          min={range.from ? addDays(range.from, 1) : undefined}
          value={range.to}
          onChange={(e) => setRange((r) => ({ ...r, to: e.target.value }))}
          invalid={Boolean(invalid.to)}
          hint={invalid.to}
        />
      </div>

      {valid && (
        <dl className="grid gap-2 border-t border-border-soft pt-4 text-sm">
          <div className="flex justify-between gap-4">
            <dt className="text-muted-foreground">
              {formatPrice(vehicle.pricePerDay)} × {days}{" "}
              {days === 1 ? "día" : "días"}
            </dt>
            <dd>{formatPrice(vehicle.pricePerDay * days)}</dd>
          </div>
          <div className="flex justify-between gap-4 text-[15px] font-bold">
            <dt>Total estimado</dt>
            <dd>{formatPrice(vehicle.pricePerDay * days)}</dd>
          </div>
        </dl>
      )}

      <SubmitButton loading={reserve.isPending}>Solicitar reserva</SubmitButton>
      <p className="-mt-2 text-center text-[12.5px] text-muted-foreground">
        Aún no se te cobrará nada.
      </p>
    </form>
  );
}
