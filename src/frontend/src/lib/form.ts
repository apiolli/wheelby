import { cn } from "cn";

// Clases del input de estos formularios (alto cómodo, esquinas de 12 px, error en rojo).
export const inputClass = (invalid?: boolean) =>
  cn(
    "h-12 w-full rounded-xl border bg-background px-3.5 text-[15px] outline-none placeholder:text-muted-foreground/70",
    "focus-visible:border-foreground focus-visible:ring-1 focus-visible:ring-foreground",
    "disabled:cursor-not-allowed disabled:bg-secondary disabled:text-muted-foreground",
    invalid ? "border-destructive ring-1 ring-destructive" : "border-input",
  );
