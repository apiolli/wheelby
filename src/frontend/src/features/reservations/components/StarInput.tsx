import { useRef, type KeyboardEvent } from "react";
import { Star } from "lucide-react";
import { cn } from "cn";

const LABELS = ["Muy mala", "Mala", "Regular", "Buena", "Excelente"];

// Estrellas de 1 a 5 como grupo de radio: flechas para cambiar, Tab para salir.
export default function StarInput({
  value,
  onChange,
  labelledBy,
}: {
  value: number;
  onChange: (value: number) => void;
  labelledBy: string;
}) {
  const refs = useRef<(HTMLButtonElement | null)[]>([]);

  const onKeyDown = (e: KeyboardEvent) => {
    const delta =
      e.key === "ArrowRight" || e.key === "ArrowUp"
        ? 1
        : e.key === "ArrowLeft" || e.key === "ArrowDown"
          ? -1
          : 0;
    if (!delta) return;
    e.preventDefault();
    const next = Math.min(5, Math.max(1, (value || 0) + delta));
    onChange(next);
    refs.current[next - 1]?.focus();
  };

  return (
    <div className="grid gap-1.5">
      <div
        role="radiogroup"
        aria-labelledby={labelledBy}
        className="flex gap-1"
        onKeyDown={onKeyDown}
      >
        {[1, 2, 3, 4, 5].map((n) => (
          <button
            key={n}
            ref={(el) => {
              refs.current[n - 1] = el;
            }}
            type="button"
            role="radio"
            aria-checked={value === n}
            aria-label={`${n} ${n === 1 ? "estrella" : "estrellas"}: ${LABELS[n - 1]}`}
            tabIndex={value === n || (value === 0 && n === 1) ? 0 : -1}
            onClick={() => onChange(n)}
            className="rounded-full p-1 outline-none focus-visible:ring-2 focus-visible:ring-ring"
          >
            <Star
              className={cn(
                "size-8 transition-transform hover:scale-110",
                n <= value ? "text-highlight" : "text-border",
              )}
              strokeWidth={1.25}
              fill="currentColor"
            />
          </button>
        ))}
      </div>
      <p className="h-5 text-[13px] text-muted-foreground" aria-live="polite">
        {value ? LABELS[value - 1] : "Elige de 1 a 5 estrellas"}
      </p>
    </div>
  );
}
