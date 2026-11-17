import { useState, type FormEvent } from "react";
import { CircleAlert } from "lucide-react";

import { Alert, AlertDescription } from "@/components/ui/alert";
import * as authApi from "@/api/auth.api";
import { toApiError } from "@/api/errors";
import PasswordStrength from "@/components/custom/PasswordStrength";
import {
  emailSchema,
  fieldErrors,
  RESET_CODE_LENGTH,
  resetSchema,
} from "@/lib/schemas";
import AuthPanel from "../../shared/components/AuthPanel";
import { SubmitButton } from "../../shared/components/SubmitButton";
import MailSent from "../../shared/components/MailSent";
import { useAuthMotion } from "../../shared/motion.context";
import { FloatingField } from "../../shared/components/FloatingField";
import { ModalFoot } from "../../shared/components/ModalFoot";
import { PasswordField } from "../../shared/components/PasswordField";
import { TextLink } from "../../shared/components/TextLink";

type Step = "request" | "sent" | "reset";

const CODE_LENGTH = RESET_CODE_LENGTH;

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
                  await authApi.forgotPassword(email).catch(() => {});
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
  const [error, setError] = useState<string | null>(null);
  const check = emailSchema.safeParse(email);
  const emailError =
    touched && !check.success ? check.error.issues[0].message : undefined;

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setTouched(true);
    setError(null);
    if (!check.success) return;
    setLoading(true);
    try {
      // RF-CA-09: la respuesta es la misma exista o no la cuenta.
      await authApi.forgotPassword(email);
      setLoading(false);
      onSent();
    } catch (err) {
      setLoading(false);
      setError(toApiError(err).message);
    }
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
        {error && (
          <Alert variant="destructive" className="mb-4">
            <CircleAlert />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        <FloatingField
          id="recover-email"
          label="Correo electrónico"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(e) => onEmail(e.target.value)}
          invalid={Boolean(emailError)}
          hint={emailError}
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
  const [error, setError] = useState<string | null>(null);

  const values = { code, password, confirm };
  const invalid = touched ? fieldErrors(resetSchema, values) : {};

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setTouched(true);
    if (!resetSchema.safeParse(values).success) return;
    setInvalidCode(false);
    setError(null);
    setLoading(true);
    try {
      await authApi.resetPassword(email, code, password);
      setLoading(false);
      onDone();
    } catch (err) {
      const { status, message } = toApiError(err);
      setLoading(false);
      // 400: el código no coincide, venció o ya se usó.
      if (status === 400) setInvalidCode(true);
      else setError(message);
    }
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
        ) : error ? (
          <Alert variant="destructive" className="mb-4">
            <CircleAlert />
            <AlertDescription>{error}</AlertDescription>
          </Alert>
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
          invalid={Boolean(invalid.code)}
          hint={invalid.code}
          autoFocus
        />
        <PasswordField
          id="recover-password"
          label="Nueva contraseña"
          autoComplete="new-password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          invalid={Boolean(invalid.password)}
        />
        <PasswordStrength password={password} />
        <PasswordField
          id="recover-confirm"
          label="Confirma la contraseña"
          autoComplete="new-password"
          value={confirm}
          onChange={(e) => setConfirm(e.target.value)}
          invalid={Boolean(invalid.confirm)}
          hint={invalid.confirm}
        />

        <SubmitButton morph loading={loading} className="mt-2">
          Restablecer contraseña
        </SubmitButton>
      </form>
    </AuthPanel>
  );
}
