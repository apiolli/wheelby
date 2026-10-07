import { DEMO, DEMO_ADMIN, DEMO_INACTIVE } from "./demo";

// "Base de datos" en memoria del prototipo: cuentas y solicitudes de anfitrión. La comparten
// el acceso, el perfil y el panel de administración, así que lo que hace el admin se ve en el
// perfil al momento. Se lee con useDb() y se modifica solo desde src/data/*.ts.

export type Role = "standard" | "admin";
export type AccountStatus = "active" | "inactive" | "locked" | "disabled";
export type HostStatus = "none" | "pending" | "verified" | "rejected";
export type RequestStatus = "pending" | "approved" | "rejected";
export type DocumentKind = "id-front" | "id-back" | "license";

export interface Account {
  id: string;
  name: string;
  email: string;
  password: string;
  role: Role;
  // Abrió el enlace de activación del correo.
  activated: boolean;
  // Desactivada por un administrador.
  disabled: boolean;
  // Bloqueo por intentos fallidos (RF-CA-19); 0 si no hay.
  lockedUntil: number;
  createdAt: string;
  // Último restablecimiento de contraseña forzado por un administrador.
  resetForcedAt?: string;
}

export interface HostRequest {
  id: string;
  accountId: string;
  submittedAt: string;
  status: RequestStatus;
  documents: DocumentKind[];
  decidedAt?: string;
  reason?: string;
}

export interface Db {
  accounts: Account[];
  hostRequests: HostRequest[];
}

export const ROLE_LABEL: Record<Role, string> = {
  standard: "Estándar",
  admin: "Administrador",
};
export const STATUS_LABEL: Record<AccountStatus, string> = {
  active: "Activa",
  inactive: "Sin activar",
  locked: "Bloqueada",
  disabled: "Desactivada",
};
export const DOCUMENT_LABEL: Record<DocumentKind, string> = {
  "id-front": "Cédula (frente)",
  "id-back": "Cédula (reverso)",
  license: "Licencia de conducir",
};
export const ALL_DOCUMENTS: DocumentKind[] = ["id-front", "id-back", "license"];

export function accountStatus(
  account: Account,
  now = Date.now(),
): AccountStatus {
  if (account.disabled) return "disabled";
  if (!account.activated) return "inactive";
  if (account.lockedUntil > now) return "locked";
  return "active";
}

// El estado de anfitrión sale de la solicitud más reciente de la cuenta.
export function latestHostRequest(db: Db, accountId: string) {
  return db.hostRequests.filter((r) => r.accountId === accountId).at(-1);
}

export function hostStatus(request: HostRequest | undefined): HostStatus {
  if (!request) return "none";
  return request.status === "approved" ? "verified" : request.status;
}

export const normalizeEmail = (email: string) => email.trim().toLowerCase();

// ---- Datos de ejemplo ----

type Seed = Pick<Account, "name" | "email" | "createdAt"> & Partial<Account>;

const seed = (id: string, s: Seed): Account => ({
  id,
  password: "demo1234",
  role: "standard",
  activated: true,
  disabled: false,
  lockedUntil: 0,
  ...s,
});

const ACCOUNTS: Account[] = [
  seed("u-admin", { ...DEMO_ADMIN, role: "admin", createdAt: "2024-11-02" }),
  seed("u-demo", { ...DEMO, createdAt: "2025-03-14" }),
  seed("u-inactiva", {
    ...DEMO_INACTIVE,
    activated: false,
    createdAt: "2026-10-03",
  }),
  seed("u-maria", {
    name: "María Santos",
    email: "maria.santos@correo.do",
    createdAt: "2025-01-20",
  }),
  seed("u-jose", {
    name: "José Martínez",
    email: "jose.martinez@correo.do",
    createdAt: "2025-06-08",
  }),
  seed("u-carmen", {
    name: "Carmen Reyes",
    email: "carmen.reyes@correo.do",
    createdAt: "2025-09-30",
  }),
  seed("u-pedro", {
    name: "Pedro Gómez",
    email: "pedro.gomez@correo.do",
    createdAt: "2025-12-11",
    lockedUntil: Date.now() + 15 * 60_000,
  }),
  seed("u-rosa", {
    name: "Rosa Jiménez",
    email: "rosa.jimenez@correo.do",
    createdAt: "2025-04-02",
    disabled: true,
  }),
  seed("u-miguel", {
    name: "Miguel Torres",
    email: "miguel.torres@correo.do",
    createdAt: "2026-02-17",
  }),
  seed("u-laura", {
    name: "Laura Castillo",
    email: "laura.castillo@wheelby.do",
    role: "admin",
    createdAt: "2024-11-15",
  }),
  seed("u-daniel", {
    name: "Daniel Vargas",
    email: "daniel.vargas@correo.do",
    createdAt: "2026-08-21",
  }),
  seed("u-sofia", {
    name: "Sofía Herrera",
    email: "sofia.herrera@correo.do",
    activated: false,
    createdAt: "2026-10-04",
  }),
  seed("u-andres", {
    name: "Andrés Polanco",
    email: "andres.polanco@correo.do",
    createdAt: "2026-05-09",
  }),
  seed("u-patricia", {
    name: "Patricia Ureña",
    email: "patricia.urena@correo.do",
    createdAt: "2025-07-23",
  }),
];

const HOST_REQUESTS: HostRequest[] = [
  {
    id: "hr-1",
    accountId: "u-maria",
    submittedAt: "2025-02-02",
    status: "approved",
    decidedAt: "2025-02-04",
    documents: ALL_DOCUMENTS,
  },
  {
    id: "hr-2",
    accountId: "u-patricia",
    submittedAt: "2025-08-10",
    status: "approved",
    decidedAt: "2025-08-11",
    documents: ALL_DOCUMENTS,
  },
  {
    id: "hr-3",
    accountId: "u-miguel",
    submittedAt: "2026-09-12",
    status: "rejected",
    decidedAt: "2026-09-14",
    reason:
      "La foto de la licencia está borrosa y no se puede leer el número. Súbela de nuevo con buena luz.",
    documents: ALL_DOCUMENTS,
  },
  {
    id: "hr-4",
    accountId: "u-jose",
    submittedAt: "2026-10-01",
    status: "pending",
    documents: ALL_DOCUMENTS,
  },
  {
    id: "hr-5",
    accountId: "u-carmen",
    submittedAt: "2026-10-03",
    status: "pending",
    documents: ["id-front", "id-back"],
  },
  {
    id: "hr-6",
    accountId: "u-daniel",
    submittedAt: "2026-10-04",
    status: "pending",
    documents: ALL_DOCUMENTS,
  },
];

// ---- Store ----

let db: Db = { accounts: ACCOUNTS, hostRequests: HOST_REQUESTS };
const listeners = new Set<() => void>();

export const getDb = () => db;

export function subscribeDb(listener: () => void) {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

// Cambios inmutables: cada escritura crea un snapshot nuevo para que React lo detecte.
export function updateDb(change: (db: Db) => Db) {
  db = change(db);
  listeners.forEach((l) => l());
}

export function updateAccount(id: string, patch: Partial<Account>) {
  updateDb((d) => ({
    ...d,
    accounts: d.accounts.map((a) => (a.id === id ? { ...a, ...patch } : a)),
  }));
}

export const findAccountByEmail = (email: string) =>
  db.accounts.find((a) => a.email === normalizeEmail(email));

export const wait = (ms: number) =>
  new Promise((resolve) => setTimeout(resolve, ms));
