import { Menu } from "lucide-react";
import { useNavigate } from "react-router";

import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { initial } from "@/lib/format";
import { isAdmin, useSession } from "@/stores/session.store";
import type { User } from "@/types/types";

export default function UserMenu({ user }: { user: User }) {
  const navigate = useNavigate();
  const logout = useSession((s) => s.logout);
  const admin = isAdmin(user);

  return (
    <DropdownMenu modal={false}>
      <DropdownMenuTrigger
        aria-label="Menú de usuario"
        className="ml-1 flex h-11 items-center gap-2.5 rounded-full border border-border bg-background py-1.25 pr-1.25 pl-3.5 transition-shadow outline-none hover:shadow-float focus-visible:ring-2 focus-visible:ring-ring data-popup-open:shadow-float"
      >
        <Menu className="size-4" strokeWidth={2} />
        <span className="grid size-8 place-items-center rounded-full bg-primary text-sm font-bold text-primary-foreground">
          {initial(user.fullName)}
        </span>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end">
        {admin ? (
          <DropdownMenuItem
            className="font-semibold"
            onClick={() => navigate("/admin")}
          >
            Panel de administración
          </DropdownMenuItem>
        ) : (
          <>
            <DropdownMenuItem
              className="font-semibold"
              onClick={() => navigate("/perfil/reservas")}
            >
              Mis reservas
            </DropdownMenuItem>
            <DropdownMenuItem
              className="font-semibold"
              onClick={() => navigate("/anfitrion/reservas")}
            >
              Solicitudes recibidas
            </DropdownMenuItem>
          </>
        )}
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={() => navigate("/perfil")}>
          Perfil
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => navigate("/perfil/documentos")}>
          Documentos
        </DropdownMenuItem>
        {!admin && (
          <DropdownMenuItem onClick={() => navigate("/perfil/anfitrion")}>
            Conviértete en anfitrión
          </DropdownMenuItem>
        )}
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={logout}>Cerrar sesión</DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
