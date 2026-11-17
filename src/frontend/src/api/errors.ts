import { isAxiosError } from "axios";

// Respuesta de error de la API (application/problem+json).
interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export interface ApiError {
  status: number | null;
  message: string;
}

const NETWORK_MESSAGE =
  "No pudimos conectar con Wheelby. Revisa tu conexión e inténtalo de nuevo.";
const GENERIC_MESSAGE = "Algo salió mal. Inténtalo de nuevo en unos minutos.";

// Traduce cualquier error de una llamada a un estado HTTP y un mensaje para la interfaz. Los
// mensajes de la API ya vienen en español; si es un error de validación, se toma el primero.
export function toApiError(error: unknown): ApiError {
  if (!isAxiosError(error)) return { status: null, message: GENERIC_MESSAGE };
  if (!error.response) return { status: null, message: NETWORK_MESSAGE };

  const { status, data } = error.response;
  const problem = (data ?? {}) as ProblemDetails;
  const firstFieldError = problem.errors
    ? Object.values(problem.errors).flat()[0]
    : undefined;

  return {
    status,
    message: firstFieldError ?? problem.detail ?? GENERIC_MESSAGE,
  };
}

export const errorMessage = (error: unknown) => toApiError(error).message;
