import { z } from "zod";

import { vehicleSchema } from "@/lib/schemas";
import type { Vehicle, VehicleType } from "@/types/types";

export const VEHICLE_TYPES: { value: VehicleType; label: string }[] = [
  { value: "Car", label: "Auto" },
  { value: "Motorcycle", label: "Moto" },
  { value: "Bicycle", label: "Bicicleta" },
  { value: "Scooter", label: "Scooter" },
  { value: "Other", label: "Otro" },
];

export const CITY_MAX = 80;
export const DESCRIPTION_MAX = 1000;

// Mismas reglas que Vehicle.Create/Update en el backend; tipo, ciudad y descripción (P4) son
// opcionales.
export const vehicleFormSchema = vehicleSchema.extend({
  type: z.enum(["Car", "Motorcycle", "Bicycle", "Scooter", "Other"]),
  city: z.string().trim().max(CITY_MAX, `Máximo ${CITY_MAX} caracteres.`),
  description: z
    .string()
    .trim()
    .max(DESCRIPTION_MAX, `Máximo ${DESCRIPTION_MAX} caracteres.`),
});

export interface VehicleFormValues {
  brand: string;
  model: string;
  year: string;
  plate: string;
  pricePerDay: string;
  type: VehicleType;
  city: string;
  description: string;
}

export const EMPTY_VEHICLE: VehicleFormValues = {
  brand: "",
  model: "",
  year: "",
  plate: "",
  pricePerDay: "",
  type: "Car",
  city: "",
  description: "",
};

export const valuesFrom = (v: Vehicle): VehicleFormValues => ({
  brand: v.brand,
  model: v.model,
  year: String(v.year),
  plate: v.plate,
  pricePerDay: String(v.pricePerDay),
  type: v.type,
  city: v.city ?? "",
  description: v.description ?? "",
});
