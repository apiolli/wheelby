import { useDeferredValue, useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { AnimatePresence, m } from "motion/react";
import { ChevronLeft, CircleAlert, LoaderCircle, Lock } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router";
import { cn } from "cn";

import { errorCode, ERROR_CODES, errorMessage, toApiError } from "@/api/errors";
import { PageError } from "@/components/custom/ErrorStates";
import { Loading, Skeleton } from "@/components/custom/Skeletons";
import { NotFoundPage } from "@/features/errors/ErrorPages";
import { getVehicle, publishVehicle, updateVehicle } from "@/api/wheelby.api";
import FormField from "@/components/custom/FormField";
import { inputClass } from "@/lib/form";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import VehicleCard from "@/features/marketplace/catalog/components/VehicleCard";
import { features } from "@/lib/features";
import { queryKeys } from "@/lib/query-keys";
import { fieldErrors } from "@/lib/schemas";
import { showToast, useSession } from "@/stores/session.store";
import type { Vehicle } from "@/types/types";
import PhotosField from "./PhotosField";
import { usePhotoUploads } from "./usePhotoUploads";
import {
  CITY_MAX,
  DESCRIPTION_MAX,
  EMPTY_VEHICLE,
  VEHICLE_TYPES,
  valuesFrom,
  vehicleFormSchema,
  type VehicleFormValues,
} from "./vehicleForm";

// Estado que Mis vehículos lee para mostrar la confirmación.
export interface VehicleSavedState {
  saved: { name: string; created: boolean };
}

// Publicar (/anfitrion/vehiculos/nuevo) o editar (/anfitrion/vehiculos/:id, P4) un vehículo.
export default function VehicleFormPage() {
  const { id } = useParams();
  const user = useSession((s) => s.user)!;
  const existing = useQuery({
    queryKey: queryKeys.vehicle(id ?? ""),
    queryFn: () => getVehicle(id!),
    enabled: Boolean(id),
  });

  if (!id) return <VehicleForm />;
  if (existing.isPending)
    return (
      <Loading label="Cargando vehículo" className="px-gutter pt-12">
        <div className="mx-auto grid max-w-280 gap-6">
          <Skeleton className="h-9 w-64 rounded-full" />
          <Skeleton className="h-64 max-w-180 rounded-[20px]" />
        </div>
      </Loading>
    );
  // Inexistente o ajeno: 404, sin revelar si existe. Otro fallo: error de página.
  if (existing.isError) {
    if (errorCode(existing.error) === ERROR_CODES.vehicleNotFound)
      return <NotFoundPage />;
    return (
      <PageError error={existing.error} onRetry={() => existing.refetch()} />
    );
  }
  if (existing.data.ownerId !== user.id) return <NotFoundPage />;
  return <VehicleForm vehicle={existing.data} />;
}

function VehicleForm({ vehicle }: { vehicle?: Vehicle }) {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const user = useSession((s) => s.user)!;
  const editing = Boolean(vehicle);
  const [values, setValues] = useState<VehicleFormValues>(() =>
    vehicle ? valuesFrom(vehicle) : EMPTY_VEHICLE,
  );
  const [touched, setTouched] = useState<
    Partial<Record<keyof VehicleFormValues, boolean>>
  >({});
  const [submitted, setSubmitted] = useState(false);

  const errors = fieldErrors(vehicleFormSchema, values);
  const shown = (key: keyof VehicleFormValues) =>
    submitted || touched[key] ? errors[key] : undefined;

  const photos = usePhotoUploads(vehicle?.id);
  const [uploadingPhotos, setUploadingPhotos] = useState(false);

  // Publica o guarda los datos; devuelve el id del vehículo.
  async function saveVehicle(): Promise<string> {
    const data = vehicleFormSchema.parse(values);
    const input = {
      ...data,
      city: data.city || null,
      description: data.description || null,
    };
    if (!features.vehicleDetails) {
      const { brand, model, year, plate, pricePerDay } = data;
      if (vehicle) {
        await updateVehicle(vehicle.id, {
          brand,
          model,
          year,
          plate: vehicle.plate,
          pricePerDay,
          type: vehicle.type,
        });
        return vehicle.id;
      }
      return (await publishVehicle({ brand, model, year, plate, pricePerDay }))
        .id;
    }
    // La placa no se edita: se reenvía la actual.
    if (vehicle) {
      await updateVehicle(vehicle.id, { ...input, plate: vehicle.plate });
      return vehicle.id;
    }
    return (await publishVehicle(input)).id;
  }

  const save = useMutation({
    mutationFn: async () => {
      const id = await saveVehicle();
      // Al publicar, las fotos en cola se suben cuando ya existe el vehículo.
      if (!vehicle && features.vehiclePhotos && photos.hasQueued) {
        setUploadingPhotos(true);
        try {
          return { id, failedPhotos: await photos.uploadQueued(id) };
        } finally {
          setUploadingPhotos(false);
        }
      }
      return { id, failedPhotos: 0 };
    },
    // El error del servidor debe verse: foco a la placa si está repetida, si no al banner.
    onError: (err) => {
      if (toApiError(err).code === "vehicle.plate-taken")
        requestAnimationFrame(() =>
          document.getElementById("v-plate")?.focus(),
        );
      else
        requestAnimationFrame(() =>
          document
            .getElementById("vehicle-form-error")
            ?.scrollIntoView({ behavior: "smooth", block: "center" }),
        );
    },
    onSuccess: ({ id, failedPhotos }) => {
      queryClient.invalidateQueries({ queryKey: ["vehicles"] });
      const name = `${values.brand.trim()} ${values.model.trim()}`;
      // Se publicó, pero alguna foto falló: a la edición, donde se reintenta.
      if (failedPhotos > 0) {
        showToast(
          `Publicamos tu ${name}, pero ${failedPhotos === 1 ? "una foto no se subió" : `${failedPhotos} fotos no se subieron`}. Vuelve a agregarlas.`,
        );
        navigate(`/anfitrion/vehiculos/${id}`, { replace: true });
        return;
      }
      if (!features.myVehicles) {
        showToast(`Publicamos tu ${name}`);
        navigate("/buscar");
        return;
      }
      const state: VehicleSavedState = { saved: { name, created: !editing } };
      navigate("/anfitrion/vehiculos", { state });
    },
  });

  const serverError = save.isError ? toApiError(save.error) : null;
  const plateTaken = serverError?.code === "vehicle.plate-taken";
  const serverField = (key: string) =>
    serverError?.code === ERROR_CODES.validation
      ? serverError.fields[key.toLowerCase()]
      : undefined;

  const set = (key: keyof VehicleFormValues) => (value: string) => {
    if (save.isError) save.reset();
    setValues((v) => ({ ...v, [key]: value }));
  };

  const submit = (e: FormEvent) => {
    e.preventDefault();
    setSubmitted(true);
    if (Object.keys(errors).length === 0) save.mutate();
    else document.getElementById(`v-${Object.keys(errors)[0]}`)?.focus();
  };

  const text = (
    key: keyof VehicleFormValues,
    { className, ...props }: React.ComponentProps<"input"> = {},
  ) => {
    const error =
      shown(key) ??
      serverField(key) ??
      (key === "plate" && plateTaken ? serverError?.message : undefined);
    return {
      error,
      input: (
        <input
          id={`v-${key}`}
          value={values[key]}
          onChange={(e) => set(key)(e.target.value)}
          onBlur={() => setTouched((t) => ({ ...t, [key]: true }))}
          aria-invalid={Boolean(error) || undefined}
          aria-describedby={`v-${key}-hint`}
          autoComplete="off"
          className={cn(inputClass(Boolean(error)), className)}
          {...props}
        />
      ),
    };
  };

  const brand = text("brand", { placeholder: "Toyota" });
  const model = text("model", { placeholder: "Corolla" });
  const year = text("year", {
    inputMode: "numeric",
    maxLength: 4,
    placeholder: "2020",
  });
  const plate = text("plate", {
    placeholder: "A123456",
    maxLength: 10,
    disabled: editing,
    className: "uppercase",
  });
  const priceError = shown("pricePerDay") ?? serverField("pricePerDay");
  const city = text("city", { placeholder: "Santiago", maxLength: CITY_MAX });

  // Vista previa: la tarjeta del catálogo con los valores escritos, con un fundido corto.
  const preview = useDeferredValue(values);
  // Portada de la vista previa: la primera foto (local o ya guardada).
  const cover = features.vehiclePhotos ? photos.items[0] : undefined;
  const previewVehicle: Vehicle = {
    id: vehicle?.id ?? "preview",
    ownerId: user.id,
    brand: preview.brand.trim() || "Marca",
    model: preview.model.trim() || "Modelo",
    year: Number(preview.year) || new Date().getFullYear(),
    plate: preview.plate,
    pricePerDay: Number(preview.pricePerDay) || 0,
    createdAt: new Date().toISOString(),
    currency: "DOP",
    convertedPricePerDay: null,
    convertedCurrency: null,
    exchangeRate: null,
    type: preview.type,
    city: preview.city || null,
    description: preview.description || null,
    isActive: true,
    coverPhotoId: cover?.kind === "saved" ? cover.documentId : null,
  };

  return (
    <main className="px-gutter pt-4 pb-16">
      <div className="mx-auto max-w-280">
        <Link
          to={
            features.myVehicles ? "/anfitrion/vehiculos" : "/perfil/anfitrion"
          }
          className="mb-4 inline-flex items-center gap-1 rounded-full py-1 pr-2 text-[13.5px] font-semibold text-muted-foreground outline-none hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring"
        >
          <ChevronLeft className="size-4" /> Mis vehículos
        </Link>
        <h1 className="text-[26px] leading-tight font-bold tracking-tight md:text-[30px]">
          {editing ? "Editar vehículo" : "Publicar vehículo"}
        </h1>
        <p className="mt-1 mb-8 text-[14.5px] text-muted-foreground">
          {editing
            ? "Los cambios de precio no afectan reservas ya hechas."
            : "Aparecerá en el catálogo para las fechas en que esté libre."}
        </p>

        <div className="grid gap-10 lg:grid-cols-[minmax(0,1fr)_340px] lg:gap-16">
          <form
            onSubmit={submit}
            noValidate
            className="grid content-start gap-6"
          >
            <AnimatePresence initial={false}>
              {/* Errores del servidor que no son de un campo: banner rojo suave. */}
              {serverError &&
                !plateTaken &&
                serverError.code !== ERROR_CODES.validation && (
                  <m.div
                    id="vehicle-form-error"
                    key="error"
                    initial={{ opacity: 0, y: -6 }}
                    animate={{ opacity: 1, y: 0 }}
                    exit={{ opacity: 0 }}
                  >
                    <Alert variant="destructive">
                      <CircleAlert />
                      <AlertDescription>
                        {errorMessage(save.error)}
                      </AlertDescription>
                    </Alert>
                  </m.div>
                )}
            </AnimatePresence>

            <Section title="Datos" description="Como aparece en la matrícula.">
              <div className="grid gap-x-4 sm:grid-cols-2">
                <FormField id="v-brand" label="Marca" error={brand.error}>
                  {brand.input}
                </FormField>
                <FormField id="v-model" label="Modelo" error={model.error}>
                  {model.input}
                </FormField>
                <FormField
                  id="v-year"
                  label="Año"
                  error={year.error}
                  hint={`Entre 1980 y ${new Date().getFullYear() + 1}.`}
                >
                  {year.input}
                </FormField>
                <FormField
                  id="v-plate"
                  label="Placa"
                  error={plate.error}
                  hint={
                    editing ? (
                      <span className="inline-flex items-center gap-1">
                        <Lock className="size-3" /> La placa no se puede
                        cambiar.
                      </span>
                    ) : (
                      "Solo la ves tú. De 3 a 10 letras, números o guiones."
                    )
                  }
                >
                  {plate.input}
                </FormField>
              </div>
            </Section>

            <Section
              title="Precio por día"
              description="En pesos dominicanos (DOP). Es lo que se cobra al arrendatario."
            >
              <FormField
                id="v-pricePerDay"
                label="Precio por día"
                error={priceError}
                className="max-w-72"
              >
                <div className="relative">
                  <span
                    aria-hidden="true"
                    className="pointer-events-none absolute inset-y-0 left-3.5 grid place-items-center text-[15px] font-semibold text-muted-foreground"
                  >
                    RD$
                  </span>
                  <input
                    id="v-pricePerDay"
                    inputMode="decimal"
                    placeholder="2,500"
                    value={values.pricePerDay}
                    onChange={(e) =>
                      set("pricePerDay")(e.target.value.replace(/[^\d.]/g, ""))
                    }
                    onBlur={() =>
                      setTouched((t) => ({ ...t, pricePerDay: true }))
                    }
                    aria-invalid={Boolean(priceError) || undefined}
                    aria-describedby="v-pricePerDay-hint"
                    className={cn(
                      inputClass(Boolean(priceError)),
                      "pl-13 tabular-nums",
                    )}
                  />
                </div>
              </FormField>
            </Section>

            {/* P4: secciones que aparecen solo si el backend las soporta. */}
            {features.vehicleDetails && (
              <>
                <Section title="Tipo" optional>
                  <div
                    role="radiogroup"
                    aria-label="Tipo de vehículo"
                    className="flex flex-wrap gap-2"
                  >
                    {VEHICLE_TYPES.map((t) => {
                      const checked = values.type === t.value;
                      return (
                        <button
                          key={t.value}
                          type="button"
                          role="radio"
                          aria-checked={checked}
                          onClick={() => set("type")(t.value)}
                          className={cn(
                            "h-10 rounded-full border px-4 text-[14px] font-semibold outline-none focus-visible:ring-2 focus-visible:ring-ring",
                            checked
                              ? "border-primary bg-primary text-primary-foreground"
                              : "border-border hover:border-foreground",
                          )}
                        >
                          {t.label}
                        </button>
                      );
                    })}
                  </div>
                </Section>
                <Section title="Ubicación" optional>
                  <FormField
                    id="v-city"
                    label="Ciudad"
                    error={city.error}
                    className="max-w-90"
                  >
                    {city.input}
                  </FormField>
                </Section>
                <Section title="Descripción" optional>
                  <FormField
                    id="v-description"
                    label="Cuéntale a los arrendatarios cómo es"
                    error={shown("description")}
                    hint={`${values.description.length}/${DESCRIPTION_MAX}`}
                  >
                    <textarea
                      id="v-description"
                      value={values.description}
                      onChange={(e) => set("description")(e.target.value)}
                      onBlur={() =>
                        setTouched((t) => ({ ...t, description: true }))
                      }
                      aria-describedby="v-description-hint"
                      rows={4}
                      placeholder="Automático, aire acondicionado, ideal para ciudad…"
                      className={cn(
                        inputClass(Boolean(shown("description"))),
                        "h-auto py-3",
                      )}
                    />
                  </FormField>
                </Section>
              </>
            )}
            {features.vehiclePhotos && (
              <Section title="Fotos" optional>
                <PhotosField
                  vehicleId={vehicle?.id}
                  brand={values.brand || "Vehículo"}
                  items={photos.items}
                  isLoading={photos.isLoading}
                  removing={photos.removing}
                  onAdd={photos.add}
                  onRemove={photos.remove}
                  onRetry={photos.retry}
                  pendingPublish={!vehicle}
                />
              </Section>
            )}

            <div className="flex flex-wrap items-center gap-3 pt-2">
              <Button
                type="submit"
                size="lg"
                className="h-13 min-w-48 rounded-full text-[15px]"
                disabled={save.isPending}
                aria-busy={save.isPending}
              >
                {uploadingPhotos ? (
                  <>
                    <LoaderCircle className="animate-spin" /> Subiendo fotos…
                  </>
                ) : save.isPending ? (
                  <LoaderCircle className="animate-spin" />
                ) : editing ? (
                  "Guardar cambios"
                ) : (
                  "Publicar vehículo"
                )}
              </Button>
              {editing && (
                <Link
                  to="/anfitrion/vehiculos"
                  className={buttonVariants({
                    variant: "ghost",
                    size: "lg",
                    className: "rounded-full",
                  })}
                >
                  Cancelar
                </Link>
              )}
            </div>
          </form>

          <aside className="self-start lg:sticky lg:top-28">
            <p className="mb-3 text-[13px] font-bold tracking-wide text-muted-foreground uppercase">
              Así se verá
            </p>
            <m.div
              key={`${preview.brand}|${preview.model}|${preview.year}|${preview.pricePerDay}|${cover?.key ?? ""}`}
              initial={{ opacity: 0.4 }}
              animate={{ opacity: 1 }}
              transition={{ duration: 0.2 }}
              // Solo es una vista previa: ni el enlace ni el tooltip responden.
              inert
              className="max-w-80"
            >
              <ul className="list-none">
                <VehicleCard
                  vehicle={previewVehicle}
                  to="#"
                  imageUrl={cover?.kind === "local" ? cover.url : null}
                />
              </ul>
            </m.div>
            <p className="mt-4 text-[12.5px] text-muted-foreground">
              Así aparece en el catálogo. Los arrendatarios no ven la placa.
            </p>
          </aside>
        </div>
      </div>
    </main>
  );
}

function Section({
  title,
  description,
  optional,
  children,
}: {
  title: string;
  description?: string;
  optional?: boolean;
  children: React.ReactNode;
}) {
  return (
    <fieldset className="grid gap-4 rounded-[20px] border border-border p-5 sm:p-6">
      <legend className="sr-only">{title}</legend>
      <div aria-hidden="true">
        <p className="text-[16px] font-bold tracking-tight">
          {title}
          {optional && (
            <span className="font-normal text-muted-foreground">
              {" "}
              · opcional
            </span>
          )}
        </p>
        {description && (
          <p className="text-[13.5px] text-muted-foreground">{description}</p>
        )}
      </div>
      {children}
    </fieldset>
  );
}
