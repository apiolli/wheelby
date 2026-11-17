import { Menu } from "lucide-react";
import { useNavigate } from "react-router";
import { cn } from "cn";

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useHostStatus } from "@/features/host/useHostStatus";
import { initial } from "@/lib/format";
import { isAdmin, useSession } from "@/stores/session.store";
import type { User } from "@/types/types";

// Menú del avatar. El Administrador ve una píldora "Admin" junto al avatar y el panel arriba y
// resaltado; el anfitrión verificado, sus vehículos y las reservas recibidas.
export default function UserMenu({ user }: { user: User }) {
  const navigate = useNavigate();
  const logout = useSession((s) => s.logout);
  const admin = isAdmin(user);

  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger
        aria-label={
          admin ? "Menú de usuario (Administrador)" : "Menú de usuario"
        }
        className="ml-1 flex h-11 items-center gap-2 rounded-full border border-border bg-background py-1.25 pr-1.25 pl-3.5 transition-shadow outline-none hover:shadow-float focus-visible:ring-2 focus-visible:ring-ring data-popup-open:shadow-float"
      >
        <Menu className="size-4" strokeWidth={2} />
        {admin && (
          <span className="rounded-full bg-primary-soft px-2 py-0.5 text-[11px] font-bold text-primary">
            Admin
          </span>
        )}
        <span className="grid size-8 place-items-center rounded-full bg-primary text-sm font-bold text-primary-foreground">
          {initial(user.fullName)}
        </span>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end">
        {admin ? (
          <DropdownMenuItem
            className="bg-primary-soft font-semibold text-primary data-highlighted:bg-primary-soft"
            onClick={() => navigate("/admin")}
          >
            Panel de administración
          </DropdownMenuItem>
        ) : (
          <MemberItems navigate={navigate} />
        )}
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={() => navigate("/perfil")}>
          Perfil
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => navigate("/perfil/documentos")}>
          Documentos
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={logout}>Cerrar sesión</DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

function MemberItems({ navigate }: { navigate: (to: string) => void }) {
  const { status } = useHostStatus();
  const item = (to: string, label: string, strong = true) => (
    <DropdownMenuItem
      className={cn(strong && "font-semibold")}
      onClick={() => navigate(to)}
    >
      {label}
    </DropdownMenuItem>
  );
  return (
    <>
      {item("/perfil/reservas", "Mis reservas")}
      {status === "verified" ? (
        <>
          {item("/anfitrion/vehiculos", "Mis vehículos")}
          {item("/anfitrion/reservas", "Reservas recibidas")}
        </>
      ) : status === "pending" ? (
        item("/perfil/anfitrion", "Solicitud de anfitrión", false)
      ) : (
        item("/perfil/anfitrion", "Conviértete en anfitrión", false)
      )}
    </>
  );
}
