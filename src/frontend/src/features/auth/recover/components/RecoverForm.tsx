import { useState, type FormEvent } from "react";
import { CircleAlert } from "lucide-react";

import { Alert, AlertDescription } from "@/components/ui/alert";
import { EMAIL_RE, passwordChecks } from "@/lib/password";
import PasswordStrength from "@/components/custom/PasswordStrength";
import { requestPasswordReset, resetPassword } from "@/mock/auth";
import { DEMO_RESET_CODE } from "@/mock/demo";
import AuthPanel from "../../shared/components/AuthPanel";
import {
  FloatingField,
  ModalFoot,
  PasswordField,
  SubmitButton,
  TextLink,
} from "../../shared/components/FormFields";
import MailSent from "../../shared/components/MailSent";
import { useAuthMotion } from "../../shared/motion.context";

type Step = "request" | "sent" | "reset";

const CODE_LENGTH = 6;

interface RecoverFormProps {
  email?: string;
  onBack: () => void;
  onDone: (email: string) => void;
  onToast: (msg: string) => void;
}

// Recuperación de contraseña en tres pasos, dentro del mismo modal que el login.
export default function RecoverForm({
  email: initialEmail = "",
  onBack,
  onDone,
  onToast,
}: RecoverFormProps) {
  const [step, setStep] = useState<Step>("request");
  const [email, setEmail] = useState(initialEmail);
  const { swap } = useAuthMotion();
  const goTo = (next: Step, direction: "forward" | "back" = "forward") =>
    swap(() => setStep(next), direction);

  if (step === "sent") {
    return (
      <AuthPanel title="Recupera tu contraseña">
        <MailSent
          title="Revisa tu correo"
          note="El código vence en 30 minutos. Si no lo ves, revisa la carpeta de spam."
          action="Ingresar código"
          onAction={() => goTo("reset")}
          footer={
            <>
              ¿No te llegó?{" "}
              <TextLink
                onClick={async () => {
                  await requestPasswordReset(email);
                  onToast("Si el correo existe, te enviamos un código nuevo");
                }}
              >
                Reenviar código
              </TextLink>
            </>
          }
        >
          {/* RF-CA-09: no confirma ni niega que el correo exista. */}
          Si el correo existe en nuestro sistema, te enviamos un código.
        </MailSent>
      </AuthPanel>
    );
  }

  if (step === "reset") {
    return (
      <ResetStep
        email={email}
        onRestart={() => goTo("request", "back")}
        onDone={() => onDone(email)}
      />
    );
  }

  return (
    <RequestStep
      email={email}
      onEmail={setEmail}
      onSent={() => goTo("sent")}
      onBack={onBack}
    />
  );
}

interface RequestStepProps {
  email: string;
  onEmail: (email: string) => void;
  onSent: () => void;
  onBack: () => void;
}

function RequestStep({ email, onEmail, onSent, onBack }: RequestStepProps) {
  const [touched, setTouched] = useState(false);
  const [loading, setLoading] = useState(false);
  const valid = EMAIL_RE.test(email.trim());

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setTouched(true);
    if (!valid) return;
    setLoading(true);
    await requestPasswordReset(email);
    setLoading(false);
    onSent();
  };

  return (
    <AuthPanel
      title="Recupera tu contraseña"
      greeting="¿Olvidaste tu contraseña?"
    >
      <form onSubmit={submit} noValidate>
        <p className="-mt-2 mb-5 text-[14.5px] text-muted-foreground">
          Te enviaremos un código para restablecerla.
        </p>
        <FloatingField
          id="recover-email"
          label="Correo electrónico"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(e) => onEmail(e.target.value)}
          invalid={touched && !valid}
          hint="Escribe un correo válido."
          autoFocus
        />
        <SubmitButton morph loading={loading} className="mt-2">
          Enviar código
        </SubmitButton>
        <ModalFoot>
          <TextLink onClick={onBack}>Volver a iniciar sesión</TextLink>
        </ModalFoot>
      </form>
    </AuthPanel>
  );
}

interface ResetStepProps {
  email: string;
  onRestart: () => void;
  onDone: () => void;
}

function ResetStep({ email, onRestart, onDone }: ResetStepProps) {
  const [code, setCode] = useState("");
  const [password, setPassword] = useState("");
  const [confirm, setConfirm] = useState("");
  const [touched, setTouched] = useState(false);
  const [loading, setLoading] = useState(false);
  const [invalidCode, setInvalidCode] = useState(false);

  const valid = {
    code: code.length === CODE_LENGTH,
    password: passwordChecks(password).every(Boolean),
    confirm: confirm.length > 0 && confirm === password,
  };

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setTouched(true);
    if (!Object.values(valid).every(Boolean)) return;
    setInvalidCode(false);
    setLoading(true);
    const ok = await resetPassword(email, code, password);
    setLoading(false);
    if (ok) onDone();
    else setInvalidCode(true);
  };

  return (
    <AuthPanel title="Restablece tu contraseña">
      <form onSubmit={submit} noValidate>
        {invalidCode ? (
          <div className="mb-4">
            <Alert variant="destructive">
              <CircleAlert />
              <AlertDescription>
                Este código ya no es válido. Solicita uno nuevo.
              </AlertDescription>
            </Alert>
            <p className="mt-2.5 text-[13.5px]">
              <TextLink onClick={onRestart}>Solicitar un código nuevo</TextLink>
            </p>
          </div>
        ) : (
          <p className="mb-5 text-[14.5px] text-muted-foreground">
            Escribe el código que recibiste por correo y elige una contraseña
            nueva.
          </p>
        )}

        <FloatingField
          id="recover-code"
          label="Código"
          inputMode="numeric"
          autoComplete="one-time-code"
          maxLength={CODE_LENGTH}
          className="font-semibold tracking-[0.3em]"
          value={code}
          onChange={(e) =>
            setCode(e.target.value.replace(/\D/g, "").slice(0, CODE_LENGTH))
          }
          invalid={touched && !valid.code}
          hint={`El código tiene ${CODE_LENGTH} dígitos.`}
          autoFocus
        />
        <PasswordField
          id="recover-password"
          label="Nueva contraseña"
          autoComplete="new-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          invalid={touched && !valid.password}
        />
        <PasswordStrength password={password} />
        <PasswordField
          id="recover-confirm"
          label="Confirma la contraseña"
          autoComplete="new-password"
          value={confirm}
          onChange={(e) => setConfirm(e.target.value)}
          invalid={touched && !valid.confirm}
          hint="Las contraseñas no coinciden."
        />

        <SubmitButton morph loading={loading} className="mt-2">
          Restablecer contraseña
        </SubmitButton>

        <p className="mt-3.5 text-center text-xs text-muted-foreground">
          Demo: el código es{" "}
          <code className="rounded bg-secondary px-1.5 py-px text-[11.5px]">
            {DEMO_RESET_CODE}
          </code>
        </p>
      </form>
    </AuthPanel>
  );
}
