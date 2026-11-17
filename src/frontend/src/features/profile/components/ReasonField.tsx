import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { MAX_REASON, MIN_REASON } from "@/lib/schemas";

interface ReasonFieldProps {
  id: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  optional?: boolean;
  placeholder?: string;
  note?: string;
}

// Motivo de un rechazo o una cancelación, dentro de ConfirmAction.
export default function ReasonField({
  id,
  value,
  onChange,
  error,
  optional,
  placeholder,
  note,
}: ReasonFieldProps) {
  return (
    <div className="grid gap-1.5">
      <Label htmlFor={id} className="text-sm font-semibold">
        Motivo{" "}
        <span className="font-normal text-muted-foreground">
          ({optional ? "opcional" : "obligatorio"})
        </span>
      </Label>
      <Textarea
        id={id}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        aria-invalid={Boolean(error) || undefined}
        aria-describedby={`${id}-hint`}
      />
      <div className="flex justify-between gap-3">
        <p
          id={`${id}-hint`}
          className={
            error
              ? "text-[12.5px] text-destructive"
              : "text-[12.5px] text-muted-foreground"
          }
        >
          {error ??
            note ??
            (optional
              ? "Máximo 500 caracteres."
              : `Escribe al menos ${MIN_REASON} caracteres.`)}
        </p>
        <p
          aria-live="polite"
          className={
            value.length > MAX_REASON
              ? "shrink-0 text-[12.5px] text-destructive tabular-nums"
              : "shrink-0 text-[12.5px] text-muted-foreground tabular-nums"
          }
        >
          {value.length}/{MAX_REASON}
        </p>
      </div>
    </div>
  );
}
