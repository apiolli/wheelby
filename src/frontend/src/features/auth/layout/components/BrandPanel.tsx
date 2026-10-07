import type { Ref } from "react";
import { Mail } from "lucide-react";

import logoUrl from "@/assets/svg/wheelby-logo-negativo-fondo-petroleo.svg";
import { cn } from "@/lib/utils";
import { ROUTE_D, ROUTE_ID, SCENE, STOPS } from "../scene";

interface BrandPanelProps {
  ref?: Ref<HTMLElement>;
  className?: string;
}

// Panel visual de marca de las páginas de acceso: fondo petróleo con retícula y grano, una ruta
// que recorren vehículos de línea fina y el titular con la palabra rotativa en ámbar. Solo
// ilustración: ninguna foto ni dato del catálogo. AuthLayout lo anima.
export default function BrandPanel({ ref, className }: BrandPanelProps) {
  return (
    <section
      ref={ref}
      data-brand-panel
      className={cn(
        "relative isolate flex h-[32vh] min-h-55 shrink-0 flex-col justify-between overflow-hidden bg-primary px-6 pt-5 pb-6 text-cream",
        "md:sticky md:top-0 md:h-dvh md:w-[55%] md:px-12 md:pt-10 md:pb-12 lg:px-16",
        className,
      )}
    >
      {/* Textura: retícula tenue + grano, para que el petróleo nunca se vea plano. */}
      <div aria-hidden="true" className="bg-reticle absolute inset-0 -z-10" />
      <svg
        aria-hidden="true"
        className="absolute inset-0 -z-10 size-full opacity-[0.09] mix-blend-overlay"
      >
        <filter id="auth-grain">
          <feTurbulence
            type="fractalNoise"
            baseFrequency="0.85"
            numOctaves="2"
            stitchTiles="stitch"
          />
          <feColorMatrix type="saturate" values="0" />
        </filter>
        <rect width="100%" height="100%" filter="url(#auth-grain)" />
      </svg>

      {/* Escena decorativa */}
      <svg
        aria-hidden="true"
        data-scene
        viewBox="0 0 800 900"
        preserveAspectRatio="xMidYMid slice"
        className="pointer-events-none absolute inset-0 -z-10 size-full text-cream"
      >
        <path
          d={ROUTE_D}
          fill="none"
          stroke="currentColor"
          strokeOpacity="0.07"
          strokeWidth="18"
          strokeLinecap="round"
        />
        <path
          id={ROUTE_ID}
          data-route
          d={ROUTE_D}
          fill="none"
          stroke="currentColor"
          strokeOpacity="0.55"
          strokeWidth="2"
          strokeLinecap="round"
        />
        {STOPS.map((s) => (
          <circle
            key={s}
            data-stop
            r="5"
            fill="var(--primary)"
            stroke="currentColor"
            strokeOpacity="0.7"
            strokeWidth="2"
          />
        ))}
        {SCENE.map(({ word, icon: Icon }) => (
          <g key={word} data-vehicle opacity="0">
            <Icon x={-24} y={-24} size={48} strokeWidth={1.4} />
          </g>
        ))}
        <g transform="translate(400 420)">
          <g data-envelope opacity="0">
            <Mail x={-44} y={-44} size={88} strokeWidth={1.2} />
          </g>
        </g>
      </svg>

      {/* El SVG del logo trae su propio fondo petróleo: con mix-blend-lighten deja ver la retícula. */}
      <img
        data-brand-logo
        src={logoUrl}
        alt="Wheelby"
        width={189}
        height={64}
        className="-mt-3 -ml-3.5 h-12 w-auto self-start mix-blend-lighten md:-mt-3.75 md:-ml-3.75 md:h-16"
      />

      <div>
        <p className="sr-only">
          Tu próximo viaje está a una llave de distancia.
        </p>
        <p
          aria-hidden="true"
          className="max-w-140 text-[23px] leading-[1.08] font-bold tracking-tight md:text-[clamp(34px,3.6vw,54px)]"
        >
          <span data-split>Tu próximo</span>
          <span
            data-word-slot
            className="relative block h-[1.12em] overflow-hidden text-highlight"
          >
            {SCENE.map(({ word }, i) => (
              <span
                key={word}
                data-word
                className={cn(
                  "absolute inset-x-0 top-0 whitespace-nowrap",
                  i > 0 && "opacity-0",
                )}
              >
                {word}
              </span>
            ))}
          </span>
          <span data-split>está a una llave de distancia.</span>
        </p>
        <p
          data-trust
          className="mt-4 hidden text-[15px] text-cream/85 md:block"
        >
          Anfitriones verificados · Reseñas reales
        </p>
      </div>
    </section>
  );
}
