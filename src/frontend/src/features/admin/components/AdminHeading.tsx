import type { ReactNode } from "react";

export default function AdminHeading({
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
        <h1 className="text-2xl leading-tight font-bold tracking-tight">
          {title}
        </h1>
        {description && (
          <p className="mt-1 text-[14px] text-muted-foreground">
            {description}
          </p>
        )}
      </div>
      {children}
    </header>
  );
}
