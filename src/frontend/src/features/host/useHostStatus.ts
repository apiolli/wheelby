import { useQuery } from "@tanstack/react-query";

import { listMyRequests } from "@/api/wheelby.api";
import { hostStatus, latestHostRequest } from "@/lib/permissions";
import { queryKeys } from "@/lib/query-keys";

// "Anfitrión" no es un rol: es el permiso wheelby.publish-vehicle. Su estado sale de la última
// solicitud de ese permiso.
export function useHostStatus() {
  const requests = useQuery({
    queryKey: queryKeys.myRequests,
    queryFn: () => listMyRequests(1, 100),
    staleTime: 30_000,
  });
  const request = requests.data
    ? latestHostRequest(requests.data.items)
    : undefined;
  return { ...requests, request, status: hostStatus(request) };
}
