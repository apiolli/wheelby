import { useLayoutEffect, useRef } from "react";
import { animate, useReducedMotion } from "motion/react";

import { formatPrice } from "@/lib/format";

// Importe que cuenta del valor anterior al nuevo (0.45 s) cuando cambian las fechas. La primera
// vez se muestra tal cual. Escribe en el nodo de texto, sin renders por cuadro.
export default function AnimatedAmount({
  value,
  currency = "DOP",
  className,
}: {
  value: number;
  currency?: string;
  className?: string;
}) {
  const ref = useRef<HTMLSpanElement>(null);
  const previous = useRef(value);
  const reduce = useReducedMotion();

  useLayoutEffect(() => {
    const text = ref.current?.firstChild;
    const from = previous.current;
    previous.current = value;
    if (!text || reduce || from === value) return;
    const controls = animate(from, value, {
      duration: 0.45,
      ease: "easeOut",
      onUpdate: (v) => {
        text.nodeValue = formatPrice(v, currency);
      },
      onComplete: () => {
        text.nodeValue = formatPrice(value, currency);
      },
    });
    return () => controls.stop();
  }, [value, currency, reduce]);

  return (
    <span ref={ref} className={className}>
      {formatPrice(value, currency)}
    </span>
  );
}
