import { useEffect, useEffectEvent, useRef, type ReactNode } from "react";
import { Clock } from "lucide-react";

import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { useAuthMotion } from "../motion.context";
import MailIllustration from "./MailIllustration";

interface MailSentProps {
  title: string;
  children: ReactNode;
  note: ReactNode;
  action: string;
  onAction: () => void;
  footer?: ReactNode;
}

// Estado "revisa tu correo": reemplaza al formulario en la misma página y el mismo espacio.
// Lo usan el registro, el reenvío de activación y la recuperación de contraseña. La entrada la
// anima AuthLayout (swap), por eso no lleva clases animate-* propias.
export default function MailSent({
  title,
  children,
  note,
  action,
  onAction,
  footer,
}: MailSentProps) {
  const titleRef = useRef<HTMLHeadingElement>(null);
  const { mailSent } = useAuthMotion();
  const flyEnvelope = useEffectEvent(() => mailSent());

  // El botón que lanzó el envío desaparece; llevamos el foco al título para no perderlo. A la vez,
  // el panel de marca dibuja un sobre que sale volando.
  useEffect(() => {
    titleRef.current?.focus();
    flyEnvelope();
  }, []);

  return (
    <div className="px-1 pt-2 pb-1 text-center" role="status">
      <MailIllustration className="mx-auto mt-1 mb-5 w-42" />
      <h3
        ref={titleRef}
        tabIndex={-1}
        className="mb-2 text-[22px] leading-tight font-bold tracking-tight outline-none"
      >
        {title}
      </h3>
      <p className="mx-auto max-w-75 text-[14.5px] text-muted-foreground">
        {children}
      </p>
      <Alert
        // variant="highlight"
        variant={"default"}
        role="note"
        className="my-5 text-left text-[13px]"
      >
        <Clock />
        <AlertDescription>{note}</AlertDescription>
      </Alert>
      <Button
        size="lg"
        className="h-12.5 w-full text-[15.5px] font-bold"
        onClick={onAction}
      >
        {action}
      </Button>
      {footer && (
        <p className="mt-3.5 text-[13.5px] text-muted-foreground">{footer}</p>
      )}
    </div>
  );
}
