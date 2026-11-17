import type { LoginResult, User } from "@/types/types";
import { wheelbyApi } from "./wheelby-api";

// Control de acceso (/auth). Las respuestas de error se interpretan con toApiError.

export const login = (email: string, password: string) =>
  wheelbyApi
    .post<LoginResult>("/auth/login", { email: email.trim(), password })
    .then((r) => r.data);

// Valida un token concreto (antes de guardarlo) o el guardado si no se pasa ninguno.
export const getMe = (token?: string) =>
  wheelbyApi
    .get<User>(
      "/auth/me",
      token ? { headers: { Authorization: `Bearer ${token}` } } : undefined,
    )
    .then((r) => r.data);

export const logout = () => wheelbyApi.post("/auth/logout");

export const register = (fullName: string, email: string, password: string) =>
  wheelbyApi.post("/auth/register", {
    fullName: fullName.trim(),
    email: email.trim(),
    password,
  });

export const activate = (userId: string, token: string) =>
  wheelbyApi.post("/auth/activate", { userId, token });

export const resendActivation = (email: string) =>
  wheelbyApi.post("/auth/resend-activation", { email: email.trim() });

export const forgotPassword = (email: string) =>
  wheelbyApi.post("/auth/forgot-password", { email: email.trim() });

export const resetPassword = (
  email: string,
  code: string,
  newPassword: string,
) =>
  wheelbyApi.post("/auth/reset-password", {
    email: email.trim(),
    code,
    newPassword,
  });

// Revoca las demás sesiones y devuelve un token nuevo para esta.
export const changePassword = (currentPassword: string, newPassword: string) =>
  wheelbyApi
    .post<LoginResult>("/auth/change-password", {
      currentPassword,
      newPassword,
    })
    .then((r) => r.data);
