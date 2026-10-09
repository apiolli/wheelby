import { EyeOff, Eye } from "lucide-react";
import { useState } from "react";
import { FloatingField, type FieldProps } from "./FloatingField";
import { Button } from "@/components/ui/button";

export const PasswordField = ({
  label = "Contraseña",
  ...rest
}: Omit<FieldProps, "label" | "type" | "trailing"> & { label?: string }) => {
  const [shown, setShown] = useState(false);
  return (
    <FloatingField
      label={label}
      type={shown ? "text" : "password"}
      trailing={
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="absolute top-1/2 right-1.5 -translate-y-1/2 [&_svg]:size-4.75"
          aria-label={shown ? "Ocultar contraseña" : "Mostrar contraseña"}
          aria-pressed={shown}
          disabled={rest.disabled}
          onClick={() => setShown((s) => !s)}
        >
          {shown ? <EyeOff strokeWidth={1.7} /> : <Eye strokeWidth={1.7} />}
        </Button>
      }
      {...rest}
    />
  );
};
