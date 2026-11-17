import { Star } from "lucide-react";
import { cn } from "cn";

// Calificación de solo lectura, de 1 a 5 (admite medias en el promedio).
export default function StarRating({
  value,
  size = "sm",
  className,
}: {
  value: number;
  size?: "sm" | "md";
  className?: string;
}) {
  const icon = size === "sm" ? "size-3.5" : "size-4.5";
  return (
    <span
      role="img"
      aria-label={`${value.toLocaleString("es-DO", { maximumFractionDigits: 1 })} de 5 estrellas`}
      className={cn("inline-flex items-center gap-0.5", className)}
    >
      {[1, 2, 3, 4, 5].map((n) => {
        const fill = Math.max(0, Math.min(1, value - n + 1));
        return (
          <span key={n} className={cn("relative", icon)}>
            <Star
              className={cn("absolute inset-0 text-border", icon)}
              strokeWidth={1.5}
              fill="currentColor"
            />
            <span
              className="absolute inset-0 overflow-hidden"
              style={{ width: `${fill * 100}%` }}
            >
              <Star
                className={cn("text-highlight", icon)}
                strokeWidth={1.5}
                fill="currentColor"
              />
            </span>
          </span>
        );
      })}
    </span>
  );
}
