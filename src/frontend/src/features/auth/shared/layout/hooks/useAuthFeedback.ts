import type { ContextSafeFunc } from "@gsap/react";
import type { StageRefs } from "../types";
import { gsap, prefersReducedMotion } from "@/lib/gsap";

export const useAuthFeedback = (
  contextSafe: ContextSafeFunc,
  { contentRef }: StageRefs,
) => {
  const shake = () =>
    contextSafe(() => {
      if (!contentRef.current || prefersReducedMotion()) return;
      gsap.fromTo(
        contentRef.current,
        { x: 0 },
        {
          keyframes: { x: [0, -6, 6, -6, 6, -3, 0], easeEach: "sine.inOut" },
          duration: 0.35,
          ease: "none",
          clearProps: "transform",
        },
      );
    })();

  // "Revisa tu correo": un sobre de línea fina se dibuja en el panel y sale volando hacia arriba.
  const mailSent = () =>
    contextSafe(() => {
      if (prefersReducedMotion()) return;
      const strokes = "[data-envelope] path, [data-envelope] rect";
      gsap
        .timeline()
        .set("[data-envelope]", { opacity: 1, y: 0 })
        .fromTo(
          strokes,
          { drawSVG: "0%" },
          {
            drawSVG: "100%",
            duration: 0.6,
            stagger: 0.1,
            ease: "power2.inOut",
          },
        )
        .to(
          "[data-envelope]",
          { y: -260, opacity: 0, duration: 0.7, ease: "power2.in" },
          "+=0.3",
        );
    })();

  return { feedback: { mailSent, shake } };
};
