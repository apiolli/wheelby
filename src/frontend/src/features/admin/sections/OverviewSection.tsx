import { useQuery } from "@tanstack/react-query";
import { m } from "motion/react";
import {
  BadgeCheck,
  CalendarClock,
  MailWarning,
  UserCheck,
  UserX,
  type LucideIcon,
} from "lucide-react";
import { Link } from "react-router";
import { cn } from "cn";

import { listAllReservations, listPendingRequests } from "@/api/admin.api";
import { firstName } from "@/lib/format";
import { STAGGER_ITEM, STAGGER_LIST } from "@/lib/motion";
import { queryKeys } from "@/lib/query-keys";
import { useSession } from "@/stores/session.store";
import { ReservationStatus } from "@/types/types";
import AdminHeading from "../components/AdminHeading";
import CountUp from "../components/CountUp";
import { useAllUsers } from "../hooks/useAllUsers";

const TONE = {
  primary: "bg-primary-soft text-primary",
  highlight: "bg-highlight-soft text-highlight-strong",
  destructive: "bg-destructive-soft text-destructive",
  muted: "bg-secondary text-muted-foreground",
};

// Métricas simples: número grande, ícono y etiqueta. Cada tarjeta lleva a la lista filtrada.
export default function OverviewSection() {
  const user = useSession((s) => s.user);
  const users = useAllUsers();
  const requests = useQuery({
    queryKey: queryKeys.adminRequests,
    queryFn: () => listPendingRequests(1, 100),
  });
  const pendingReservations = useQuery({
    queryKey: queryKeys.adminReservations(ReservationStatus.Pending, 1),
    queryFn: () => listAllReservations(ReservationStatus.Pending, 1, 20),
  });

  const count = (s: string) => users.data?.filter((u) => u.status === s).length;

  const metrics: {
    label: string;
    value: number | undefined;
    icon: LucideIcon;
    tone: keyof typeof TONE;
    to: string;
  }[] = [
    {
      label: "Usuarios activos",
      value: count("Active"),
      icon: UserCheck,
      tone: "primary",
      to: "/admin/usuarios?estado=activa",
    },
    {
      label: "Solicitudes pendientes",
      value: requests.data?.totalItems,
      icon: BadgeCheck,
      tone: "highlight",
      to: "/admin/verificaciones",
    },
    {
      label: "Reservas por confirmar",
      value: pendingReservations.data?.totalItems,
      icon: CalendarClock,
      tone: "primary",
      to: "/admin/reservas?estado=solicitada",
    },
    {
      label: "Cuentas sin activar",
      value: count("PendingActivation"),
      icon: MailWarning,
      tone: "muted",
      to: "/admin/usuarios?estado=sin-activar",
    },
    {
      label: "Cuentas desactivadas",
      value: count("Disabled"),
      icon: UserX,
      tone: "destructive",
      to: "/admin/usuarios?estado=desactivada",
    },
  ];

  return (
    <>
      <AdminHeading
        title="Resumen"
        description={
          user
            ? `Hola, ${firstName(user.fullName)}. Esto es lo que necesita tu atención.`
            : undefined
        }
      />
      <m.ul
        {...STAGGER_LIST}
        className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5"
      >
        {metrics.map(({ label, value, icon: Icon, tone, to }) => (
          <m.li key={label} {...STAGGER_ITEM}>
            <Link
              to={to}
              className="flex h-full flex-col gap-4 rounded-2xl border border-border-soft bg-background p-5 transition-shadow outline-none hover:shadow-float focus-visible:ring-2 focus-visible:ring-ring"
            >
              <span
                className={cn(
                  "grid size-10 place-items-center rounded-xl",
                  TONE[tone],
                )}
              >
                <Icon className="size-5" strokeWidth={1.8} />
              </span>
              <span>
                {value === undefined ? (
                  <span className="block h-8.5 w-12 animate-pulse rounded-lg bg-secondary" />
                ) : (
                  <CountUp
                    value={value}
                    className="block text-[34px] leading-none font-bold tracking-tight tabular-nums"
                  />
                )}
                <span className="mt-1.5 block text-[14px] text-muted-foreground">
                  {label}
                </span>
              </span>
            </Link>
          </m.li>
        ))}
      </m.ul>
    </>
  );
}
