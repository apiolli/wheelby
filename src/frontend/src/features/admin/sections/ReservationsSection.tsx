import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { Inbox } from "lucide-react";
import { Link, useNavigate, useSearchParams } from "react-router";

import { listAllReservations } from "@/api/admin.api";
import { SectionError } from "@/components/custom/ErrorStates";
import Pagination from "@/components/custom/Pagination";
import { FadeIn, TableSkeleton } from "@/components/custom/Skeletons";
import { ReservationStatusBadge } from "@/components/custom/StatusBadges";
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
// en la URL (?estado=solicitada&pagina=2).
export default function ReservationsSection() {
  const [params, setParams] = useSearchParams();
  const filter = findFilter(RESERVATION_FILTERS, params.get("estado"));
  const page = pageParam(params);
  const lookup = useUserLookup();
  const navigate = useNavigate();

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
        <SectionError
          error={reservations.error}
          onRetry={() => reservations.refetch()}
          className="bg-background"
        />
      ) : (
        <div className="overflow-hidden rounded-2xl border border-border-soft bg-background">
          {reservations.isPending ? (
            <TableSkeleton rows={6} columns={6} />
          ) : items.length === 0 ? (
            <div className="flex flex-col items-center px-6 py-14 text-center">
              <Inbox
                className="size-6 text-muted-foreground"
                strokeWidth={1.6}
              />
              <p className="mt-3 text-[14px] text-muted-foreground">
                {filter.value === undefined
                  ? "Todavía no hay reservas."
                  : "No hay reservas con ese estado."}
              </p>
            </div>
          ) : (
            <FadeIn>
              <Table
                style={{ opacity: reservations.isPlaceholderData ? 0.6 : 1 }}
              >
                <TableHeader className="bg-secondary/60">
                  <TableRow>
                    <TableHead>ID</TableHead>
                    <TableHead>Vehículo</TableHead>
                    <TableHead className="hidden md:table-cell">
                      Arrendatario
                    </TableHead>
                    <TableHead className="hidden lg:table-cell">
                      Anfitrión
                    </TableHead>
                    <TableHead>Fechas</TableHead>
                    <TableHead>Estado</TableHead>
                    <TableHead className="hidden xl:table-cell">
                      Creada el
                    </TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {items.map((r) => (
                    // Solo lectura: la fila abre el detalle; el Administrador no ejecuta transiciones.
                    <TableRow
                      key={r.id}
                      onClick={() => navigate(`/reservas/${r.id}`)}
                      className="cursor-pointer"
                    >
                      <TableCell>
                        <Link
                          to={`/reservas/${r.id}`}
                          onClick={(e) => e.stopPropagation()}
                          className="font-semibold underline-offset-4 outline-none hover:underline focus-visible:ring-2 focus-visible:ring-ring"
                        >
                          #{shortId(r.id)}
                        </Link>
                      </TableCell>
                      <TableCell className="whitespace-nowrap">
                        {r.brand} {r.model}
                        <span className="block text-[12.5px] text-muted-foreground">
                          {r.year}
                        </span>
                      </TableCell>
                      <TableCell className="hidden md:table-cell">
                        <Person user={lookup(r.renterId)} id={r.renterId} />
                      </TableCell>
                      <TableCell className="hidden lg:table-cell">
                        <Person user={lookup(r.ownerId)} id={r.ownerId} />
                      </TableCell>
                      <TableCell className="whitespace-nowrap">
                        {formatDateRange(r.startDate, r.endDate)}
                      </TableCell>
                      <TableCell>
                        <ReservationStatusBadge status={r.status} />
                      </TableCell>
                      <TableCell className="hidden whitespace-nowrap text-muted-foreground xl:table-cell">
                        {formatDate(r.createdAt)}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </FadeIn>
          )}
        </div>
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

function Person({
  user,
  id,
}: {
  user: { fullName: string; email: string } | undefined;
  id: string;
}) {
  return (
    <>
      <span className="block max-w-48 truncate">
        {user?.fullName ?? `Cuenta #${shortId(id)}`}
      </span>
      {user && (
        <span className="block max-w-48 truncate text-[12.5px] text-muted-foreground">
          {user.email}
        </span>
      )}
    </>
  );
}
