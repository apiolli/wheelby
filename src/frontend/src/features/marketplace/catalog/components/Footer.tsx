import { Link } from "react-router";

import logoUrl from "@/assets/svg/wheelby-logo-principal.svg";
import { Separator } from "@/components/ui/separator";
import { isAdmin, useSession } from "@/stores/session.store";

const MEMBER_COLUMNS = [
  {
    title: "Alquila",
    links: [
      { to: "/", label: "Buscar vehículos" },
      { to: "/perfil/reservas", label: "Mis reservas" },
    ],
  },
  {
    title: "Anfitriones",
    links: [
      { to: "/perfil/anfitrion", label: "Conviértete en anfitrión" },
      { to: "/anfitrion/reservas", label: "Solicitudes recibidas" },
    ],
  },
  {
    title: "Tu cuenta",
    links: [
      { to: "/perfil/cuenta", label: "Perfil" },
      { to: "/perfil/seguridad", label: "Seguridad" },
      { to: "/perfil/documentos", label: "Documentos" },
    ],
  },
];

const ADMIN_COLUMNS = [
  {
    title: "Administración",
    links: [
      { to: "/admin", label: "Resumen" },
      { to: "/admin/usuarios", label: "Usuarios" },
      { to: "/admin/verificaciones", label: "Verificaciones" },
    ],
  },
  MEMBER_COLUMNS[2],
];

export default function Footer() {
  const user = useSession((s) => s.user);
  const columns = isAdmin(user) ? ADMIN_COLUMNS : MEMBER_COLUMNS;

  return (
    <footer className="mt-16 border-t border-border-soft bg-secondary px-gutter pt-12 text-sm">
      <div className="mb-10 flex flex-col gap-2 md:flex-row md:items-center md:justify-between">
        <img
          src={logoUrl}
          alt="Wheelby"
          width={146}
          height={36}
          className="h-9 w-auto self-start"
        />
        <p className="max-w-sm text-muted-foreground">
          Alquila autos, motos y ligeros de personas cerca de ti.
        </p>
      </div>
      <div className="grid gap-8 pb-10 min-[480px]:grid-cols-2 md:grid-cols-4">
        {columns.map((col) => (
          <div key={col.title}>
            <h2 className="mb-3.5 font-bold">{col.title}</h2>
            <ul className="grid gap-3">
              {col.links.map((l) => (
                <li key={l.to}>
                  <Link to={l.to} className="hover:underline">
                    {l.label}
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        ))}
      </div>
      <Separator />
      <div className="flex flex-wrap items-center justify-between gap-3 pt-5 pb-6">
        <small className="flex flex-wrap items-center gap-x-3 text-sm">
          © {new Date().getFullYear()} Wheelby
          <Link to="/terminos" className="hover:underline">
            Términos
          </Link>
          <Link to="/privacidad" className="hover:underline">
            Privacidad
          </Link>
        </small>
        <span className="text-sm text-muted-foreground">
          Español (DO) · Precios en DOP
        </span>
      </div>
    </footer>
  );
}
