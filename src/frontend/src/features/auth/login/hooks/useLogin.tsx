import { login, resendActivation } from "@/mock/auth";
import { useSession } from "@/stores/session.store";
import {
  useEffect,
  useEffectEvent,
  useRef,
  useState,
  type FormEvent,
} from "react";
import { useAuthMotion } from "../../shared/motion.context";

type LoginError =
  | { kind: "invalid" }
  | { kind: "inactive" }
  | { kind: "locked"; until: number };

const ERRORS: Record<LoginError["kind"], string> = {
  invalid:
    "El correo o la contraseña no son correctos. Revisa tus datos e inténtalo de nuevo.",
  inactive: "Tu cuenta aún no está activa. Revisa tu correo para activarla.",
  locked: "Demasiados intentos. Intenta de nuevo en unos minutos.",
};

export const useLogin = (initialEmail: string) => {
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

  return {
    loading,
    resending,
    locked,
    resent,
    ERRORS,
    errorRef,
    error,
    email,
    password,
    setEmail,
    setPassword,
    resend,
    submit,
    swap,
    setResent,
    setError,
  };
};
