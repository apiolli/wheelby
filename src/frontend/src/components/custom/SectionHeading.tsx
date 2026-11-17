import type { ReactNode } from "react";

// Título de sección del perfil (26 px) con descripción y acciones opcionales a la derecha.
export default function SectionHeading({
  title,
  description,
  children,
}: {
  title: string;
  description?: string;
  children?: ReactNode;
}) {
  return (
    <header className="mb-6 flex flex-wrap items-end justify-between gap-4">
      <div>
        <h1 className="text-[26px] leading-tight font-bold tracking-tight">
          {title}
        </h1>
        {description && (
          <p className="mt-1.5 text-[14.5px] text-muted-foreground">
            {description}
          </p>
        )}
      </div>
      {children}
    </header>
  );
}
