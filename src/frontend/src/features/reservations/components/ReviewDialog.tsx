import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { CircleAlert, Lock } from "lucide-react";

import { errorMessage } from "@/api/errors";
import { createReview } from "@/api/wheelby.api";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { queryKeys } from "@/lib/query-keys";
import { showToast } from "@/stores/session.store";
import type { Reservation } from "@/types/types";
import type { Side } from "../rules";
import StarInput from "./StarInput";

const MAX_COMMENT = 500;

// P4: reseña de una reserva completada. Una por persona y no se puede editar después.
export default function ReviewDialog({
  reservation,
  side,
}: {
  reservation: Reservation;
  side: Side;
}) {
  const queryClient = useQueryClient();
  const [open, setOpen] = useState(false);
  const [rating, setRating] = useState(0);
  const [comment, setComment] = useState("");

  const publish = useMutation({
    mutationFn: () => createReview(reservation.id, rating, comment),
    onSuccess: () => {
      showToast("Publicaste tu reseña");
      setOpen(false);
      queryClient.invalidateQueries({
        queryKey: queryKeys.reservationPart(reservation.id, "reviews"),
      });
      queryClient.invalidateQueries({
        queryKey: queryKeys.vehicleReviews(reservation.vehicleId),
      });
    },
  });

  const subject =
    side === "renter" ? "el vehículo y el anfitrión" : "al arrendatario";

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        if (publish.isPending) return;
        setOpen(next);
        if (!next) publish.reset();
      }}
    >
      <DialogTrigger render={<Button size="lg" className="rounded-full" />}>
        Dejar reseña
      </DialogTrigger>
      <DialogContent className="max-w-120">
        <DialogHeader>
          <DialogTitle>Deja tu reseña</DialogTitle>
        </DialogHeader>
        <form
          className="grid gap-5 p-6"
          onSubmit={(e) => {
            e.preventDefault();
            if (rating > 0 && comment.length <= MAX_COMMENT) publish.mutate();
          }}
        >
          <DialogDescription className="text-[14.5px] text-muted-foreground">
            Cuéntanos cómo te fue con {subject}.
          </DialogDescription>
          <div className="grid gap-2">
            <p id="rating-label" className="text-sm font-semibold">
              Calificación
            </p>
            <StarInput
              value={rating}
              onChange={setRating}
              labelledBy="rating-label"
            />
          </div>
          <div className="grid gap-1.5">
            <Label htmlFor="review-comment" className="text-sm font-semibold">
              Comentario{" "}
              <span className="font-normal text-muted-foreground">
                (opcional)
              </span>
            </Label>
            <Textarea
              id="review-comment"
              value={comment}
              onChange={(e) => setComment(e.target.value)}
              aria-invalid={comment.length > MAX_COMMENT || undefined}
              placeholder="¿Qué destacarías?"
            />
            <p
              aria-live="polite"
              className={
                comment.length > MAX_COMMENT
                  ? "text-right text-[12.5px] text-destructive tabular-nums"
                  : "text-right text-[12.5px] text-muted-foreground tabular-nums"
              }
            >
              {comment.length}/{MAX_COMMENT}
            </p>
          </div>
          {publish.isError && (
            <Alert variant="destructive">
              <CircleAlert />
              <AlertDescription>{errorMessage(publish.error)}</AlertDescription>
            </Alert>
          )}
          <p className="flex items-center gap-2 text-[13px] text-muted-foreground">
            <Lock className="size-4 shrink-0" strokeWidth={1.75} />
            No podrás editarla después.
          </p>
          <Button
            type="submit"
            size="lg"
            className="rounded-full"
            disabled={
              rating === 0 || comment.length > MAX_COMMENT || publish.isPending
            }
            aria-busy={publish.isPending}
          >
            {publish.isPending ? "Publicando…" : "Publicar reseña"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
}
