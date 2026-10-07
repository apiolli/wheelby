import { useRef, useState, type ComponentProps, type ReactNode } from "react";
import { Eye, EyeOff, LoaderCircle } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Separator } from "@/components/ui/separator";
import { gsap, prefersReducedMotion, useGSAP } from "@/lib/gsap";
import { cn } from "@/lib/utils";
import { AppleLogo, GoogleLogo } from "./BrandLogos";

type FieldProps = Omit<ComponentProps<"input">, "placeholder"> & {
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

export function PasswordField({
  label = "Contraseña",
  ...rest
}: Omit<FieldProps, "label" | "type" | "trailing"> & { label?: string }) {
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
}

export function SocialButtons({
  onSocial,
}: {
  onSocial: (provider: "Google" | "Apple") => void;
}) {
  return (
    <>
      <div className="grid gap-2.5">
        <Button
          type="button"
          variant="outline"
          size="lg"
          className="relative w-full"
          onClick={() => onSocial("Google")}
        >
          <GoogleLogo className="absolute left-5 size-5" />
          Continuar con Google
        </Button>
        <Button
          type="button"
          variant="outline"
          size="lg"
          className="relative w-full"
          onClick={() => onSocial("Apple")}
        >
          <AppleLogo className="absolute left-5 size-5" />
          Continuar con Apple
        </Button>
      </div>
      <div className="my-5 flex items-center gap-3.5 text-[12.5px] text-muted-foreground">
        <Separator className="flex-1" />o<Separator className="flex-1" />
      </div>
    </>
  );
}

type SubmitButtonProps = ComponentProps<typeof Button> & {
  loading: boolean;
  // Páginas de acceso: al enviar, el botón se encoge a un círculo con el indicador de carga.
  morph?: boolean;
};

export function SubmitButton({
  loading,
  morph,
  disabled,
  className,
  children,
  ...rest
}: SubmitButtonProps) {
  const ref = useRef<HTMLButtonElement>(null);
  const fullWidth = useRef(0);

  useGSAP(
    () => {
      const button = ref.current;
      if (!button || !morph) return;
      const duration = prefersReducedMotion() ? 0 : 0.25;
      if (loading) {
        fullWidth.current = button.offsetWidth;
        gsap.fromTo(
          button,
          { width: fullWidth.current },
          { width: button.offsetHeight, duration, ease: "power2.inOut" },
        );
      } else if (fullWidth.current) {
        gsap.fromTo(
          button,
          { width: button.offsetWidth },
          {
            width: fullWidth.current,
            duration,
            ease: "power2.inOut",
            clearProps: "width",
          },
        );
        fullWidth.current = 0;
      }
    },
    { dependencies: [loading, morph], scope: ref },
  );

  const button = (
    <Button
      ref={ref}
      type="submit"
      size="lg"
      className={cn(
        "h-12.5 w-full overflow-hidden text-[15.5px] font-bold whitespace-nowrap",
        morph && loading && "disabled:opacity-100",
        className,
      )}
      disabled={loading || disabled}
      aria-busy={loading}
      {...rest}
    >
      {loading ? <LoaderCircle className="animate-spin" /> : null}
      {loading ? (
        morph ? (
          <span className="sr-only">Un momento…</span>
        ) : (
          "Un momento…"
        )
      ) : (
        children
      )}
    </Button>
  );

  // El contenedor centra el círculo mientras el botón se encoge.
  return morph ? <div className="flex justify-center">{button}</div> : button;
}

export function ModalFoot({ children }: { children: ReactNode }) {
  return (
    <p className="mt-5 border-t border-border-soft pt-4.5 text-center text-sm text-muted-foreground">
      {children}
    </p>
  );
}

export function TextLink({ className, ...props }: ComponentProps<"button">) {
  return (
    <button
      type="button"
      className={cn(
        "cursor-pointer font-bold text-primary hover:underline focus-visible:underline focus-visible:outline-none disabled:cursor-default disabled:opacity-60",
        className,
      )}
      {...props}
    />
  );
}
