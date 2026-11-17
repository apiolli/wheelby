import type { ReactNode } from "react";
import { LazyMotion, MotionConfig } from "motion/react";

// Las funciones de animación de Motion (layout y layoutId incluidos) se cargan en un chunk aparte.
const loadMotionFeatures = () =>
  import("@/lib/motion-features").then((m) => m.default);

// reducedMotion="user": respeta prefers-reduced-motion en todas las animaciones de Motion.
// strict: obliga a usar m.* en lugar de motion.*, para no cargar todo Motion por adelantado.
export default function MotionRoot({ children }: { children: ReactNode }) {
  return (
    <MotionConfig reducedMotion="user">
      <LazyMotion features={loadMotionFeatures} strict>
        {children}
      </LazyMotion>
    </MotionConfig>
  );
}
