import { gsap } from "gsap";
import { useGSAP } from "@gsap/react";

// GSAP para las animaciones de contenido
gsap.registerPlugin(useGSAP);

export { gsap, useGSAP };

export const DURATION = { micro: 0.2, enter: 0.5, reduced: 0.15 };
export const EASE = { out: "power2.out", enter: "power3.out" };

let reduced = false;
gsap
  .matchMedia()
  .add(
    {
      reduce: "(prefers-reduced-motion: reduce)",
      ok: "(prefers-reduced-motion: no-preference)",
    },
    (ctx) => {
      reduced = Boolean(ctx.conditions?.reduce);
    },
  );

export const prefersReducedMotion = () => reduced;
