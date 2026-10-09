import { useRef, type ComponentProps } from "react";
import { LoaderCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { gsap, prefersReducedMotion, useGSAP } from "@/lib/gsap";
import { cn } from "@/lib/utils";

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
