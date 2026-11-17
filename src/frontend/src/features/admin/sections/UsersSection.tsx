import { useState } from "react";
import { m } from "motion/react";
import { ChevronRight, CircleAlert, Search, SearchX } from "lucide-react";
import { useSearchParams } from "react-router";

import { errorMessage } from "@/api/errors";
import Pagination from "@/components/custom/Pagination";
import { RoleBadge, UserStatusBadge } from "@/components/custom/StatusBadges";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { formatDate, initial } from "@/lib/format";
import { pageParam } from "@/lib/schemas";
import { useSession } from "@/stores/session.store";
import AdminHeading from "../components/AdminHeading";
import FilterSelect from "../components/FilterSelect";
import UserSheet from "../components/UserSheet";
import { findFilter, ROLE_FILTERS, STATUS_FILTERS } from "../filters";
import { useAllUsers } from "../hooks/useAllUsers";

const PAGE_SIZE = 20;

const normalize = (s: string) =>
  s.toLowerCase().normalize("NFD").replace(/[̀-ͯ]/g, "");

// Usuarios: búsqueda (sin distinguir acentos), filtros y página en la URL
// (?q=&rol=estandar|admin&estado=activa|sin-activar|desactivada&pagina=). La cuenta abierta en el
// panel lateral también va en la URL (?cuenta=<id>).
export default function UsersSection() {
  const me = useSession((s) => s.user);
  const [params, setParams] = useSearchParams();
  const users = useAllUsers();

  const query = params.get("q") ?? "";
  const roleFilter = findFilter(ROLE_FILTERS, params.get("rol"));
  const statusFilter = findFilter(STATUS_FILTERS, params.get("estado"));
  const selectedId = params.get("cuenta");
  // Se conserva al cerrar para que el panel no se vacíe durante su animación de salida.
  const [lastSelected, setLastSelected] = useState(selectedId);
  if (selectedId && selectedId !== lastSelected) setLastSelected(selectedId);

  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(params);
    if (value && value !== "todos") next.set(key, value);
    else next.delete(key);
    // Al filtrar se vuelve a la primera página.
    if (key !== "cuenta") next.delete("pagina");
    setParams(next, { replace: key !== "cuenta" });
  };

  const q = normalize(query.trim());
  const rows = (users.data ?? []).filter((u) => {
    if (roleFilter.value && u.role !== roleFilter.value) return false;
    if (statusFilter.value && u.status !== statusFilter.value) return false;
    return (
      !q ||
      normalize(u.fullName).includes(q) ||
      u.email.toLowerCase().includes(q)
    );
  });
  const filtered = Boolean(q || roleFilter.value || statusFilter.value);
  const totalPages = Math.max(1, Math.ceil(rows.length / PAGE_SIZE));
  const page = Math.min(pageParam(params), totalPages);
  const visible = rows.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const selected = users.data?.find(
    (u) => u.id === (selectedId ?? lastSelected),
  );

  return (
    <>
      <AdminHeading
        title="Usuarios"
        description="Consulta las cuentas y gestiona su rol, su estado y su acceso."
      />

      <div className="mb-4 flex flex-col gap-3 lg:flex-row lg:items-center">
        <label className="relative flex-1 lg:max-w-90">
          <span className="sr-only">Buscar por nombre o correo</span>
          <Search
            className="pointer-events-none absolute top-1/2 left-3.5 size-4 -translate-y-1/2 text-muted-foreground"
            strokeWidth={2}
          />
          <Input
            type="search"
            placeholder="Buscar por nombre o correo"
            value={query}
            onChange={(e) => setParam("q", e.target.value)}
            className="h-10 rounded-full pl-10"
          />
        </label>
        <div className="flex gap-2">
          <FilterSelect
            label="Filtrar por rol"
            value={roleFilter.slug}
            options={ROLE_FILTERS}
            onChange={(v) => setParam("rol", v)}
          />
          <FilterSelect
            label="Filtrar por estado"
            value={statusFilter.slug}
            options={STATUS_FILTERS}
            onChange={(v) => setParam("estado", v)}
          />
        </div>
        {users.data && (
          <p
            className="text-[13px] text-muted-foreground lg:ml-auto"
            aria-live="polite"
          >
            {rows.length} {rows.length === 1 ? "usuario" : "usuarios"}
          </p>
        )}
      </div>

      {users.isError ? (
        <Alert variant="destructive">
          <CircleAlert />
          <AlertDescription>{errorMessage(users.error)}</AlertDescription>
        </Alert>
      ) : (
        <m.div
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ duration: 0.4 }}
          className="overflow-hidden rounded-2xl border border-border-soft bg-background"
        >
          {users.isPending ? (
            <div className="grid gap-px" aria-busy="true">
              {Array.from({ length: 6 }, (_, i) => (
                <div key={i} className="h-14 animate-pulse bg-secondary/60" />
              ))}
            </div>
          ) : visible.length === 0 ? (
            <div className="flex flex-col items-center px-6 py-14 text-center">
              <SearchX
                className="size-6 text-muted-foreground"
                strokeWidth={1.6}
              />
              <p className="mt-3 font-semibold">
                No hay usuarios con estos filtros
              </p>
              <p className="mt-1 text-[14px] text-muted-foreground">
                Prueba con otro nombre o quita algún filtro.
              </p>
              {filtered && (
                <Button
                  variant="outline"
                  size="sm"
                  className="mt-4"
                  onClick={() => setParams({}, { replace: true })}
                >
                  Limpiar filtros
                </Button>
              )}
            </div>
          ) : (
            <Table>
              <TableHeader className="bg-secondary/60">
                <TableRow>
                  <TableHead>Nombre</TableHead>
                  <TableHead className="hidden md:table-cell">Correo</TableHead>
                  <TableHead className="hidden sm:table-cell">Rol</TableHead>
                  <TableHead className="max-sm:pl-2">Estado</TableHead>
                  <TableHead className="hidden lg:table-cell">
                    Registro
                  </TableHead>
                  <TableHead
                    className="hidden w-10 sm:table-cell"
                    aria-hidden="true"
                  />
                </TableRow>
              </TableHeader>
              <TableBody>
                {visible.map((u) => (
                  <TableRow
                    key={u.id}
                    // La fila entera abre el panel con el ratón o el dedo.
                    onClick={() => setParam("cuenta", u.id)}
                    data-state={selectedId === u.id ? "selected" : undefined}
                    className="cursor-pointer hover:bg-secondary/60"
                  >
                    <TableCell className="max-sm:pr-2">
                      {/* El nombre es el botón que abre el panel (teclado incluido). */}
                      <button
                        type="button"
                        onClick={(e) => {
                          e.stopPropagation();
                          setParam("cuenta", u.id);
                        }}
                        aria-label={`Ver detalles de ${u.fullName}`}
                        className="-m-1 flex max-w-[60vw] items-center gap-3 rounded-lg p-1 text-left outline-none focus-visible:ring-2 focus-visible:ring-ring sm:max-w-none"
                      >
                        <span className="grid size-8 shrink-0 place-items-center rounded-full bg-primary-soft text-[13px] font-bold text-primary">
                          {initial(u.fullName)}
                        </span>
                        <span className="min-w-0">
                          <span className="block truncate font-semibold">
                            {u.fullName}
                            {u.id === me?.id && (
                              <span className="ml-1.5 font-normal text-muted-foreground">
                                (tú)
                              </span>
                            )}
                          </span>
                          <span className="block truncate text-[12.5px] text-muted-foreground md:hidden">
                            {u.email}
                            {u.role === "Administrator" && (
                              <span className="font-semibold text-primary sm:hidden">
                                {" "}
                                · Admin
                              </span>
                            )}
                          </span>
                        </span>
                      </button>
                    </TableCell>
                    <TableCell className="hidden text-muted-foreground md:table-cell">
                      {u.email}
                    </TableCell>
                    <TableCell className="hidden sm:table-cell">
                      <RoleBadge role={u.role} />
                    </TableCell>
                    <TableCell className="max-sm:pl-2">
                      <UserStatusBadge status={u.status} />
                    </TableCell>
                    <TableCell className="hidden whitespace-nowrap text-muted-foreground lg:table-cell">
                      {formatDate(u.createdAt)}
                    </TableCell>
                    <TableCell
                      className="hidden pr-4 text-muted-foreground sm:table-cell"
                      aria-hidden="true"
                    >
                      <ChevronRight className="size-4" strokeWidth={2} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </m.div>
      )}
      <Pagination page={page} totalPages={totalPages} className="mt-6" />

      <UserSheet
        user={selected}
        open={Boolean(selectedId)}
        onOpenChange={(open) => !open && setParam("cuenta", null)}
      />
    </>
  );
}
