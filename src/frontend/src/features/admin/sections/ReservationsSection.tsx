import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { m } from "motion/react";
import { CircleAlert, Inbox } from "lucide-react";
import { useSearchParams } from "react-router";

import { listAllReservations } from "@/api/admin.api";
import { errorMessage } from "@/api/errors";
import Pagination from "@/components/custom/Pagination";
import { ReservationStatusBadge } from "@/components/custom/StatusBadges";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { formatDate, formatDateRange, shortId } from "@/lib/format";
import { queryKeys } from "@/lib/query-keys";
import { pageParam } from "@/lib/schemas";
import AdminHeading from "../components/AdminHeading";
import FilterSelect from "../components/FilterSelect";
import { findFilter, RESERVATION_FILTERS } from "../filters";
import { useUserLookup } from "../hooks/useAllUsers";

const PAGE_SIZE = 20;

// Todas las reservas, solo de consulta: el Administrador no participa en ellas. Filtro y página
// en la URL (?estado=pendiente&pagina=2).
export default function ReservationsSection() {
  const [params, setParams] = useSearchParams();
  const filter = findFilter(RESERVATION_FILTERS, params.get("estado"));
  const page = pageParam(params);
  const lookup = useUserLookup();

  const reservations = useQuery({
    queryKey: queryKeys.adminReservations(filter.value, page),
    queryFn: () => listAllReservations(filter.value, page, PAGE_SIZE),
    placeholderData: keepPreviousData,
  });

  const setFilter = (slug: string) => {
    const next = new URLSearchParams(params);
    if (slug === "todas") next.delete("estado");
    else next.set("estado", slug);
    next.delete("pagina");
    setParams(next, { replace: true });
  };

  const items = reservations.data?.items ?? [];

  return (
    <>
      <AdminHeading
        title="Reservas"
        description="Todas las reservas del marketplace y su estado."
      >
        <div className="flex items-center gap-3">
          {reservations.data && (
            <span
              className="text-[13px] text-muted-foreground"
              aria-live="polite"
            >
              {reservations.data.totalItems}{" "}
              {reservations.data.totalItems === 1 ? "reserva" : "reservas"}
            </span>
          )}
          <FilterSelect
            label="Filtrar por estado"
            value={filter.slug}
            options={RESERVATION_FILTERS}
            onChange={setFilter}
          />
        </div>
      </AdminHeading>

      {reservations.isError ? (
        <Alert variant="destructive">
          <CircleAlert />
          <AlertDescription>
            {errorMessage(reservations.error)}
          </AlertDescription>
        </Alert>
      ) : (
        <m.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.4 }}
          className="overflow-hidden rounded-2xl border border-border-soft bg-background"
        >
          {reservations.isPending ? (
            <div className="grid gap-px" aria-busy="true">
              {Array.from({ length: 6 }, (_, i) => (
                <div key={i} className="h-14 animate-pulse bg-secondary/60" />
              ))}
            </div>
          ) : items.length === 0 ? (
            <div className="flex flex-col items-center px-6 py-14 text-center">
              <Inbox
                className="size-6 text-muted-foreground"
                strokeWidth={1.6}
              />
              <p className="mt-3 text-[14px] text-muted-foreground">
                No hay reservas{" "}
                {filter.value === undefined ? "todavía" : "con este estado"}.
              </p>
            </div>
          ) : (
            <Table
              style={{ opacity: reservations.isPlaceholderData ? 0.6 : 1 }}
            >
              <TableHeader className="bg-secondary/60">
                <TableRow>
                  <TableHead>Reserva</TableHead>
                  <TableHead className="hidden md:table-cell">
                    Arrendatario
                  </TableHead>
                  <TableHead>Fechas</TableHead>
                  <TableHead>Estado</TableHead>
                  <TableHead className="hidden lg:table-cell">
                    Solicitada
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {items.map((r) => {
                  const renter = lookup(r.renterId);
                  return (
                    <TableRow key={r.id}>
                      <TableCell>
                        <span className="block font-semibold">
                          #{shortId(r.id)}
                        </span>
                        <span className="block text-[12.5px] text-muted-foreground">
                          Vehículo #{shortId(r.vehicleId)}
                        </span>
                      </TableCell>
                      <TableCell className="hidden md:table-cell">
                        <span className="block truncate">
                          {renter?.fullName ?? `Cuenta #${shortId(r.renterId)}`}
                        </span>
                        {renter && (
                          <span className="block truncate text-[12.5px] text-muted-foreground">
                            {renter.email}
                          </span>
                        )}
                      </TableCell>
                      <TableCell className="whitespace-nowrap">
                        {formatDateRange(r.startDate, r.endDate)}
                      </TableCell>
                      <TableCell>
                        <ReservationStatusBadge status={r.status} />
                      </TableCell>
                      <TableCell className="hidden whitespace-nowrap text-muted-foreground lg:table-cell">
                        {formatDate(r.createdAt)}
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </m.div>
      )}
      {reservations.data && (
        <Pagination
          page={reservations.data.page}
          totalPages={reservations.data.totalPages}
          className="mt-6"
        />
      )}
    </>
  );
}
