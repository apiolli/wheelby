import { useRouteError } from "react-router";

import { PageError } from "@/components/custom/ErrorStates";

// Error inesperado al pintar una ruta (o al cargar su código): página de error con reintento.
export default function RouteError() {
  const error = useRouteError();
  return <PageError error={error} onRetry={() => window.location.reload()} />;
}
