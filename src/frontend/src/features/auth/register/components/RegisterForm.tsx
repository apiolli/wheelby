import { useState, type FormEvent } from "react";
import { CircleAlert } from "lucide-react";

import * as authApi from "@/api/auth.api";
import { errorMessage } from "@/api/errors";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Checkbox } from "@/components/ui/checkbox";
import { fieldErrors, registerSchema } from "@/lib/schemas";
import { cn } from "cn";
import AuthPanel from "../../shared/components/AuthPanel";
import { SubmitButton } from "../../shared/components/SubmitButton";
import MailSent from "../../shared/components/MailSent";
import { useAuthMotion } from "../../shared/motion.context";
import PasswordStrength from "@/components/custom/PasswordStrength";
import { FloatingField } from "../../shared/components/FloatingField";
import { ModalFoot } from "../../shared/components/ModalFoot";
import { PasswordField } from "../../shared/components/PasswordField";
import { TextLink } from "../../shared/components/TextLink";

interface RegisterFormProps {
  onSwitch: (email?: string) => void;
  onToast: (msg: string) => void;
}

export default function RegisterForm({ onSwitch, onToast }: RegisterFormProps) {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [accepted, setAccepted] = useState(false);
  const [touched, setTouched] = useState(false);
  const [loading, setLoading] = useState(false);
  const [sentTo, setSentTo] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const { swap } = useAuthMotion();

  const values = { name, email, password, accepted };
  const invalid = touched ? fieldErrors(registerSchema, values) : {};

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setTouched(true);
    setError(null);
    if (!registerSchema.safeParse(values).success) return;
    setLoading(true);
    try {
      await authApi.register(name, email, password);
      swap(() => {
        setLoading(false);
        setSentTo(email.trim());
      });
    } catch (err) {
      setLoading(false);
      setError(errorMessage(err));
    }
  };

  const resend = async () => {
    if (!sentTo) return;
    await authApi.resendActivation(sentTo).catch(() => {});
    onToast("Si tu cuenta sigue pendiente, te enviamos un enlace nuevo");
  };

  // Estado posterior al envío: mismo modal, mismo espacio.
  if (sentTo) {
    return (
      <AuthPanel title="Confirma tu correo">
        <MailSent
          title="Revisa tu correo para activar tu cuenta"
          note="Tu cuenta quedará inactiva hasta que abras el enlace. Vence en 24 horas; si no lo ves, revisa la carpeta de spam."
          action="Ir a iniciar sesión"
          onAction={() => onSwitch(sentTo)}
          footer={
            <>
              ¿No te llegó?{" "}
              <TextLink onClick={resend}>Reenviar correo</TextLink>
            </>
          }
        >
          Enviamos un enlace de activación a{" "}
          <strong className="font-semibold break-all text-foreground">
            {sentTo}
          </strong>
          .
        </MailSent>
      </AuthPanel>
    );
  }

  return (
    <AuthPanel title="Crea tu cuenta" greeting="Únete a Wheelby">
      <form onSubmit={submit} noValidate>
        {error && (
          <Alert variant="destructive" className="mb-4">
            <CircleAlert />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        <FloatingField
          id="reg-name"
          label="Nombre completo"
          autoComplete="name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          invalid={Boolean(invalid.name)}
          hint={invalid.name}
        />
        <FloatingField
          id="reg-email"
          label="Correo electrónico"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          invalid={Boolean(invalid.email)}
          hint={invalid.email}
        />
        <PasswordField
          id="reg-password"
          autoComplete="new-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          invalid={Boolean(invalid.password)}
        />

        <PasswordStrength password={password} />

        <div className="mb-5 flex items-start gap-2.5">
          <Checkbox
            id="reg-terms"
            className="mt-px"
            checked={accepted}
            onCheckedChange={(v) => setAccepted(v === true)}
            aria-invalid={Boolean(invalid.accepted) || undefined}
          />
          <label
            htmlFor="reg-terms"
            className={cn(
              "cursor-pointer text-[13px] leading-snug text-muted-foreground",
              invalid.accepted && "text-destructive",
            )}
          >
            Acepto los{" "}
            {/* Pestaña nueva: así no se pierde lo escrito en el formulario. */}
            <a
              href="/terminos"
              target="_blank"
              rel="noopener"
              className="font-semibold text-primary underline underline-offset-2"
            >
              Términos
            </a>{" "}
            y la{" "}
            <a
              href="/privacidad"
              target="_blank"
              rel="noopener"
              className="font-semibold text-primary underline underline-offset-2"
            >
              Política de Privacidad
            </a>
          </label>
        </div>

        <SubmitButton morph loading={loading}>
          Crear cuenta
        </SubmitButton>

        <ModalFoot>
          ¿Ya tienes cuenta?{" "}
          <TextLink onClick={() => onSwitch()}>Inicia sesión</TextLink>
        </ModalFoot>
      </form>
    </AuthPanel>
  );
}
