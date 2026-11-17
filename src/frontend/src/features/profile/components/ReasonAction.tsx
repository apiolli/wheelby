import { useState, type ReactElement } from "react";

import ConfirmAction from "@/components/custom/ConfirmAction";
import { reasonSchema } from "@/lib/schemas";
import ReasonField from "./ReasonField";

interface ReasonActionProps {
  id: string;
  trigger: ReactElement;
  title: string;
  description: string;
  confirmLabel: string;
  optional?: boolean;
  note?: string;
  onConfirm: (reason: string) => Promise<unknown>;
}

// Acción destructiva que pide un motivo (validado con Zod) antes de confirmarse.
export default function ReasonAction({
  id,
  trigger,
  title,
  description,
  confirmLabel,
  optional,
  note,
  onConfirm,
}: ReasonActionProps) {
  const [reason, setReason] = useState("");
  const [touched, setTouched] = useState(false);
  const check = reasonSchema.safeParse(reason);
  const valid = check.success || (optional && reason.trim().length === 0);
  const error =
    touched && !valid && !check.success
      ? check.error.issues[0].message
      : undefined;

  return (
    <ConfirmAction
      trigger={trigger}
      title={title}
      description={description}
      confirmLabel={confirmLabel}
      destructive
      confirmDisabled={!valid}
      onOpenChange={(open) => {
        if (!open) {
          setReason("");
          setTouched(false);
        }
      }}
      onConfirm={() => onConfirm(reason.trim())}
    >
      <ReasonField
        id={id}
        value={reason}
        onChange={(v) => {
          setReason(v);
          setTouched(true);
        }}
        error={error}
        optional={optional}
        note={note}
      />
    </ConfirmAction>
  );
}
