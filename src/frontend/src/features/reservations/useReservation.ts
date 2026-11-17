import { useQuery } from "@tanstack/react-query";

import {
  getReservation,
  getReservationHistory,
  listReservationDocuments,
  listReservationReviews,
} from "@/api/wheelby.api";
import { features } from "@/lib/features";
import { queryKeys } from "@/lib/query-keys";

// Detalle de una reserva y lo que cuelga de ella. El Administrador lee por /admin/reservations.
export function useReservation(id: string, admin: boolean) {
  const reservation = useQuery({
    queryKey: queryKeys.reservation(id),
    queryFn: () => getReservation(id, admin),
    retry: (count, error) => count < 2 && !isClientError(error),
  });
  const ready = reservation.isSuccess;

  const history = useQuery({
    queryKey: queryKeys.reservationPart(id, "history"),
    queryFn: () => getReservationHistory(id, admin),
    enabled: ready && features.reservationHistory,
  });
  const documents = useQuery({
    queryKey: queryKeys.reservationPart(id, "documents"),
    queryFn: () => listReservationDocuments(id, admin),
    enabled: ready && features.reservationDocuments,
  });
  const reviews = useQuery({
    queryKey: queryKeys.reservationPart(id, "reviews"),
    queryFn: () => listReservationReviews(id, admin),
    enabled: ready && features.reviews,
  });

  return { reservation, history, documents, reviews };
}

function isClientError(error: unknown) {
  const status = (error as { response?: { status?: number } })?.response
    ?.status;
  return status !== undefined && status >= 400 && status < 500;
}
