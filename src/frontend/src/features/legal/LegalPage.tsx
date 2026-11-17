import { useEffect, useState } from "react";
import { m } from "motion/react";
import { ChevronDown, ChevronLeft, FlaskConical } from "lucide-react";
import { Link, useNavigate } from "react-router";
import { cn } from "cn";

import { Button, buttonVariants } from "@/components/ui/button";
import Footer from "@/features/marketplace/catalog/components/Footer";
import Logo from "@/features/marketplace/header/Logo";
import { formatDate } from "@/lib/format";
import { ENTER } from "@/lib/motion";
import { AppLayout } from "@/router/pages";
import { useSession } from "@/stores/session.store";
import { LEGAL, type LegalDocId } from "./content";

// Términos y Privacidad: públicas. Con sesión, el layout normal; sin ella, un header simple.
export default function LegalPage({ doc }: { doc: LegalDocId }) {
  const status = useSession((s) => s.status);
  const page = <LegalContent doc={doc} />;

  if (status === "authenticated") return <AppLayout>{page}</AppLayout>;
  return (
    <>
      <header className="sticky top-0 z-40 border-b border-border-soft bg-background">
        <div className="flex h-18 items-center justify-between px-gutter">
          <Logo variant="responsive" to="/auth/login" />
          {status === "anonymous" && (
            <Link
              to="/auth/login"
              className={buttonVariants({
                variant: "outline",
                className: "h-11 rounded-full px-5",
              })}
            >
              Iniciar sesión
            </Link>
          )}
        </div>
      </header>
      {page}
      <Footer />
    </>
  );
}

function LegalContent({ doc: id }: { doc: LegalDocId }) {
  const navigate = useNavigate();
  const doc = LEGAL[id];
  const other: LegalDocId = id === "terminos" ? "privacidad" : "terminos";
  const active = useActiveSection(doc.sections.map((s) => s.id));

  useEffect(() => {
    document.title = `${doc.title} · Wheelby`;
  }, [doc.title]);

  const toc = (onPick?: () => void) => (
    <ol className="grid gap-0.5 text-[14px]">
      {doc.sections.map((s, i) => (
        <li key={s.id}>
          <a
            href={`#${s.id}`}
            onClick={onPick}
            aria-current={active === s.id ? "location" : undefined}
            className={cn(
              "relative flex gap-2 rounded-lg py-1.5 pr-2 pl-3 outline-none focus-visible:ring-2 focus-visible:ring-ring",
              active === s.id
                ? "font-semibold text-primary"
                : "text-muted-foreground hover:text-foreground",
            )}
          >
            {active === s.id && (
              <m.span
                layoutId="legal-toc"
                aria-hidden="true"
                className="absolute inset-y-1 left-0 w-0.5 rounded-full bg-primary"
              />
            )}
            <span className="tabular-nums">{i + 1}.</span> {s.title}
          </a>
        </li>
      ))}
    </ol>
  );

  return (
    <main className="px-gutter pt-8 pb-16">
      <div className="mx-auto grid max-w-260 gap-10 lg:grid-cols-[220px_minmax(0,720px)] lg:gap-16">
        {/* Índice lateral sticky (escritorio), con la sección visible resaltada */}
        <aside className="hidden lg:block">
          <nav aria-label="En esta página" className="sticky top-28">
            <p className="mb-2 pl-3 text-[12px] font-bold tracking-wide text-muted-foreground uppercase">
              En esta página
            </p>
            {toc()}
          </nav>
        </aside>

        <m.article {...ENTER} className="min-w-0">
          <Button
            variant="ghost"
            size="sm"
            className="-ml-3 mb-4 rounded-full text-muted-foreground"
            onClick={() =>
              window.history.length > 1 ? navigate(-1) : navigate("/")
            }
          >
            <ChevronLeft /> Volver
          </Button>
          <p className="mb-4 inline-flex items-center gap-1.5 rounded-full bg-highlight-soft px-3 py-1 text-[12.5px] font-semibold text-highlight-foreground">
            <FlaskConical className="size-3.5" strokeWidth={2} />
            Proyecto académico: texto de ejemplo.
          </p>
          <h1 className="text-[30px] leading-tight font-bold tracking-tight md:text-[36px]">
            {doc.title}
          </h1>
          <p className="mt-2 text-[14px] text-muted-foreground">
            Última actualización: {formatDate(doc.updated)}
          </p>

          {/* Índice colapsable arriba (móvil) */}
          <details className="group mt-6 rounded-2xl border border-border lg:hidden">
            <summary className="flex h-12 cursor-pointer list-none items-center justify-between px-4 text-[14.5px] font-semibold outline-none focus-visible:ring-2 focus-visible:ring-ring [&::-webkit-details-marker]:hidden">
              En esta página
              <ChevronDown className="size-4 transition-transform group-open:rotate-180" />
            </summary>
            <div className="border-t border-border-soft px-2 py-2">{toc()}</div>
          </details>

          <p className="mt-8 text-[16px] leading-relaxed">{doc.intro}</p>
          {doc.sections.map((s, i) => (
            <section
              key={s.id}
              id={s.id}
              aria-labelledby={`${s.id}-t`}
              className="mt-10 scroll-mt-28"
            >
              <h2 id={`${s.id}-t`} className="text-xl font-bold tracking-tight">
                {i + 1}. {s.title}
              </h2>
              {s.paragraphs.map((p, j) => (
                <p
                  key={j}
                  className="mt-3 text-[15.5px] leading-relaxed text-foreground/90"
                >
                  {p}
                </p>
              ))}
            </section>
          ))}

          <div className="mt-14 flex flex-wrap items-center justify-between gap-4 border-t border-border-soft pt-6 text-[14.5px]">
            <span className="text-muted-foreground">Lee también</span>
            <Link
              to={`/${other}`}
              className="font-semibold text-primary underline underline-offset-4"
            >
              {LEGAL[other].title}
            </Link>
          </div>
        </m.article>
      </div>
    </main>
  );
}

// Sección visible: la última cuyo título ya pasó la línea de lectura (primer tercio).
function useActiveSection(ids: string[]) {
  const [active, setActive] = useState(ids[0]);
  const key = ids.join(",");
  useEffect(() => {
    const list = key.split(",");
    const onScroll = () => {
      // Línea de lectura: el primer tercio de la pantalla.
      const line = Math.max(160, window.innerHeight * 0.3);
      let current = list[0];
      for (const id of list) {
        const el = document.getElementById(id);
        if (el && el.getBoundingClientRect().top < line) current = id;
      }
      // Al final de la página, la última sección (puede ser corta y no llegar arriba).
      if (window.innerHeight + window.scrollY >= document.body.scrollHeight - 4)
        current = list[list.length - 1];
      setActive(current);
    };
    onScroll();
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, [key]);
  return active;
}
