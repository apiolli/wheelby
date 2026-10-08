import { useRef } from "react";
import { Check, Dot } from "lucide-react";

import { gsap, prefersReducedMotion, useGSAP } from "@/lib/gsap";
import { cn } from "@/lib/utils";
import { PASSWORD_RULES, passwordChecks } from "../../lib/password";

// Color de cada nivel (1-3), leído de los tokens para poder interpolarlo con GSAP.
const LEVEL_TOKEN = ["--strength-weak", "--highlight", "--primary"];
const tokenColor = (level: number) =>
  getComputedStyle(document.documentElement)
    .getPropertyValue(LEVEL_TOKEN[level - 1])
    .trim();

// Indicador de fuerza sutil: tres barras y la lista de requisitos.
export default function PasswordStrength({ password }: { password: string }) {
  const barsRef = useRef<HTMLDivElement>(null);
  const prevLevel = useRef<number | null>(null);
  const met = passwordChecks(password);
  const level = password ? met.filter(Boolean).length : 0;

  // Los segmentos crecen con un elástico corto y el color pasa de un nivel a otro sin parpadeo
  // (se interpola el mismo relleno, no se cambia de clase).
  useGSAP(
    () => {
      const fills = gsap.utils.toArray<HTMLElement>("[data-strength-fill]");
      const color = level ? tokenColor(level) : null;
      const prev = prevLevel.current;
      prevLevel.current = level;
      if (prev === null) {
        gsap.set(fills, {
          scaleX: (i: number) => (i < level ? 1 : 0),
          ...(color && { backgroundColor: color }),
        });
        return;
      }
      const reduce = prefersReducedMotion();
      fills.forEach((fill, i) => {
        const on = i < level;
        gsap.to(fill, {
          scaleX: on ? 1 : 0,
          duration: reduce ? 0.15 : on ? 0.45 : 0.2,
          ease: reduce ? "none" : on ? "elastic.out(1, 0.75)" : "power2.out",
          overwrite: "auto",
        });
      });
      if (!color) return;
      // Desde vacío las barras miden 0: el color se aplica sin interpolar desde transparente.
      if (prev === 0) gsap.set(fills, { backgroundColor: color });
      else
        gsap.to(fills, {
          backgroundColor: color,
          duration: reduce ? 0.15 : 0.25,
          ease: "none",
          overwrite: "auto",
        });
    },
    { dependencies: [level], scope: barsRef },
  );

  return (
    <div className="-mt-0.5 mb-4" aria-live="polite">
      <div
        ref={barsRef}
        className="mb-2 grid grid-cols-3 gap-1"
        aria-hidden="true"
      >
        {[1, 2, 3].map((n) => (
          <span key={n} className="relative h-1 rounded-full bg-border-soft">
            <span
              data-strength-fill
              className="absolute inset-0 origin-left rounded-full"
            />
          </span>
        ))}
      </div>
      <ul className="grid gap-0.5 text-[12.5px] text-muted-foreground">
        {PASSWORD_RULES.map((r, i) => (
          <li
            key={r.id}
            className={cn(
              "flex items-center gap-1.5 transition-colors",
              met[i] && "text-foreground",
            )}
          >
            {met[i] ? (
              <Check className="size-3.25 text-success" strokeWidth={2.6} />
            ) : (
              <Dot className="size-3.25" strokeWidth={4} />
            )}
            {r.label}
            <span className="sr-only">
              {met[i] ? "(cumplido)" : "(pendiente)"}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}
