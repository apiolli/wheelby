import type { ReactNode } from "react";

interface EmptyStateProps {
  illustration: ReactNode;
  title: string;
  text: string;
  action?: ReactNode;
}

// Estado vacío cálido: ilustración simple, mensaje corto y una salida.
export default function EmptyState({
  illustration,
  title,
  text,
  action,
}: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center rounded-2xl border border-dashed border-border px-6 py-12 text-center">
      {illustration}
      <h2 className="mt-4 text-lg leading-tight font-bold tracking-tight">
        {title}
      </h2>
      <p className="mt-1.5 mb-5 max-w-85 text-[14.5px] text-muted-foreground">
        {text}
      </p>
      {action}
    </div>
  );
}
