import { CircleAlert } from "lucide-react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import type { AuthNotice } from "@/types/types";
import AuthPanel from "../../shared/components/AuthPanel";
import MailSent from "../../shared/components/MailSent";
import { loginErrorMessage, useLogin } from "../hooks/useLogin";
import { Notice } from "./Notice";
import { FloatingField } from "../../shared/components/FloatingField";
import { ModalFoot } from "../../shared/components/ModalFoot";
import { PasswordField } from "../../shared/components/PasswordField";
import { SubmitButton } from "../../shared/components/SubmitButton";
import { TextLink } from "../../shared/components/TextLink";

export const LOGIN_NOTICE_ID = "login-notice";

interface Props {
  email?: string;
  notice?: AuthNotice;
  onSwitch: () => void;
  onRecover: (email: string) => void;
}

export const LoginForm = ({
  email: initialEmail = "",
  notice,
  onSwitch,
  onRecover,
}: Props) => {
  const {
    resend,
    resent,
    submit,
    invalid,
    error,
    errorRef,
    resending,
    setPassword,
    swap,
    setError,
    setResent,
    email,
    setEmail,
    loading,
    locked,
    password,
  } = useLogin(initialEmail);

  // Reenvío de activación: mismo estado de confirmación que el registro, en el mismo modal.
  if (resent) {
    return (
      <AuthPanel title="Confirma tu correo">
        <MailSent
          title="Revisa tu correo para activar tu cuenta"
          note="Tu cuenta quedará inactiva hasta que abras el enlace. Vence en 24 horas; si no lo ves, revisa la carpeta de spam."
          action="Volver a iniciar sesión"
          onAction={() =>
            swap(() => {
              setResent(false);
              setError(null);
              setPassword("");
            }, "back")
          }
        >
          Si tu cuenta está pendiente de activación, enviamos un enlace nuevo a{" "}
          <strong className="font-semibold break-all text-foreground">
            {email.trim()}
          </strong>
          .
        </MailSent>
      </AuthPanel>
    );
  }

  return (
    <AuthPanel
      title="Inicia sesión"
      greeting="Te damos la bienvenida a Wheelby"
    >
      <form onSubmit={submit} noValidate>
        {notice && !error && <Notice notice={notice} />}
        {error && (
          <div className="mb-4">
            <Alert
              ref={errorRef}
              tabIndex={-1}
              variant="destructive"
              className="outline-none"
            >
              <CircleAlert />
              <AlertDescription>{loginErrorMessage(error)}</AlertDescription>
            </Alert>
            {error.kind === "inactive" && (
              <p className="mt-2.5 text-[13.5px]">
                <TextLink
                  onClick={resend}
                  disabled={resending}
                  aria-busy={resending}
                >
                  {resending ? "Enviando…" : "Reenviar correo de activación"}
                </TextLink>
              </p>
            )}
          </div>
        )}

        <FloatingField
          id="login-email"
          label="Correo electrónico"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          disabled={locked}
          invalid={Boolean(invalid.email)}
          hint={invalid.email}
          required
        />
        <PasswordField
          id="login-password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          disabled={locked}
          invalid={Boolean(invalid.password)}
          hint={invalid.password}
          required
        />
        <div className="-mt-0.5 mb-5 flex justify-end">
          <button
            type="button"
            className="cursor-pointer text-[13px] font-semibold underline underline-offset-2"
            onClick={() => onRecover(email)}
          >
            ¿Olvidaste tu contraseña?
          </button>
        </div>

        <SubmitButton morph loading={loading} disabled={locked}>
          Continuar
        </SubmitButton>

        <ModalFoot>
          ¿No tienes cuenta? <TextLink onClick={onSwitch}>Regístrate</TextLink>
        </ModalFoot>
      </form>
    </AuthPanel>
  );
};
