import { KeyRound, ShieldCheck } from "lucide-react";
import { Link } from "react-router";

import { buttonVariants } from "@/components/ui/button";

// Variantes del panel de reserva cuando quien mira no puede reservar.

export function OwnVehicleNotice() {
  return (
    <div className="grid gap-3 text-[14.5px]">
      <span className="grid size-11 place-items-center rounded-full bg-primary-soft text-primary">
        <KeyRound className="size-5" strokeWidth={1.75} />
      </span>
      <h2 className="text-lg font-bold tracking-tight">
        Este vehículo es tuyo
      </h2>
      <p className="text-muted-foreground">
        Así lo ven los arrendatarios. No puedes reservar tu propio vehículo.
      </p>
      <Link
        to="/anfitrion/vehiculos"
        className={buttonVariants({
          variant: "outline",
          size: "lg",
          className: "mt-1 rounded-full",
        })}
      >
        Ver mis vehículos
      </Link>
    </div>
  );
}

export function AdminNotice() {
  return (
    <div className="grid gap-3 text-[14.5px]">
      <span className="grid size-11 place-items-center rounded-full bg-primary-soft text-primary">
        <ShieldCheck className="size-5" strokeWidth={1.75} />
      </span>
      <p className="font-semibold">
        Eres Administrador. Las reservas las realizan los usuarios.
      </p>
      <Link
        to="/admin"
        className={buttonVariants({
          variant: "outline",
          className: "rounded-full",
        })}
      >
        Volver al panel
      </Link>
    </div>
  );
}
