import { Input } from "@/components/ui/input";
import { prefersReducedMotion } from "@/lib/gsap";
import { useGSAP } from "@gsap/react";
import { cn } from "cn";
import gsap from "gsap";
import { useRef, type ComponentProps, type ReactNode } from "react";

export type FieldProps = Omit<ComponentProps<"input">, "placeholder"> & {
  id: string;
  label: string;
  invalid?: boolean;
  hint?: ReactNode;
  trailing?: ReactNode;
};

// Campo con label flotante estilo Airbnb: el placeholder sube al enfocar o al tener valor.
export function FloatingField({
  id,
  label,
  invalid,
  hint,
  trailing,
  className,
  onFocus,
  onBlur,
  ...rest
}: FieldProps) {
  const hintId = hint ? `${id}-hint` : undefined;
  const inputRef = useRef<HTMLInputElement>(null);
  const lineRef = useRef<HTMLSpanElement>(null);

  // Línea inferior en petróleo que se ilumina de izquierda a derecha al enfocar (200 ms). Arranca
  // apagada salvo que el campo ya tenga el foco (autoFocus).
  const { contextSafe } = useGSAP(
    () => {
      gsap.set(lineRef.current, {
        scaleX: document.activeElement === inputRef.current ? 1 : 0,
      });
    },
    { scope: lineRef },
  );
  const light = (on: boolean) =>
    contextSafe(() => {
      if (prefersReducedMotion()) {
        gsap.set(lineRef.current, { scaleX: 1 });
        gsap.to(lineRef.current, {
          opacity: on ? 1 : 0,
          duration: 0.15,
          ease: "none",
          overwrite: true,
        });
        return;
      }
      gsap.to(lineRef.current, {
        scaleX: on ? 1 : 0,
        opacity: 1,
        duration: 0.2,
        ease: "power2.out",
        overwrite: true,
      });
    })();

  return (
    <div className="mb-3">
      <div className="relative">
        <Input
          ref={inputRef}
          id={id}
          placeholder=" "
          aria-invalid={invalid || undefined}
          aria-describedby={hintId}
          className={cn(
            "peer h-14 pt-5 pb-1.5",
            trailing && "pr-13",
            className,
          )}
          onFocus={(e) => {
            light(true);
            onFocus?.(e);
          }}
          onBlur={(e) => {
            light(false);
            onBlur?.(e);
          }}
          {...rest}
        />
        <span
          ref={lineRef}
          aria-hidden="true"
          className="pointer-events-none absolute inset-x-3 bottom-0 h-0.5 origin-left rounded-full bg-primary"
        />
        <label
          htmlFor={id}
          className={cn(
            "pointer-events-none absolute top-1/2 left-3.75 -translate-y-1/2 text-[15px] text-muted-foreground transition-all duration-150",
            "peer-focus:top-2.5 peer-focus:translate-y-0 peer-focus:text-xs",
            "peer-not-placeholder-shown:top-2.5 peer-not-placeholder-shown:translate-y-0 peer-not-placeholder-shown:text-xs",
            "peer-disabled:opacity-50",
          )}
        >
          {label}
        </label>
        {trailing}
      </div>
      {invalid && hint ? (
        <p id={hintId} className="mt-1.5 text-[12.5px] text-destructive">
          {hint}
        </p>
      ) : null}
    </div>
  );
}
