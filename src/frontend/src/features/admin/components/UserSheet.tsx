import type { ReactNode } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  KeyRound,
  ShieldCheck,
  UserCheck,
  UserX,
  type LucideIcon,
} from "lucide-react";

import {
  changeRole,
  disableUser,
  enableUser,
  forcePasswordReset,
} from "@/api/admin.api";
import ConfirmAction from "@/components/custom/ConfirmAction";
import DisabledHint from "@/components/custom/DisabledHint";
import { RoleBadge, UserStatusBadge } from "@/components/custom/StatusBadges";
import { Button } from "@/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import { formatDate, initial } from "@/lib/format";
import { ROLE_LABEL } from "@/lib/labels";
import { queryKeys } from "@/lib/query-keys";
import { showToast, useSession } from "@/stores/session.store";
import type { UserSummary } from "@/types/types";

interface UserSheetProps {
  user: UserSummary | undefined;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

// Panel lateral con el detalle de una cuenta y sus acciones, todas con confirmación.
export default function UserSheet({
  user,
  open,
  onOpenChange,
}: UserSheetProps) {
  const me = useSession((s) => s.user);

  return (
    <Sheet open={open && Boolean(user)} onOpenChange={onOpenChange}>
      <SheetContent>
        {user && (
          <>
            <SheetHeader className="flex items-center gap-4">
              <span className="grid size-12 shrink-0 place-items-center rounded-full bg-primary text-lg font-bold text-primary-foreground">
                {initial(user.fullName)}
              </span>
              <div className="min-w-0">
                <SheetTitle className="truncate">{user.fullName}</SheetTitle>
                <SheetDescription className="truncate">
                  {user.email}
                </SheetDescription>
              </div>
            </SheetHeader>

            <div className="flex-1 overflow-y-auto px-6 py-6">
              <dl className="grid grid-cols-[auto_1fr] items-center gap-x-6 gap-y-3.5 text-sm">
                <dt className="text-muted-foreground">Rol</dt>
                <dd>
                  <RoleBadge role={user.role} />
                </dd>
                <dt className="text-muted-foreground">Estado</dt>
                <dd>
                  <UserStatusBadge status={user.status} />
                </dd>
                <dt className="text-muted-foreground">Miembro desde</dt>
                <dd className="font-semibold">{formatDate(user.createdAt)}</dd>
              </dl>

              <h3 className="mt-8 mb-1 text-[13px] font-bold tracking-wide text-muted-foreground uppercase">
                Acciones
              </h3>
              <Actions key={user.id} user={user} isSelf={user.id === me?.id} />
            </div>
          </>
        )}
      </SheetContent>
    </Sheet>
  );
}

function Actions({ user, isSelf }: { user: UserSummary; isSelf: boolean }) {
  const queryClient = useQueryClient();
  const act = useMutation({
    mutationFn: ({ run }: { run: () => Promise<unknown>; done: string }) =>
      run(),
    onSuccess: (_, { done }) => {
      showToast(done);
      return queryClient.invalidateQueries({
        queryKey: queryKeys.adminUsers(),
      });
    },
  });
  const run = (fn: () => Promise<unknown>, done: string) =>
    act.mutateAsync({ run: fn, done });

  const nextRole = user.role === "Administrator" ? "Standard" : "Administrator";
  const disabled = user.status === "Disabled";

  const roleButton = (
    <Button variant="outline" size="sm" disabled={isSelf}>
      Cambiar a {ROLE_LABEL[nextRole]}
    </Button>
  );
  const accessButton = disabled ? (
    <Button variant="outline" size="sm" disabled={isSelf}>
      Reactivar
    </Button>
  ) : (
    <Button variant="destructive-outline" size="sm" disabled={isSelf}>
      Desactivar
    </Button>
  );

  return (
    <ul className="divide-y divide-border-soft">
      <ActionRow
        icon={ShieldCheck}
        title="Rol"
        text={
          user.role === "Administrator"
            ? "Puede entrar a este panel y gestionar usuarios."
            : "Usa Wheelby como arrendatario o anfitrión."
        }
      >
        {isSelf ? (
          <DisabledHint hint="No puedes cambiar tu propio rol.">
            {roleButton}
          </DisabledHint>
        ) : (
          <ConfirmAction
            trigger={roleButton}
            title={`¿Cambiar el rol de ${user.fullName}?`}
            description={
              nextRole === "Administrator"
                ? "Pasará a ser Administrador: podrá entrar a este panel, cambiar roles y aprobar solicitudes. Dejará de poder reservar y publicar."
                : "Pasará a ser Estándar y perderá el acceso a este panel."
            }
            confirmLabel={`Cambiar a ${ROLE_LABEL[nextRole]}`}
            onConfirm={() =>
              run(
                () => changeRole(user.id, nextRole),
                `${user.fullName} ahora es ${ROLE_LABEL[nextRole]}`,
              )
            }
          />
        )}
      </ActionRow>

      <ActionRow
        icon={disabled ? UserCheck : UserX}
        title={disabled ? "Reactivar cuenta" : "Desactivar cuenta"}
        text={
          disabled
            ? "Podrá volver a iniciar sesión."
            : "No podrá iniciar sesión hasta que la vuelvas a reactivar."
        }
      >
        {isSelf ? (
          <DisabledHint hint="No puedes desactivar tu propia cuenta.">
            {accessButton}
          </DisabledHint>
        ) : (
          <ConfirmAction
            trigger={accessButton}
            destructive={!disabled}
            title={
              disabled
                ? `¿Reactivar la cuenta de ${user.fullName}?`
                : `¿Desactivar la cuenta de ${user.fullName}?`
            }
            description={
              disabled
                ? "Podrá iniciar sesión de inmediato con su contraseña actual."
                : "No podrá iniciar sesión mientras esté desactivada. Puedes revertirlo cuando quieras."
            }
            confirmLabel={disabled ? "Reactivar cuenta" : "Desactivar cuenta"}
            onConfirm={() =>
              disabled
                ? run(() => enableUser(user.id), "Cuenta reactivada")
                : run(() => disableUser(user.id), "Cuenta desactivada")
            }
          />
        )}
      </ActionRow>

      <ActionRow
        icon={KeyRound}
        title="Restablecer contraseña"
        text="Anula su contraseña y le enviamos un código por correo para que elija una nueva."
      >
        <ConfirmAction
          trigger={
            <Button variant="outline" size="sm">
              Forzar
            </Button>
          }
          title="¿Forzar el restablecimiento de contraseña?"
          description={`Su contraseña actual dejará de funcionar y enviaremos un código a ${user.email} para que elija otra.`}
          confirmLabel="Enviar código"
          onConfirm={() =>
            run(
              () => forcePasswordReset(user.id),
              `Enviamos el código a ${user.email}`,
            )
          }
        />
      </ActionRow>
    </ul>
  );
}

function ActionRow({
  icon: Icon,
  title,
  text,
  children,
}: {
  icon: LucideIcon;
  title: string;
  text: string;
  children: ReactNode;
}) {
  return (
    // En pantallas estrechas el botón baja bajo el texto en vez de apretarlo.
    <li className="grid grid-cols-[20px_minmax(0,1fr)] gap-x-3 gap-y-3 py-4 min-[420px]:grid-cols-[20px_minmax(0,1fr)_auto] min-[420px]:items-center">
      <Icon
        className="mt-0.5 size-5 text-muted-foreground min-[420px]:mt-0"
        strokeWidth={1.7}
      />
      <div>
        <p className="text-sm font-semibold">{title}</p>
        <p className="text-[13px] text-muted-foreground">{text}</p>
      </div>
      <div className="col-start-2 min-[420px]:col-start-auto">{children}</div>
    </li>
  );
}
