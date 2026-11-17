import axios, { isAxiosError } from "axios";

import { getToken } from "@/lib/token";

// Cliente HTTP de la API de Wheelby. VITE_API_URL apunta a la raíz de la API (por defecto, la del
// contenedor: http://localhost:8080). Las rutas no llevan prefijo /api.
const wheelbyApi = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:8080",
});

wheelbyApi.interceptors.request.use((config) => {
  const token = getToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

// Un 401 en una llamada con sesión cierra la sesión: quien la gestiona se suscribe aquí. Las
// llamadas de acceso (login, registro…) no llevan token y manejan su 401 ellas mismas.
const unauthorizedListeners = new Set<() => void>();

export function onUnauthorized(listener: () => void) {
  unauthorizedListeners.add(listener);
  return () => {
    unauthorizedListeners.delete(listener);
  };
}

wheelbyApi.interceptors.response.use(undefined, (error) => {
  if (
    isAxiosError(error) &&
    error.response?.status === 401 &&
    error.config?.headers?.Authorization
  ) {
    unauthorizedListeners.forEach((l) => l());
  }
  return Promise.reject(error);
});

export { wheelbyApi };
