import type { ReactNode } from "react";

interface AuthPanelProps {
  title: string;
  greeting?: string;
  children: ReactNode;
}

// Encabezado y cuerpo de cada estado de las páginas de acceso. Cada formulario decide su título
// según su propio estado.
export default function AuthPanel({
  title,
  greeting,
  children,
}: AuthPanelProps) {
  return (
    <>
      <header data-enter className="mb-7">
        <h1 className="text-[28px] leading-tight font-bold tracking-tight md:text-[32px]">
          {title}
        </h1>
        {greeting && (
          <p className="mt-1.5 text-[15px] text-muted-foreground">{greeting}</p>
        )}
      </header>
      {children}
    </>
  );
}
