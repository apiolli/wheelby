import { useEffect, useRef, useState } from "react";
import {
  AnimatePresence,
  m,
  useMotionValueEvent,
  useReducedMotion,
  useScroll,
  useTransform,
} from "motion/react";
import { cn } from "cn";
import { Link, useLocation, useNavigate, useSearchParams } from "react-router";

import { buttonVariants } from "@/components/ui/button";
import { useMediaQuery } from "@/hooks/useMediaQuery";
import { useScrolled } from "@/hooks/useScrolled";
import { HEADER_SPRING, SEARCH_LAYOUT_ID } from "@/lib/motion";
import { parseCatalogParams, rangeSchema } from "@/lib/schemas";
import { isAdmin, showToast } from "@/stores/session.store";
import type { User } from "@/types/types";
import { CompactSearch, MobileSearchPill } from "./CompactSearch";
import Logo from "./Logo";
import { ADMIN_HEADER_NAV, MEMBER_NAV } from "./nav";
import NavTabs from "./NavTabs";
import SearchBar from "./SearchBar";
import { toSearchParams, type SearchField, type SearchQuery } from "./search";
import UserMenu from "./UserMenu";

// Distancia de scroll que vuelve a plegar la búsqueda desplegada.
const COLLAPSE_AFTER_PX = 40;

// Alturas del header en cada estado.
const HEIGHT = {
  desktop: { open: 176, compact: 80 },
  mobile: { top: 140, scrolled: 72, expanded: 272 },
};

// Entrada y salida de las pestañas.
const TABS_MOTION = {
  initial: { opacity: 0, y: -12, scale: 0.96 },
  animate: { opacity: 1, y: 0, scale: 1 },
  exit: { opacity: 0, y: -12, scale: 0.96 },
  transition: HEADER_SPRING,
};

const queryFrom = (params: URLSearchParams): SearchQuery => {
  const { from, to, currency } = parseCatalogParams(params);
  return { from, to, currency };
};

/**
 * Header fijo al estilo Airbnb.
 * - Arriba del todo: pestañas y barra de búsqueda grande.
 * - Al bajar: se encoge y la barra se transforma (layoutId) en una píldora compacta.
 * - Al pulsar la píldora: la barra se despliega sobre un fondo oscurecido.
 * El Administrador no alquila: su header no lleva búsqueda.
 */
export default function SiteHeader({ user }: { user: User }) {
  const admin = isAdmin(user);
  return admin ? <AdminHeader user={user} /> : <MemberHeader user={user} />;
}

