import {
  useEffect,
  useEffectEvent,
  useRef,
  useState,
  type FormEvent,
} from "react";
import { CircleAlert } from "lucide-react";

import { Alert, AlertDescription } from "@/components/ui/alert";
import AuthPanel from "../components/AuthPanel";
import {
  FloatingField,
  ModalFoot,
  PasswordField,
  SocialButtons,
  SubmitButton,
  TextLink,
} from "../components/FormFields";
import MailSent from "../components/MailSent";
import type { AuthNotice } from "@/types/types";
import { useSession } from "@/stores/session.store";
import { useAuthMotion } from "../motion.context";
import { login, resendActivation } from "@/mock/auth";
import { DEMO, DEMO_ADMIN, DEMO_INACTIVE } from "@/mock/demo";
import { Notice } from "./components/Notice";
import { DemoCode } from "./components/DemoCode";

type LoginError =
  | { kind: "invalid" }
  | { kind: "inactive" }
  | { kind: "locked"; until: number };

// Mensajes discretos: el de credenciales no dice qué dato falló y el de bloqueo no da el
// número de intentos ni el tiempo exacto restante.
const ERRORS: Record<LoginError["kind"], string> = {
  invalid:
    "El correo o la contraseña no son correctos. Revisa tus datos e inténtalo de nuevo.",
  inactive: "Tu cuenta aún no está activa. Revisa tu correo para activarla.",
  locked: "Demasiados intentos. Intenta de nuevo en unos minutos.",
};

export const LOGIN_NOTICE_ID = "login-notice";

interface LoginFormProps {
  email?: string;
  notice?: AuthNotice;
  onSwitch: () => void;
  onRecover: (email: string) => void;
  onToast: (msg: string) => void;
}

export default function LoginForm({
  email: initialEmail = "",
  notice,
  onSwitch,
  onRecover,
  onToast,
}: LoginFormProps) {
  const [email, setEmail] = useState(initialEmail);
  const [password, setPassword] = useState("");
  const [error, setError] = useState<LoginError | null>(null);
  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);
  const [resent, setResent] = useState(false);
  const errorRef = useRef<HTMLDivElement>(null);
  const { swap, shake, stopScene, resumeScene, celebrate } = useAuthMotion();
  const { signIn } = useSession();

  const lockedUntil = error?.kind === "locked" ? error.until : null;
  // La escena del panel no forma parte del estado del formulario: se avisa sin ser dependencia.
  const pauseScene = useEffectEvent(() => stopScene());
  const continueScene = useEffectEvent(() => resumeScene());
  const locked = lockedUntil !== null;

  useEffect(() => {
    if (lockedUntil === null) return;
    // El botón enfocado queda deshabilitado: llevamos el foco al aviso para no perderlo.
    errorRef.current?.focus();
    // El vehículo de la escena se detiene mientras dure el bloqueo.
    pauseScene();
    // Al vencer el bloqueo se reactiva el formulario.
    const t = setTimeout(() => setError(null), lockedUntil - Date.now());
    return () => {
      clearTimeout(t);
      continueScene();
    };
  }, [lockedUntil]);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);
    const res = await login(email, password);
    if (res.ok) {
      // GET /auth/me valida el token mientras corre la animación de salida; la sesión (y con ella
      // la navegación al catálogo) solo empieza cuando ambas terminan.
      if (await signIn(res.token, celebrate())) return;
      setLoading(false);
      setError({ kind: "invalid" });
      return;
    }
    setLoading(false);
    setError(
      res.reason === "locked"
        ? { kind: "locked", until: res.until }
        : { kind: res.reason },
    );
    // Solo el error de credenciales sacude el modal; el bloqueo no es un error de tecleo.
    if (res.reason === "invalid") shake();
  };

  const resend = async () => {
    setResending(true);
    await resendActivation(email);
    swap(() => {
      setResending(false);
      setResent(true);
    });
  };

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
              <AlertDescription>{ERRORS[error.kind]}</AlertDescription>
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

        <SocialButtons onSocial={(p) => onToast(`Continuar con ${p} (demo)`)} />

        <FloatingField
          id="login-email"
          label="Correo electrónico"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          disabled={locked}
          required
        />
        <PasswordField
          id="login-password"
          autoComplete="current-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          disabled={locked}
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

        <div className="mt-3.5 grid gap-1 text-center text-xs text-muted-foreground">
          <p>
            Demo: <DemoCode>{DEMO.email}</DemoCode> ·{" "}
            <DemoCode>{DEMO.password}</DemoCode>
          </p>
          <p>
            Admin: <DemoCode>{DEMO_ADMIN.email}</DemoCode> ·{" "}
            <DemoCode>{DEMO_ADMIN.password}</DemoCode>
          </p>
          <p>
            Sin activar: <DemoCode>{DEMO_INACTIVE.email}</DemoCode>
          </p>
        </div>

        <ModalFoot>
          ¿No tienes cuenta? <TextLink onClick={onSwitch}>Regístrate</TextLink>
        </ModalFoot>
      </form>
    </AuthPanel>
  );
}
