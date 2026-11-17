import { useQuery } from "@tanstack/react-query";

import { listUsers } from "@/api/admin.api";
import { queryKeys } from "@/lib/query-keys";
import type { UserSummary } from "@/types/types";

const PAGE_SIZE = 100; // máximo que acepta la API
const MAX_PAGES = 20;

// La API pagina pero no busca ni filtra: se traen todas las cuentas (de 100 en 100) para que la
// búsqueda, los filtros y las métricas cubran a todas, no solo a la primera página.
async function fetchAllUsers(): Promise<UserSummary[]> {
  const all: UserSummary[] = [];
  for (let page = 1; page <= MAX_PAGES; page++) {
    const res = await listUsers(page, PAGE_SIZE);
    all.push(...res.items);
    if (!res.hasNextPage) break;
  }
  return all;
}

export function useAllUsers() {
  return useQuery({ queryKey: queryKeys.adminUsers(), queryFn: fetchAllUsers });
}

// Nombre de una cuenta por id (para solicitudes y reservas, que solo traen el id).
export function useUserLookup() {
  const { data } = useAllUsers();
  const byId = new Map(data?.map((u) => [u.id, u]));
  return (id: string) => byId.get(id);
}
