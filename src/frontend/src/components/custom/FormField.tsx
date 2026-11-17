import type { ReactNode } from "react";
import { cn } from "cn";

interface FormFieldProps {
  id: string;
  label: string;
  // Texto de ayuda; el error lo reemplaza.
  hint?: ReactNode;
  error?: string;
  optional?: boolean;
  // Control (input, textarea…) con el id dado.
  children: ReactNode;
  className?: string;
}

// Campo de formulario operativo: etiqueta arriba, control y una línea de ayuda o error. El
// control recibe aria-describedby={`${id}-hint`} desde quien lo usa.
export default function FormField({
  id,
  label,
  hint,
  error,
  optional,
  children,
  className,
}: FormFieldProps) {
  return (
    <div className={cn("grid content-start gap-1.5", className)}>
      <label htmlFor={id} className="text-[13.5px] font-semibold">
        {label}
        {optional && (
          <span className="font-normal text-muted-foreground"> (opcional)</span>
        )}
      </label>
      {children}
      <p
        id={`${id}-hint`}
        className={cn(
          "min-h-4.5 text-[12.5px]",
          error ? "text-destructive" : "text-muted-foreground",
        )}
      >
        {error ?? hint}
      </p>
    </div>
  );
}
