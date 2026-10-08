import type { ContextSafeFunc } from "@gsap/react";
import { Flip } from "gsap/Flip";
import type { StageRefs } from "../types";
import type { AuthRouteState } from "@/types/types";
import { DURATION, EASE, gsap, prefersReducedMotion } from "@/lib/gsap";
import { flushSync } from "react-dom";
import { useNavigate } from "react-router";
import type { StepDirection } from "../../motion.context";

export const useAuthTransition = (
  { contentRef, busy, panelRef, columnRef, pendingGo }: StageRefs,
  contextSafe: ContextSafeFunc,
) => {
  const navigate = useNavigate();

  // Cambio de estado en la misma página (formulario → "Revisa tu correo", pasos de recuperación).
  const swap = (update: () => void, direction: StepDirection = "forward") =>
    contextSafe(() => {
      const content = contentRef.current;
      if (!content || busy.current) {
        update();
        return;
      }
      busy.current = true;
      const reduce = prefersReducedMotion();
      const dx = reduce ? 0 : direction === "forward" ? 16 : -16;
      gsap.to(content, {
        opacity: 0,
        x: -dx,
        duration: reduce ? DURATION.reduced / 2 : 0.18,
        ease: "power2.in",
        onComplete: () => {
          flushSync(update);
          gsap.fromTo(
            content,
            { opacity: 0, x: dx },
            {
              opacity: 1,
              x: 0,
              duration: reduce ? DURATION.reduced : 0.4,
              ease: EASE.enter,
              clearProps: "opacity,transform",
              onComplete: () => void (busy.current = false),
            },
          );
        },
      });
    })();

  // Navegación entre páginas de acceso: el formulario saliente se desvanece, el panel cruza al otro
  // lado con Flip (600 ms) y el entrante hace su stagger. Se siente como una misma página.
  const go = (to: string, state?: AuthRouteState) =>
    contextSafe(() => {
      const content = contentRef.current;
      const target = { pathname: to, search: window.location.search };
      if (!content || busy.current) {
        navigate(target, { state });
        return;
      }
      busy.current = true;
      const reduce = prefersReducedMotion();
      gsap.to(content, {
        opacity: 0,
        duration: reduce ? DURATION.reduced / 2 : 0.15,
        ease: "power1.in",
        onComplete: () => {
          const flip =
            !reduce && panelRef.current && columnRef.current
              ? Flip.getState([panelRef.current, columnRef.current])
              : null;
          pendingGo.current = { flip, reduce };
          // React Router aplica la navegación en una transición: la entrada se anima en el
          // useGSAP de abajo, cuando la ruta nueva ya está en el DOM.
          navigate(target, { state });
        },
      });
    })();

  return {
    transition: {
      go,
      swap,
    },
  };
};
