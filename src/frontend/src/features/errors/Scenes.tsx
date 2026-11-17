import { m, useReducedMotion } from "motion/react";

// Escenas de línea fina de las páginas 403 y 404: se dibujan y el vehículo recorre su camino
// una sola vez. Con prefers-reduced-motion quedan estáticas en su estado final.

const STROKE = "var(--primary)";
const ACCENT = "var(--highlight)";
const EASE = [0.22, 1, 0.36, 1] as const;

// Camino del auto en la 404: sigue la ruta, se pasa de la salida y da la vuelta.
const ROAD = "M24 176 C 96 176 112 112 184 112 S 276 64 318 64";
const CAR_PATH = `${ROAD} C 344 64 352 92 332 100 C 312 108 296 92 304 78`;

// Auto visto desde arriba: gira con el camino sin quedar de cabeza.
function TopCar() {
  return (
    <svg width="34" height="20" viewBox="0 0 34 20" aria-hidden="true">
      <rect
        x="1"
        y="2"
        width="32"
        height="16"
        rx="6"
        fill="var(--background)"
        stroke={STROKE}
        strokeWidth="2"
      />
      <path
        d="M21 4.5 h4 a3 3 0 0 1 3 3 v5 a3 3 0 0 1 -3 3 h-4 Z"
        fill="var(--primary-soft)"
        stroke={STROKE}
        strokeWidth="1.5"
      />
      <path
        d="M6 5 v10"
        stroke={STROKE}
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      <circle cx="31" cy="5" r="1.4" fill={ACCENT} />
      <circle cx="31" cy="15" r="1.4" fill={ACCENT} />
    </svg>
  );
}

export function LostCarScene() {
  const reduce = useReducedMotion();
  return (
    <div
      className="relative mx-auto h-[220px] w-[360px] max-w-full"
      aria-hidden="true"
    >
      <svg viewBox="0 0 360 220" className="absolute inset-0 size-full">
        <ellipse cx="180" cy="204" rx="140" ry="7" fill="var(--secondary)" />
        <circle cx="72" cy="66" r="34" fill="var(--primary-soft)" />
        {/* Ruta: borde y línea central discontinua */}
        <m.path
          d={ROAD}
          fill="none"
          stroke={STROKE}
          strokeOpacity="0.18"
          strokeWidth="22"
          strokeLinecap="round"
          initial={reduce ? false : { pathLength: 0 }}
          animate={{ pathLength: 1 }}
          transition={{ duration: 0.9, ease: EASE }}
        />
        <m.path
          d={ROAD}
          fill="none"
          stroke={STROKE}
          strokeWidth="1.5"
          strokeDasharray="6 8"
          initial={reduce ? false : { opacity: 0 }}
          animate={{ opacity: 0.6 }}
          transition={{ delay: 0.8, duration: 0.3 }}
        />
        {/* Fin de la ruta y cartel de salida, en ámbar */}
        <m.g
          initial={reduce ? false : { opacity: 0, y: 6 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.7, duration: 0.4 }}
        >
          <path
            d="M322 52 v24"
            stroke={STROKE}
            strokeWidth="2"
            strokeLinecap="round"
          />
          <path
            d="M268 30 v26"
            stroke={STROKE}
            strokeWidth="2"
            strokeLinecap="round"
          />
          <rect x="246" y="14" width="44" height="18" rx="4" fill={ACCENT} />
          <path
            d="M256 23 h20 m-5 -5 l5 5 l-5 5"
            stroke="var(--background)"
            strokeWidth="2"
            fill="none"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </m.g>
      </svg>
      <m.div
        className="absolute top-0 left-0"
        style={{
          offsetPath: `path("${CAR_PATH}")`,
          offsetRotate: "auto",
          offsetAnchor: "center",
        }}
        initial={reduce ? false : { offsetDistance: "0%" }}
        animate={{ offsetDistance: "100%" }}
        transition={{ delay: 0.5, duration: 2.6, ease: [0.45, 0, 0.25, 1] }}
      >
        <TopCar />
      </m.div>
    </div>
  );
}

// 403: una barrera de peaje baja y el auto se detiene frente a ella.
export function TollBarrierScene() {
  const reduce = useReducedMotion();
  return (
    <svg
      viewBox="0 0 360 200"
      className="mx-auto h-[200px] w-[360px] max-w-full"
      aria-hidden="true"
    >
      <ellipse cx="180" cy="186" rx="150" ry="7" fill="var(--secondary)" />
      <m.path
        d="M16 160 H344"
        stroke={STROKE}
        strokeWidth="2"
        strokeLinecap="round"
        initial={reduce ? false : { pathLength: 0 }}
        animate={{ pathLength: 1 }}
        transition={{ duration: 0.6, ease: EASE }}
      />
      {/* Caseta */}
      <rect
        x="262"
        y="74"
        width="54"
        height="86"
        rx="6"
        fill="var(--primary-soft)"
        stroke={STROKE}
        strokeWidth="2"
      />
      <rect
        x="274"
        y="88"
        width="30"
        height="22"
        rx="3"
        fill="var(--background)"
        stroke={STROKE}
        strokeWidth="1.5"
      />
      <path
        d="M256 74 h66"
        stroke={STROKE}
        strokeWidth="2.5"
        strokeLinecap="round"
      />
      {/* Poste y brazo de la barrera (gira sobre el poste) */}
      <rect
        x="244"
        y="112"
        width="10"
        height="48"
        rx="2"
        fill="var(--background)"
        stroke={STROKE}
        strokeWidth="2"
      />
      <m.g
        style={{ transformBox: "view-box", originX: "249px", originY: "118px" }}
        initial={reduce ? false : { rotate: 80 }}
        animate={{ rotate: 0 }}
        transition={{ delay: 1.1, type: "spring", bounce: 0.35, duration: 0.8 }}
      >
        <rect
          x="140"
          y="113"
          width="112"
          height="10"
          rx="5"
          fill="var(--background)"
          stroke={STROKE}
          strokeWidth="2"
        />
        <path
          d="M156 113 l-6 10 M176 113 l-6 10 M196 113 l-6 10 M216 113 l-6 10"
          stroke={ACCENT}
          strokeWidth="5"
        />
        <circle cx="249" cy="118" r="4" fill={STROKE} />
      </m.g>
      {/* Auto de perfil que llega y frena antes de la barrera */}
      <m.g
        initial={reduce ? false : { x: -150 }}
        animate={{ x: 0 }}
        transition={{ delay: 0.4, duration: 1.4, ease: [0.16, 1, 0.3, 1] }}
      >
        <g
          fill="none"
          stroke={STROKE}
          strokeWidth="2"
          strokeLinecap="round"
          strokeLinejoin="round"
        >
          <path
            d="M40 150 V140 Q40 132 48 131 L64 129 L76 117 Q79 114 84 114 H106 Q111 114 114 117 L124 129 L132 131 Q138 132 138 139 V150 Z"
            fill="var(--background)"
          />
          <path
            d="M70 130 L80 120 H92 V130 Z M98 120 H108 L116 130 H98 Z"
            fill="var(--primary-soft)"
            strokeWidth="1.5"
          />
          <circle cx="62" cy="151" r="9" fill="var(--background)" />
          <circle cx="118" cy="151" r="9" fill="var(--background)" />
        </g>
        <circle cx="136" cy="137" r="2.2" fill={ACCENT} />
      </m.g>
    </svg>
  );
}
