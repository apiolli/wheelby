import { m } from "motion/react";
import { ChevronLeft, Star } from "lucide-react";
import { Link, useLocation, useParams } from "react-router";

import { toApiError } from "@/api/errors";
import { PageError } from "@/components/custom/ErrorStates";
import {
  Loading,
  Skeleton,
  TimelineSkeleton,
} from "@/components/custom/Skeletons";
import { NotFoundPage } from "@/features/errors/ErrorPages";
import { ReservationStatusBadge } from "@/components/custom/StatusBadges";
import StarRating from "@/features/marketplace/vehicle/components/StarRating";
import { features } from "@/lib/features";
import { formatDate, shortId } from "@/lib/format";
import { ENTER } from "@/lib/motion";
import { isAdmin, useSession } from "@/stores/session.store";
import { ReservationStatus, type Review } from "@/types/types";
import ActionsPanel from "./components/ActionsPanel";
import DocumentsCard from "./components/DocumentsCard";
import ReviewDialog from "./components/ReviewDialog";
import SuccessBanner from "./components/SuccessBanner";
import SummaryCard from "./components/SummaryCard";
import Timeline from "./components/Timeline";
import { sideOf } from "./rules";
import { useReservation } from "./useReservation";

// Detalle de una reserva (P1): línea de tiempo, acciones según estado y actor, resumen,
// documentos (P3) y reseña (P4). Lo ven las dos partes y el Administrador.
export default function ReservationPage() {
  const { id = "" } = useParams();
  const location = useLocation();
  const user = useSession((s) => s.user)!;
  const admin = isAdmin(user);
  const { reservation, history, documents, reviews } = useReservation(
    id,
    admin,
  );
  const justRequested = Boolean(
    (location.state as { requested?: boolean } | null)?.requested,
  );

  if (reservation.isPending) return <DetailSkeleton />;

  if (reservation.isError) {
    const status = toApiError(reservation.error).status;
    // 404 también para reservas ajenas: no se revela si existen.
    if (status === 404 || status === 403) return <NotFoundPage />;
    return (
      <PageError
        error={reservation.error}
        onRetry={() => reservation.refetch()}
      />
    );
  }

  const r = reservation.data;
  const side = sideOf(r, user.id);
  const myReview = reviews.data?.find((rv) => rv.authorId === user.id);
  const canReview =
    features.reviews &&
    side !== "viewer" &&
    r.status === ReservationStatus.Completed &&
    reviews.isSuccess &&
    !myReview;

  return (
    <main className="px-gutter pt-4 pb-16">
      <div className="mx-auto grid max-w-280 gap-6">
        <Link
          to={
            admin
              ? "/admin/reservas"
              : side === "host"
                ? "/anfitrion/reservas"
                : "/perfil/reservas"
          }
          className="inline-flex w-fit items-center gap-1 rounded-full py-1 pr-2 text-[13.5px] font-semibold text-muted-foreground outline-none hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring"
        >
          <ChevronLeft className="size-4" />{" "}
          {admin
            ? "Reservas"
            : side === "host"
              ? "Reservas recibidas"
              : "Mis reservas"}
        </Link>

        {justRequested && (
          <SuccessBanner>
            Solicitud enviada. El anfitrión la revisará pronto.
          </SuccessBanner>
        )}

        <m.header {...ENTER} className="flex flex-wrap items-center gap-3">
          <h1 className="text-[26px] leading-tight font-bold tracking-tight md:text-[30px]">
            Reserva #{shortId(r.id)}
          </h1>
          <ReservationStatusBadge
            status={r.status}
            className="px-3 py-1.5 text-[12.5px]"
          />
          <p className="w-full text-[14px] text-muted-foreground">
            Solicitada el {formatDate(r.createdAt)}
            {side === "renter"
              ? " · eres el arrendatario"
              : side === "host"
                ? " · eres el anfitrión"
                : ""}
          </p>
        </m.header>

        <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_400px] lg:gap-14">
          <div className="grid content-start gap-10">
            <section aria-labelledby="timeline-title" className="grid gap-5">
              <h2
                id="timeline-title"
                className="text-xl font-bold tracking-tight"
              >
                Línea de tiempo
              </h2>
              {features.reservationHistory && history.isPending ? (
                <TimelineSkeleton />
              ) : (
                <m.div
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  transition={{ duration: 0.25 }}
                >
                  <Timeline
                    reservation={r}
                    history={history.data}
                    side={side}
                  />
                </m.div>
              )}
            </section>

            <section
              aria-labelledby="actions-title"
              className="grid gap-4 border-t border-border-soft pt-8"
            >
              <h2
                id="actions-title"
                className="text-xl font-bold tracking-tight"
              >
                Acciones
              </h2>
              <ActionsPanel reservation={r} side={side} />
            </section>

            {features.reviews && r.status === ReservationStatus.Completed && (
              <section
                aria-labelledby="review-title"
                className="grid gap-4 border-t border-border-soft pt-8"
              >
                <h2
                  id="review-title"
                  className="text-xl font-bold tracking-tight"
                >
                  Reseñas
                </h2>
                {reviews.data?.map((rv) => (
                  <ReviewItem
                    key={rv.id}
                    review={rv}
                    mine={rv.authorId === user.id}
                  />
                ))}
                {canReview ? (
                  <div className="flex flex-wrap items-center gap-4">
                    <ReviewDialog reservation={r} side={side} />
                    <p className="text-[13.5px] text-muted-foreground">
                      Ayuda a otros con tu experiencia.
                    </p>
                  </div>
                ) : (
                  reviews.isSuccess &&
                  reviews.data.length === 0 && (
                    <p className="text-[14px] text-muted-foreground">
                      Aún nadie dejó una reseña.
                    </p>
                  )
                )}
              </section>
            )}
          </div>

          <aside className="grid content-start gap-6 lg:sticky lg:top-28 lg:self-start">
            <SummaryCard reservation={r} side={side} />
            {features.reservationDocuments && (
              <DocumentsCard
                reservationId={r.id}
                status={r.status}
                side={side}
                documents={documents.data}
                isPending={documents.isPending}
                admin={admin}
              />
            )}
          </aside>
        </div>
      </div>
    </main>
  );
}

function ReviewItem({ review, mine }: { review: Review; mine: boolean }) {
  const who = mine
    ? "Tu reseña"
    : review.authorRole === "Renter"
      ? "Reseña del arrendatario"
      : "Reseña del anfitrión";
  return (
    <article className="rounded-[18px] border border-border-soft p-5">
      <div className="flex items-center justify-between gap-3">
        <p className="flex items-center gap-2 text-[13.5px] font-semibold">
          <Star
            className="size-4 text-highlight"
            fill="currentColor"
            strokeWidth={1.5}
            aria-hidden="true"
          />
          {who}
        </p>
        <StarRating value={review.rating} />
      </div>
      {review.comment && (
        <p className="mt-2 text-[14.5px] leading-relaxed">{review.comment}</p>
      )}
      <p className="mt-2 text-[12.5px] text-muted-foreground">
        {formatDate(review.createdAt)}
      </p>
    </article>
  );
}

function DetailSkeleton() {
  return (
    <Loading label="Cargando reserva" className="px-gutter pt-12 pb-16">
      <div className="mx-auto grid max-w-280 gap-8">
        <Skeleton className="h-9 w-72 rounded-full" />
        <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_400px] lg:gap-14">
          <TimelineSkeleton />
          <Skeleton className="h-80 rounded-[20px]" />
        </div>
      </div>
    </Loading>
  );
}
