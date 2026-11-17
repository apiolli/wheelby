import { useState } from "react";
import { useMotionValueEvent, useScroll } from "motion/react";

// true mientras se baja por la página (pasados los primeros píxeles); false en cuanto se sube.
// Lo usan el header y la barra inferior en móvil para dejar más espacio al contenido.
export function useScrollHidden({ after = 80, tolerance = 6 } = {}) {
  const { scrollY } = useScroll();
  const [hidden, setHidden] = useState(false);

  useMotionValueEvent(scrollY, "change", (y) => {
    const delta = y - (scrollY.getPrevious() ?? 0);
    if (y < after) setHidden(false);
    else if (delta > tolerance) setHidden(true);
    else if (delta < -tolerance) setHidden(false);
  });

  return hidden;
}
