import { useState, type ReactElement, type ReactNode } from "react";

import { errorMessage } from "@/api/errors";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from "@/components/ui/alert-dialog";
import { showToast } from "@/stores/session.store";

interface ConfirmActionProps {
  trigger: ReactElement;
  title: string;
  description: ReactNode;
  confirmLabel: string;
  destructive?: boolean;
  // Contenido extra entre la descripción y los botones (p. ej. el motivo de un rechazo).
  children?: ReactNode;
  confirmDisabled?: boolean;
  // Si la acción falla, se muestra el mensaje de la API y el diálogo sigue abierto.
  onConfirm: () => Promise<unknown>;
  onOpenChange?: (open: boolean) => void;
}

// Toda acción sensible pasa por aquí: nada se ejecuta sin confirmar.
export default function ConfirmAction({
  trigger,
  title,
  description,
  confirmLabel,
  destructive,
  children,
  confirmDisabled,
  onConfirm,
  onOpenChange,
}: ConfirmActionProps) {
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);

  const change = (next: boolean) => {
    if (busy) return;
    setOpen(next);
    onOpenChange?.(next);
  };

  const confirm = async () => {
    setBusy(true);
    try {
      await onConfirm();
      setBusy(false);
      setOpen(false);
      onOpenChange?.(false);
    } catch (err) {
      setBusy(false);
      showToast(errorMessage(err));
    }
  };

  return (
    <AlertDialog open={open} onOpenChange={change}>
      <AlertDialogTrigger render={trigger} />
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        {children}
        <AlertDialogFooter>
          <AlertDialogCancel disabled={busy}>Cancelar</AlertDialogCancel>
          <AlertDialogAction
            variant={destructive ? "destructive" : "default"}
            disabled={busy || confirmDisabled}
            aria-busy={busy}
            onClick={confirm}
          >
            {busy ? "Un momento…" : confirmLabel}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
