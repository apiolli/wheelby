import { useRef } from "react";

import symbolUrl from "@/assets/svg/wheelby-simbolo.svg";
import { gsap, prefersReducedMotion, useGSAP } from "@/lib/gsap";

// Pantalla neutra mientras se valida la sesión o carga una página: solo el símbolo con un pulso
// suave, nunca un destello del catálogo.
export default function Splash() {
  const ref = useRef<HTMLDivElement>(null);

  useGSAP(
    () => {
      if (prefersReducedMotion()) return;
      gsap.fromTo(
        "img",
        { scale: 0.94, opacity: 0.55 },
        {
          scale: 1.04,
          opacity: 1,
          duration: 0.9,
          ease: "sine.inOut",
          yoyo: true,
          repeat: -1,
        },
      );
    },
    { scope: ref },
  );

  return (
    <div
      ref={ref}
      role="status"
      className="grid min-h-dvh place-items-center bg-background"
    >
      <img src={symbolUrl} alt="" width={56} height={56} className="size-14" />
      <span className="sr-only">Cargando Wheelby…</span>
    </div>
  );
}
