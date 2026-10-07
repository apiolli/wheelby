import { Alert, AlertDescription } from "@/components/ui/alert";
import type { AuthNotice } from "@/types/types";
import { CircleCheck, Info, type LucideIcon } from "lucide-react";
import { LOGIN_NOTICE_ID } from "../LoginPage";

// Avisos informativos: estilo neutro, no rojo.
const NOTICES: Record<AuthNotice, { icon: LucideIcon; text: string }> = {
  expired: { icon: Info, text: "Tu sesión expiró. Inicia sesión de nuevo." },
  "password-reset": {
    icon: CircleCheck,
    text: "Tu contraseña se actualizó. Inicia sesión con la nueva.",
  },
};

export const Notice = ({ notice }: { notice: AuthNotice }) => {
  const { icon: Icon, text } = NOTICES[notice];
  return (
    <Alert id={LOGIN_NOTICE_ID} role="status" className="mb-4">
      <Icon />
      <AlertDescription>{text}</AlertDescription>
    </Alert>
  );
};
