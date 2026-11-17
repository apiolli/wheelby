import { ChevronLeft } from "lucide-react";
import { Link, useNavigate } from "react-router";

import { Button, buttonVariants } from "@/components/ui/button";
import { isAdmin, useSession } from "@/stores/session.store";
import { LostCarScene, TollBarrierScene } from "./Scenes";

function Shell({
  scene,
  eyebrow,
  title,
  text,
  action,
}: {
  scene: React.ReactNode;
  eyebrow: string;
  title: string;
  text: string;
  action: React.ReactNode;
}) {
  return (
    <main className="grid min-h-[70vh] place-items-center px-gutter py-12">
      <div className="grid max-w-120 justify-items-center gap-3 text-center">
        {scene}
        <p className="mt-2 text-[12.5px] font-bold tracking-widest text-muted-foreground">
          {eyebrow}
        </p>
        <h1 className="text-[26px] leading-tight font-bold tracking-tight md:text-[30px]">
          {title}
        </h1>
        <p className="text-[15px] text-muted-foreground">{text}</p>
        <div className="mt-3">{action}</div>
      </div>
    </main>
  );
}

// 404: rutas que no existen y también recursos inexistentes o ajenos (no se revela si existen).
export function NotFoundPage() {
  const user = useSession((s) => s.user);
  const admin = isAdmin(user);
  return (
    <Shell
      scene={<LostCarScene />}
      eyebrow="404"
      title="Esta ruta no existe."
      text="Parece que te desviaste."
      action={
        <Link
          to={admin ? "/admin" : "/buscar"}
          className={buttonVariants({
            size: "lg",
            className: "rounded-full px-8",
          })}
        >
          {admin ? "Volver al panel" : "Volver al catálogo"}
        </Link>
      }
    />
  );
}

// 403: sin permiso. La variante de anfitrión lleva a pedir el permiso desde el perfil.
export function ForbiddenPage({
  variant = "default",
}: {
  variant?: "default" | "host";
}) {
  const navigate = useNavigate();
  if (variant === "host")
    return (
      <Shell
        scene={<TollBarrierScene />}
        eyebrow="403"
        title="Necesitas ser anfitrión verificado para entrar aquí."
        text="Solicita el permiso de anfitrión desde tu perfil. Un administrador lo revisa."
        action={
          <Link
            to="/perfil/anfitrion"
            className={buttonVariants({
              size: "lg",
              className: "rounded-full px-8",
            })}
          >
            Ir a mi perfil
          </Link>
        }
      />
    );
  return (
    <Shell
      scene={<TollBarrierScene />}
      eyebrow="403"
      title="No tienes acceso a esto."
      text="Esta sección es solo para quien tiene el permiso correspondiente."
      action={
        <Button
          size="lg"
          variant="outline"
          className="rounded-full px-8"
          // Si se entró directo por URL no hay historial: se vuelve al inicio.
          onClick={() =>
            window.history.length > 1 ? navigate(-1) : navigate("/")
          }
        >
          <ChevronLeft /> Volver
        </Button>
      }
    />
  );
}
