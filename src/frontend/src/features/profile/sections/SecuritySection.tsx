import { useState, type FormEvent } from "react";
import { useMutation } from "@tanstack/react-query";
import { CircleAlert, CircleCheck, LogOut } from "lucide-react";

import { changePassword } from "@/api/auth.api";
import { errorMessage } from "@/api/errors";
import PasswordStrength from "@/components/custom/PasswordStrength";
import SectionHeading from "@/components/custom/SectionHeading";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { PasswordField } from "@/features/auth/shared/components/PasswordField";
import { SubmitButton } from "@/features/auth/shared/components/SubmitButton";
import { changePasswordSchema, fieldErrors } from "@/lib/schemas";
import { useSession } from "@/stores/session.store";

const EMPTY = { current: "", password: "", confirm: "" };

// Cambio de contraseña en línea (RF-CA-22), sin modal.
export default function SecuritySection() {
  const logout = useSession((s) => s.logout);
  const replaceToken = useSession((s) => s.replaceToken);
  const [values, setValues] = useState(EMPTY);
  const [touched, setTouched] = useState(false);
  const invalid = touched ? fieldErrors(changePasswordSchema, values) : {};

  const change = useMutation({
    mutationFn: () => changePassword(values.current, values.password),
    onSuccess: (res) => {
      // La API cierra las demás sesiones y entrega un token nuevo para esta.
      replaceToken(res.accessToken, res.expiresAt);
      setTouched(false);
      setValues(EMPTY);
    },
  });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    setTouched(true);
    if (changePasswordSchema.safeParse(values).success) change.mutate();
  };

  const set =
    (key: keyof typeof EMPTY) => (e: React.ChangeEvent<HTMLInputElement>) => {
      if (change.isSuccess || change.isError) change.reset();
      setValues((v) => ({ ...v, [key]: e.target.value }));
    };

  return (
    <>
      <SectionHeading
        title="Seguridad"
        description="Cambia tu contraseña y cierra tu sesión."
      />

      <section
        className="rounded-2xl border border-border p-6 md:p-8"
        aria-labelledby="security-password"
      >
        <h2
          id="security-password"
          className="mb-1 text-lg font-bold tracking-tight"
        >
          Cambiar contraseña
        </h2>
        <p className="mb-5 text-[14px] text-muted-foreground">
          Usa al menos 8 caracteres, con letras y números.
        </p>

        <form onSubmit={submit} noValidate className="max-w-110">
          {change.isError && (
            <Alert variant="destructive" className="mb-4">
              <CircleAlert />
              <AlertDescription>{errorMessage(change.error)}</AlertDescription>
            </Alert>
          )}
          {change.isSuccess && (
            <Alert variant="success" role="status" className="mb-4">
              <CircleCheck />
              <AlertDescription>
                Tu contraseña se actualizó. Úsala la próxima vez que inicies
                sesión.
              </AlertDescription>
            </Alert>
          )}

          <PasswordField
            id="security-current"
            label="Contraseña actual"
            autoComplete="current-password"
            value={values.current}
            onChange={set("current")}
            invalid={Boolean(invalid.current) || change.isError}
            hint={invalid.current}
          />
          <PasswordField
            id="security-new"
            label="Nueva contraseña"
            autoComplete="new-password"
            value={values.password}
            onChange={set("password")}
            invalid={Boolean(invalid.password)}
            hint={
              invalid.password === "Debe ser distinta de la actual."
                ? invalid.password
                : undefined
            }
          />
          <PasswordStrength password={values.password} />
          <PasswordField
            id="security-confirm"
            label="Confirma la nueva contraseña"
            autoComplete="new-password"
            value={values.confirm}
            onChange={set("confirm")}
            invalid={Boolean(invalid.confirm)}
            hint={invalid.confirm}
          />

          <SubmitButton
            loading={change.isPending}
            className="mt-2 sm:w-auto sm:px-8"
          >
            Actualizar contraseña
          </SubmitButton>
        </form>
      </section>

      <section className="mt-8 flex flex-col gap-4 border-t border-border-soft pt-8 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 className="text-lg font-bold tracking-tight">Cerrar sesión</h2>
          <p className="text-[14px] text-muted-foreground">
            Sal de tu cuenta en este dispositivo.
          </p>
        </div>
        <Button
          variant="outline"
          size="lg"
          className="shrink-0"
          onClick={logout}
        >
          <LogOut />
          Cerrar sesión
        </Button>
      </section>
    </>
  );
}
