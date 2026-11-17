import { useState, type FormEvent } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { CircleAlert, CircleCheck } from "lucide-react";

import { errorMessage } from "@/api/errors";
import { publishVehicle } from "@/api/wheelby.api";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { FloatingField } from "@/features/auth/shared/components/FloatingField";
import { SubmitButton } from "@/features/auth/shared/components/SubmitButton";
import { fieldErrors, vehicleSchema } from "@/lib/schemas";

const EMPTY = { brand: "", model: "", year: "", plate: "", pricePerDay: "" };

// Publicar un vehículo (POST /vehicles). Las reglas de Zod son las mismas que valida el backend.
export default function PublishVehicleForm() {
  const queryClient = useQueryClient();
  const [values, setValues] = useState(EMPTY);
  const [touched, setTouched] = useState(false);
  const [published, setPublished] = useState<string | null>(null);
  const invalid = touched ? fieldErrors(vehicleSchema, values) : {};

  const publish = useMutation({
    mutationFn: publishVehicle,
    onSuccess: (_, input) => {
      setPublished(`${input.brand} ${input.model}`);
      setValues(EMPTY);
      setTouched(false);
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
    },
  });

  const submit = (e: FormEvent) => {
    e.preventDefault();
    setTouched(true);
    const parsed = vehicleSchema.safeParse(values);
    if (parsed.success) publish.mutate(parsed.data);
  };

  const field = (key: keyof typeof EMPTY) => ({
    id: `vehicle-${key}`,
    value: values[key],
    onChange: (e: React.ChangeEvent<HTMLInputElement>) => {
      setPublished(null);
      if (publish.isError) publish.reset();
      setValues((v) => ({ ...v, [key]: e.target.value }));
    },
    invalid: Boolean(invalid[key]),
    hint: invalid[key],
  });

  return (
    <section
      className="mt-8 rounded-2xl border border-border p-6 md:p-8"
      aria-labelledby="publish-title"
    >
      <h2 id="publish-title" className="mb-1 text-lg font-bold tracking-tight">
        Publica un vehículo
      </h2>
      <p className="mb-5 text-[14px] text-muted-foreground">
        Aparecerá en el catálogo para las fechas en que esté libre. El precio va
        en pesos dominicanos (DOP).
      </p>

      <form onSubmit={submit} noValidate className="max-w-140">
        {publish.isError && (
          <Alert variant="destructive" className="mb-4">
            <CircleAlert />
            <AlertDescription>{errorMessage(publish.error)}</AlertDescription>
          </Alert>
        )}
        {published && (
          <Alert variant="success" role="status" className="mb-4">
            <CircleCheck />
            <AlertDescription>
              Publicamos tu {published}. Ya está en el catálogo.
            </AlertDescription>
          </Alert>
        )}

        <div className="grid gap-x-3 sm:grid-cols-2">
          <FloatingField label="Marca" autoComplete="off" {...field("brand")} />
          <FloatingField
            label="Modelo"
            autoComplete="off"
            {...field("model")}
          />
          <FloatingField
            label="Año"
            inputMode="numeric"
            maxLength={4}
            {...field("year")}
          />
          <FloatingField
            label="Placa"
            autoComplete="off"
            className="uppercase"
            maxLength={10}
            {...field("plate")}
          />
          <FloatingField
            label="Precio por día (DOP)"
            inputMode="decimal"
            {...field("pricePerDay")}
          />
        </div>

        <SubmitButton
          loading={publish.isPending}
          className="mt-2 sm:w-auto sm:px-8"
        >
          Publicar vehículo
        </SubmitButton>
      </form>
    </section>
  );
}
