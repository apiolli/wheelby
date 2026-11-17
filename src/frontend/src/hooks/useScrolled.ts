import { useState } from "react";
import { useMotionValueEvent, useScroll } from "motion/react";

interface Thresholds {
  /** Píxeles de scroll a partir de los que se considera "desplazado". */
  enter?: number;
  /** Píxeles por debajo de los que vuelve a "arriba del todo". */
  exit?: number;
}

/**
 * true cuando la página se ha desplazado. Usa histéresis (enter > exit) para que
 * el estado no parpadee si el scroll se queda cerca del umbral o rebota (iOS, trackpad).
 * Solo provoca un render al cruzar un umbral, no en cada píxel.
 */
export function useScrolled({ enter = 24, exit = 4 }: Thresholds = {}) {
  const { scrollY } = useScroll();
  const [scrolled, setScrolled] = useState(
    () => typeof window !== "undefined" && window.scrollY > enter,
  );

  useMotionValueEvent(scrollY, "change", (y) => {
    setScrolled((prev) => (prev ? y > exit : y > enter));
  });

  return scrolled;
}
