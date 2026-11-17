import { useEffect } from "react";
import { useLocation } from "react-router";

// Cada página nueva empieza arriba. Cambiar solo la query (filtros, diálogos) no mueve el scroll.
export default function ScrollToTop() {
  const { pathname } = useLocation();
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);
  return null;
}
