import type { User } from "@/types/types";
import {
  findAccountByEmail,
  getDb,
  normalizeEmail,
  updateAccount,
  updateDb,
  wait,
} from "./db";
import { DEMO_RESET_CODE } from "./demo";
import { getToken, notifyUnauthorized } from "./token";

// Simula las respuestas del backend de control de acceso sobre la base de datos en memoria
// (src/data/db.ts): al recargar la página se reinicia. Al conectar la API real, conserva las
// firmas y los tipos de resultado.

export const MAX_ATTEMPTS = 5; // RF-CA-19
export const LOCK_MS = 15 * 60_000;
export const SESSION_MS = 60 * 60_000;
export const RESET_CODE_MS = 30 * 60_000;

export type LoginResult =
  | { ok: true; token: string }
  | { ok: false; reason: "invalid" | "inactive" }
  | { ok: false; reason: "locked"; until: number };

export type MeResult =
  | { ok: true; user: User; expiresAt: number }
  | { ok: false; status: 401 };

// Token simulado: lleva la cuenta y el vencimiento, como un JWT sin firma. Al conectar la API real,
// el token es opaco para el cliente y solo el backend lo interpreta.
interface Claims {
  sub: string;
  exp: number;
}

const TOKEN_PREFIX = "wb.";
const issueToken = (sub: string): string =>
  TOKEN_PREFIX + btoa(JSON.stringify({ sub, exp: Date.now() + SESSION_MS }));

function readClaims(token: string | null): Claims | null {
  if (!token?.startsWith(TOKEN_PREFIX)) return null;
  try {
    const claims = JSON.parse(atob(token.slice(TOKEN_PREFIX.length)));
    return typeof claims.sub === "string" && typeof claims.exp === "number"
      ? claims
      : null;
  } catch {
    return null;
  }
}

// Intentos fallidos por correo, exista o no la cuenta, para no revelar cuáles existen. El
// bloqueo de una cuenta existente se guarda en ella para que el panel de administración lo vea.
const failures = new Map<string, { count: number; lockedUntil: number }>();

// Códigos de recuperación por correo. Se generan aunque la cuenta no exista (RF-CA-09).
const resetCodes = new Map<
  string,
  { code: string; expiresAt: number; used: boolean }
>();

export async function login(
  email: string,
  password: string,
): Promise<LoginResult> {
  await wait(700);
  const key = normalizeEmail(email);
  const now = Date.now();
  const account = findAccountByEmail(key);
  const prev = failures.get(key);
  const lockedUntil = account ? account.lockedUntil : (prev?.lockedUntil ?? 0);
  if (lockedUntil > now)
    return { ok: false, reason: "locked", until: lockedUntil };

  // Una cuenta desactivada por un administrador recibe el error genérico.
  if (!account || account.password !== password || account.disabled) {
    const count = (prev && prev.lockedUntil <= now ? prev.count : 0) + 1;
    if (count >= MAX_ATTEMPTS) {
      const until = now + LOCK_MS;
      failures.set(key, { count: 0, lockedUntil: until });
      if (account) updateAccount(account.id, { lockedUntil: until });
      return { ok: false, reason: "locked", until };
    }
    failures.set(key, { count, lockedUntil: 0 });
    return { ok: false, reason: "invalid" };
  }

  failures.delete(key);
  // RF-CA-15: solo se informa de la cuenta inactiva cuando la contraseña es correcta.
  if (!account.activated) return { ok: false, reason: "inactive" };
  return { ok: true, token: issueToken(account.id) };
}

// GET /auth/me: valida el token y devuelve la cuenta. 401 si falta, venció o la cuenta ya no puede
// entrar (desactivada o sin activar).
export async function getMe(token: string): Promise<MeResult> {
  await wait(450);
  const claims = readClaims(token);
  const account =
    claims && claims.exp > Date.now()
      ? getDb().accounts.find((a) => a.id === claims.sub)
      : undefined;
  if (!claims || !account || !account.activated || account.disabled)
    return { ok: false, status: 401 };
  const { id, name, email, role } = account;
  return { ok: true, user: { id, name, email, role }, expiresAt: claims.exp };
}

// Las llamadas autenticadas leen el token guardado, como si viajara en la cabecera Authorization.
// Si falta o venció, el "backend" responde 401: se avisa a la sesión (que redirige al login) y la
// promesa queda pendiente, porque la página que la esperaba se desmonta.
export async function authorize(): Promise<string> {
  const claims = readClaims(getToken());
  if (claims && claims.exp > Date.now()) return claims.sub;
  notifyUnauthorized();
  return new Promise(() => {});
}

// La cuenta nace inactiva. Si el correo ya existe no se toca, y la respuesta es la misma.
export async function register(
  name: string,
  email: string,
  password: string,
): Promise<void> {
  await wait(800);
  const key = normalizeEmail(email);
  if (findAccountByEmail(key)) return;
  updateDb((d) => ({
    ...d,
    accounts: [
      ...d.accounts,
      {
        id: `u-${Date.now()}`,
        name: name.trim(),
        email: key,
        password,
        role: "standard",
        activated: false,
        disabled: false,
        lockedUntil: 0,
        createdAt: new Date().toISOString().slice(0, 10),
      },
    ],
  }));
}

// RF-CA-17: siempre responde igual, exista o no la cuenta.
export async function resendActivation(_email: string): Promise<void> {
  await wait(700);
}

// RF-CA-09: la respuesta es la misma exista o no la cuenta.
export async function requestPasswordReset(email: string): Promise<void> {
  await wait(700);
  resetCodes.set(normalizeEmail(email), {
    code: DEMO_RESET_CODE,
    expiresAt: Date.now() + RESET_CODE_MS,
    used: false,
  });
}

// false si el código no coincide, venció o ya se usó.
export async function resetPassword(
  email: string,
  code: string,
  password: string,
): Promise<boolean> {
  await wait(800);
  const key = normalizeEmail(email);
  const entry = resetCodes.get(key);
  if (
    !entry ||
    entry.used ||
    entry.expiresAt < Date.now() ||
    entry.code !== code
  )
    return false;
  entry.used = true;
  failures.delete(key);
  const account = findAccountByEmail(key);
  if (account) updateAccount(account.id, { password, lockedUntil: 0 });
  return true;
}

// RF-CA-22: cambio de contraseña con sesión iniciada. false si la actual no coincide.
export async function changePassword(
  email: string,
  current: string,
  next: string,
): Promise<boolean> {
  await authorize();
  await wait(800);
  const account = findAccountByEmail(email);
  if (!account || account.password !== current) return false;
  updateAccount(account.id, { password: next });
  return true;
}

// Limpia los intentos fallidos al desbloquear una cuenta desde el panel de administración.
export function clearFailures(email: string) {
  failures.delete(normalizeEmail(email));
}