function MemberHeader({ user }: { user: User }) {
  const navigate = useNavigate();
  const { pathname } = useLocation();
  const [params] = useSearchParams();
  const isDesktop = useMediaQuery("(min-width: 768px)");
  const scrolled = useScrolled();
  const reduceMotion = useReducedMotion();
  const [expanded, setExpanded] = useState(false);
  const [focusField, setFocusField] = useState<SearchField | null>(null);
  const expandedAtY = useRef(0);

  // El borrador de la búsqueda se reinicia cuando cambia la búsqueda de la URL (en el catálogo
  // y en los resultados).
  const onSearchPage = pathname === "/" || pathname === "/buscar";
  const urlKey = onSearchPage ? params.toString() : "";
  const [synced, setSynced] = useState(urlKey);
  const [query, setQuery] = useState<SearchQuery>(() => queryFrom(params));
  if (urlKey !== synced) {
    setSynced(urlKey);
    if (onSearchPage) setQuery(queryFrom(params));
  }

  // Sombra progresiva ligada al scroll, sin renders de React.
  const { scrollY } = useScroll();
  const boxShadow = useTransform(
    scrollY,
    [0, 80],
    ["0 1px 12px rgba(0,0,0,0)", "0 1px 12px rgba(0,0,0,0.08)"],
  );

  // En escritorio la barra grande está visible arriba del todo o cuando se despliega.
  const desktopOpen = !scrolled || expanded;
  const showMobileTabs = !scrolled && !expanded;

  const height = isDesktop
    ? desktopOpen
      ? HEIGHT.desktop.open
      : HEIGHT.desktop.compact
    : expanded
      ? HEIGHT.mobile.expanded
      : scrolled
        ? HEIGHT.mobile.scrolled
        : HEIGHT.mobile.top;

  const expand = (field: SearchField) => {
    expandedAtY.current = window.scrollY;
    setFocusField(field);
    setExpanded(true);
  };

  const collapse = () => {
    setExpanded(false);
    setFocusField(null);
  };

  // Plegar al hacer scroll…
  useMotionValueEvent(scrollY, "change", (y) => {
    if (expanded && Math.abs(y - expandedAtY.current) > COLLAPSE_AFTER_PX)
      collapse();
  });

  // …o con Escape.
  useEffect(() => {
    if (!expanded) return;
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "Escape") {
        setExpanded(false);
        setFocusField(null);
      }
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [expanded]);

  // La búsqueda lleva a los resultados con las fechas y la moneda en la URL.
  const submit = (q: SearchQuery) => {
    const range = rangeSchema.safeParse(q);
    if (!range.success) {
      showToast(range.error.issues[0].message);
      return;
    }
    collapse();
    navigate({ pathname: "/buscar", search: `?${toSearchParams(q)}` });
  };

  const userMenu = <UserMenu user={user} />;

  return (
    <>
      <m.header
        initial={false}
        animate={{ height }}
        transition={reduceMotion ? { duration: 0 } : HEADER_SPRING}
        style={{ boxShadow }}
        className="fixed inset-x-0 top-0 z-40 overflow-hidden border-b border-border-soft bg-background"
      >
        {isDesktop ? (
          /* ---------- Escritorio ---------- */
          <>
            <div className="grid h-20 grid-cols-[1fr_auto_1fr] items-center gap-x-6 px-gutter">
              <Logo variant="responsive" className="justify-self-start" />

              <div className="relative flex h-full min-w-120 items-center justify-center">
                <AnimatePresence initial={false}>
                  {desktopOpen && (
                    <m.div key="tabs" {...TABS_MOTION} className="h-full">
                      <NavTabs items={MEMBER_NAV} className="gap-6 lg:gap-8" />
                    </m.div>
                  )}
                </AnimatePresence>
                <AnimatePresence initial={false}>
                  {!desktopOpen && (
                    <CompactSearch
                      key="compact"
                      value={query}
                      onPick={expand}
                      layoutId={SEARCH_LAYOUT_ID}
                      className="absolute inset-0 m-auto w-max"
                    />
                  )}
                </AnimatePresence>
              </div>

              <div className="flex items-center justify-end gap-1">
                {/* Es una invitación, no la acción principal: botón ghost. */}
                <Link
                  to="/perfil/anfitrion"
                  className={cn(
                    buttonVariants({ variant: "ghost" }),
                    "hidden h-11 px-3.5 text-sm xl:inline-flex",
                  )}
                >
                  Conviértete en anfitrión
                </Link>
                {userMenu}
              </div>
            </div>

            <div className="flex justify-center px-gutter pt-1">
              <AnimatePresence initial={false}>
                {desktopOpen && (
                  <SearchBar
                    key="search"
                    layoutId={SEARCH_LAYOUT_ID}
                    autoFocusField={focusField}
                    value={query}
                    onChange={setQuery}
                    onSubmit={submit}
                  />
                )}
              </AnimatePresence>
            </div>
          </>
        ) : (
          /* ---------- Móvil ---------- */
          <>
            <div className="relative flex h-18 items-center gap-3 px-gutter">
              <Logo variant="symbol" />
              <AnimatePresence initial={false}>
                {!expanded && (
                  <MobileSearchPill
                    key="pill"
                    value={query}
                    onPick={expand}
                    layoutId={SEARCH_LAYOUT_ID}
                    className="flex-1"
                  />
                )}
              </AnimatePresence>
              <AnimatePresence initial={false}>
                {expanded && (
                  <m.p
                    key="title"
                    initial={{ opacity: 0 }}
                    animate={{ opacity: 1 }}
                    exit={{ opacity: 0 }}
                    className="pointer-events-none absolute inset-x-0 text-center text-[15px] font-bold"
                  >
                    Busca tu vehículo
                  </m.p>
                )}
              </AnimatePresence>
              <div className="ml-auto">{userMenu}</div>
            </div>

            <AnimatePresence initial={false}>
              {showMobileTabs && (
                <m.div
                  key="tabs"
                  {...TABS_MOTION}
                  className="h-17 border-t border-border-soft px-gutter"
                >
                  <NavTabs items={MEMBER_NAV} className="justify-around" />
                </m.div>
              )}
            </AnimatePresence>

            <div className="absolute inset-x-0 top-18 px-gutter pt-1">
              <AnimatePresence initial={false}>
                {expanded && (
                  <SearchBar
                    key="search"
                    layoutId={SEARCH_LAYOUT_ID}
                    orientation="stack"
                    idPrefix="mq"
                    autoFocusField={focusField}
                    value={query}
                    onChange={setQuery}
                    onSubmit={submit}
                  />
                )}
              </AnimatePresence>
            </div>
          </>
        )}
      </m.header>

      {/* Reserva el espacio del header completo: el contenido empieza debajo */}
      <div aria-hidden="true" className="h-35 md:h-44" />

      {/* Fondo oscurecido mientras la búsqueda está desplegada */}
      <AnimatePresence>
        {expanded && (
          <m.div
            key="overlay"
            aria-hidden="true"
            onClick={collapse}
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.25 }}
            className="fixed inset-0 z-30 bg-black/25"
          />
        )}
      </AnimatePresence>
    </>
  );
}

// Header del Administrador en las páginas del marketplace que comparte (su perfil): sin búsqueda.
function AdminHeader({ user }: { user: User }) {
  const { scrollY } = useScroll();
  const boxShadow = useTransform(
    scrollY,
    [0, 80],
    ["0 1px 12px rgba(0,0,0,0)", "0 1px 12px rgba(0,0,0,0.08)"],
  );

  return (
    <>
      <m.header
        style={{ boxShadow }}
        className="fixed inset-x-0 top-0 z-40 border-b border-border-soft bg-background"
      >
        <div className="grid h-20 grid-cols-[auto_1fr_auto] items-center gap-x-4 px-gutter md:grid-cols-[1fr_auto_1fr]">
          <Logo
            variant="responsive"
            to="/admin"
            className="justify-self-start"
          />
          <NavTabs
            items={ADMIN_HEADER_NAV}
            className="justify-center gap-6 lg:gap-8"
          />
          <div className="flex justify-end">
            <UserMenu user={user} />
          </div>
        </div>
      </m.header>
      <div aria-hidden="true" className="h-20" />
    </>
  );
}
