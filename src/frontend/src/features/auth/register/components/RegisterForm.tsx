import { useState, type FormEvent } from "react";

import { Checkbox } from "@/components/ui/checkbox";
import { register } from "@/mock/auth";
import { cn } from "cn";
import AuthPanel from "../../shared/components/AuthPanel";
import {
  TextLink,
  FloatingField,
  PasswordField,
  SubmitButton,
  ModalFoot,
} from "../../shared/components/FormFields";
import MailSent from "../../shared/components/MailSent";
import { useAuthMotion } from "../../shared/motion.context";
import { EMAIL_RE, passwordChecks } from "../../../../lib/password";
import PasswordStrength from "@/components/custom/PasswordStrength";

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
  const { swap } = useAuthMotion();

  const valid = {
    name: name.trim().length >= 3,
    email: EMAIL_RE.test(email.trim()),
    password: passwordChecks(password).every(Boolean),
    accepted,
  };
  const canSubmit = Object.values(valid).every(Boolean);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setTouched(true);
    if (!canSubmit) return;
    setLoading(true);
    await register(name, email, password);
    swap(() => {
      setLoading(false);
      setSentTo(email.trim());
    });
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
              <TextLink onClick={() => onToast("Enlace reenviado")}>
                Reenviar correo
              </TextLink>
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
        <FloatingField
          id="reg-name"
          label="Nombre completo"
          autoComplete="name"
          value={name}
          onChange={(e) => setName(e.target.value)}
          invalid={touched && !valid.name}
          hint="Escribe tu nombre y apellido."
        />
        <FloatingField
          id="reg-email"
          label="Correo electrónico"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          invalid={touched && !valid.email}
          hint="Escribe un correo válido."
        />
        <PasswordField
          id="reg-password"
          autoComplete="new-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          invalid={touched && !valid.password}
        />

        <PasswordStrength password={password} />

        <div className="mb-5 flex items-start gap-2.5">
          <Checkbox
            id="reg-terms"
            className="mt-px"
            checked={accepted}
            onCheckedChange={(v) => setAccepted(v === true)}
            aria-invalid={(touched && !accepted) || undefined}
          />
          <label
            htmlFor="reg-terms"
            className={cn(
              "cursor-pointer text-[13px] leading-snug text-muted-foreground",
              touched && !accepted && "text-destructive",
            )}
          >
            Acepto los{" "}
            <a
              href="#"
              className="font-semibold text-primary underline underline-offset-2"
              onClick={(e) => e.preventDefault()}
            >
              Términos
            </a>{" "}
            y la{" "}
            <a
              href="#"
              className="font-semibold text-primary underline underline-offset-2"
              onClick={(e) => e.preventDefault()}
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
