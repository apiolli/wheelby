import { isAxiosError } from "axios";

// Respuesta de error de la API (application/problem+json).
interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
  // Código estable del backend (p. ej. "vehicle.overlap"): se decide con él, no con el texto.
  code?: string;
  // Identificador de la traza del servidor, para reportar el error.
  traceId?: string;
}

export interface ApiError {
  status: number | null;
  message: string;
  code: string | null;
  // Errores por campo de una validación (400), con el nombre del campo en minúsculas.
  fields: Record<string, string>;
  traceId: string | null;
  // Sin respuesta del servidor (red caída o servidor inalcanzable).
  offline: boolean;
}

const NETWORK_MESSAGE =
  "No pudimos conectar con Wheelby. Revisa tu conexión e inténtalo de nuevo.";
const GENERIC_MESSAGE = "Algo salió mal. Inténtalo de nuevo en unos minutos.";

// Traduce cualquier error de una llamada a un estado HTTP y un mensaje para la interfaz. Los
// mensajes de la API ya vienen en español; si es un error de validación, se toma el primero.
export function toApiError(error: unknown): ApiError {
  if (!isAxiosError(error))
    return {
      status: null,
      message: GENERIC_MESSAGE,
      code: null,
      fields: {},
      traceId: null,
      offline: false,
    };
  if (!error.response)
    return {
      status: null,
      message: NETWORK_MESSAGE,
      code: null,
      fields: {},
      traceId: null,
      offline: true,
    };

  const { status, data } = error.response;
  const problem = (data ?? {}) as ProblemDetails;
  const firstFieldError = problem.errors
    ? Object.values(problem.errors).flat()[0]
    : undefined;

  const fields: Record<string, string> = {};
  for (const [key, messages] of Object.entries(problem.errors ?? {}))
    if (messages[0]) fields[key.toLowerCase()] = messages[0];

  return {
    status,
    message: firstFieldError ?? problem.detail ?? GENERIC_MESSAGE,
    code: problem.code ?? null,
    fields,
    traceId: problem.traceId ?? null,
    offline: false,
  };
}

export const errorMessage = (error: unknown) => toApiError(error).message;

export const errorCode = (error: unknown) => toApiError(error).code;

// Códigos del backend que la interfaz trata de forma propia.
export const ERROR_CODES = {
  overlap: "vehicle.overlap",
  ownVehicle: "reservation.own-vehicle",
  vehicleInactive: "vehicle.inactive",
  vehicleNotFound: "vehicle.not-found",
  reservationNotFound: "reservation.not-found",
  validation: "validation",
} as const;
