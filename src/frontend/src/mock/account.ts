import { authorize, clearFailures, requestPasswordReset } from "./auth";
import {
  ALL_DOCUMENTS,
  getDb,
  updateAccount,
  updateDb,
  wait,
  type Role,
} from "./db";

// Acciones de anfitrión y de administración sobre la base de datos simulada.

const today = () => new Date().toISOString().slice(0, 10);

// El usuario solicita ser anfitrión. En la app real subiría aquí sus documentos.
export async function requestHost(accountId: string) {
  await authorize();
  await wait(700);
  updateDb((d) => ({
    ...d,
    hostRequests: [
      ...d.hostRequests,
      {
        id: `hr-${Date.now()}`,
        accountId,
        submittedAt: today(),
        status: "pending",
        documents: ALL_DOCUMENTS,
      },
    ],
  }));
}

export async function approveHostRequest(id: string) {
  await authorize();
  await wait(500);
  updateDb((d) => ({
    ...d,
    hostRequests: d.hostRequests.map((r) =>
      r.id === id ? { ...r, status: "approved", decidedAt: today() } : r,
    ),
  }));
}

export async function rejectHostRequest(id: string, reason: string) {
  await authorize();
  await wait(500);
  updateDb((d) => ({
    ...d,
    hostRequests: d.hostRequests.map((r) =>
      r.id === id
        ? {
            ...r,
            status: "rejected",
            decidedAt: today(),
            reason: reason.trim(),
          }
        : r,
    ),
  }));
}

export async function setRole(accountId: string, role: Role) {
  await authorize();
  await wait(500);
  updateAccount(accountId, { role });
}

// Activar también da por verificado el correo y quita un bloqueo por intentos.
export async function setAccountEnabled(accountId: string, enabled: boolean) {
  await authorize();
  await wait(500);
  const account = getDb().accounts.find((a) => a.id === accountId);
  if (enabled && account) clearFailures(account.email);
  updateAccount(
    accountId,
    enabled
      ? { disabled: false, activated: true, lockedUntil: 0 }
      : { disabled: true },
  );
}

// Envía al usuario un código de recuperación, como si lo hubiera pedido él.
export async function forcePasswordReset(accountId: string) {
  await authorize();
  const account = getDb().accounts.find((a) => a.id === accountId);
  if (!account) return;
  await requestPasswordReset(account.email);
  updateAccount(accountId, { resetForcedAt: today() });
}
