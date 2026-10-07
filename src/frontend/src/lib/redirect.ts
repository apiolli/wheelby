const AUTH_PATHS = ["/login", "/registro", "/recuperar"];

// Ruta interna a la que volver tras iniciar sesión (?redirect=). Solo rutas propias que empiecen
// por "/" (nunca "//dominio" ni una URL externa) y que no sean otra página de acceso.
export function safeRedirect(search: string) {
  const target = new URLSearchParams(search).get("redirect");

  if (!target || !target.startsWith("/") || target.startsWith("//")) return "/";
  const path = target.split(/[?#]/)[0];

  return AUTH_PATHS.includes(path) ? "/" : target;
}

export const loginPath = (from: string) =>
  from === "/" ? "/login" : `/login?redirect=${encodeURIComponent(from)}`;
