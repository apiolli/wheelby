import { useId } from "react";
import { cn } from "cn";

// La API no guarda fotos: cada vehículo tiene una ilustración de marca. Silueta de línea en
// teal sobre un fondo suave cuyo tono sale de la marca, así un mismo modelo siempre se ve igual
// y dos marcas distintas se distinguen en el catálogo.
const PALETTES = [
  { from: "#E7F0EF", to: "#CFE0DE", sun: "#E8A33D" }, // bruma teal
  { from: "#FBF3E4", to: "#F2E1C3", sun: "#E8A33D" }, // arena
  { from: "#ECF3E8", to: "#D6E6CE", sun: "#0E4F4B" }, // salvia
  { from: "#E8EFF5", to: "#D1DFEA", sun: "#E8A33D" }, // cielo
  { from: "#F7ECE7", to: "#EED8CE", sun: "#0E4F4B" }, // arcilla
  { from: "#F1EFEB", to: "#E2DED6", sun: "#E8A33D" }, // piedra
] as const;

function hash(text: string) {
  let h = 0;
  for (const ch of text.trim().toLowerCase())
    h = (h * 31 + ch.charCodeAt(0)) | 0;
  return Math.abs(h);
}

const paletteFor = (brand: string) => PALETTES[hash(brand) % PALETTES.length];

interface VehicleArtProps {
  brand: string;
  className?: string;
  // Gris: reservas canceladas o vehículos inactivos.
  muted?: boolean;
}

export default function VehicleArt({
  brand,
  className,
  muted,
}: VehicleArtProps) {
  const id = useId();
  const p = paletteFor(brand);
  const h = hash(brand);
  // Pequeñas variaciones por marca: posición del sol y del auto, para que no sean calcos.
  const sunX = 290 + (h % 50);
  const carX = 68 + (h % 22);
  const initial = brand.trim().charAt(0).toUpperCase() || "W";

  return (
    <svg
      viewBox="0 0 400 300"
      preserveAspectRatio="xMidYMid slice"
      role="img"
      aria-label={`Ilustración de ${brand}`}
      className={cn(
        "block size-full",
        muted && "opacity-60 grayscale",
        className,
      )}
    >
      <defs>
        <linearGradient id={`${id}-bg`} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0" stopColor={p.from} />
          <stop offset="1" stopColor={p.to} />
        </linearGradient>
      </defs>
      <rect width="400" height="300" fill={`url(#${id}-bg)`} />

      {/* Sol con la inicial de la marca */}
      <circle cx={sunX} cy="78" r="34" fill={p.sun} opacity="0.16" />
      <circle
        cx={sunX}
        cy="78"
        r="22"
        fill="none"
        stroke={p.sun}
        strokeOpacity="0.5"
        strokeWidth="1.5"
      />
      <text
        x={sunX}
        y="78"
        textAnchor="middle"
        dominantBaseline="central"
        fontFamily="inherit"
        fontSize="20"
        fontWeight="700"
        fill="#0E4F4B"
        opacity="0.55"
      >
        {initial}
      </text>

      {/* Colinas lejanas */}
      <path
        d="M0 206 Q70 176 140 196 T280 190 T400 200 V300 H0 Z"
        fill="#ffffff"
        opacity="0.35"
      />
      {/* Camino */}
      <line
        x1="24"
        y1="236"
        x2="376"
        y2="236"
        stroke="#0E4F4B"
        strokeOpacity="0.3"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      <g
        stroke="#0E4F4B"
        strokeOpacity="0.25"
        strokeWidth="1.5"
        strokeLinecap="round"
      >
        <line x1="60" y1="252" x2="96" y2="252" />
        <line x1="150" y1="252" x2="186" y2="252" />
        <line x1="240" y1="252" x2="276" y2="252" />
        <line x1="330" y1="252" x2="352" y2="252" />
      </g>

      {/* Auto de perfil, en línea fina */}
      <g
        transform={`translate(${carX} 128)`}
        fill="none"
        stroke="#0E4F4B"
        strokeWidth="2.25"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path
          d="M14 92 H34 M90 92 H168 M224 92 H240 Q248 92 248 84 V72 Q248 62 238 60 L214 55 L186 26 Q178 18 166 18 H104 Q92 18 84 26 L60 50 L26 56 Q12 59 12 72 V84 Q12 92 14 92 Z"
          fill="#ffffff"
          fillOpacity="0.55"
        />
        <path d="M70 52 L92 30 Q96 26 102 26 H128 V52 Z" />
        <path d="M138 26 H164 Q170 26 174 30 L196 52 H138 Z" />
        <path d="M132 58 V86" strokeOpacity="0.6" />
        <path d="M236 66 H246" />
        <path d="M14 70 H22" />
        <circle cx="62" cy="92" r="20" fill="#ffffff" />
        <circle cx="62" cy="92" r="8" />
        <circle cx="196" cy="92" r="20" fill="#ffffff" />
        <circle cx="196" cy="92" r="8" />
      </g>
    </svg>
  );
}
