import { useLayoutEffect, useRef } from "react";
import { animate, useReducedMotion } from "motion/react";

// Número que cuenta desde 0 hasta su valor (0.8 s) al montarse. Con movimiento reducido se
// muestra tal cual. Se escribe en el nodo de texto que pinta React, sin renders por cuadro.
export default function CountUp({
  value,
  className,
}: {
  value: number;
  className?: string;
}) {
  const ref = useRef<HTMLSpanElement>(null);
  const reduce = useReducedMotion();

  // Antes del primer pintado, para que no se vea el valor final un instante.
  useLayoutEffect(() => {
    const text = ref.current?.firstChild;
    if (!text || reduce) return;
    const controls = animate(0, value, {
      duration: 0.8,
      ease: "easeOut",
      onUpdate: (v) => {
        text.nodeValue = String(Math.round(v));
      },
    });
    return () => controls.stop();
  }, [value, reduce]);

  return (
    <span ref={ref} className={className}>
      {value}
    </span>
  );
}
