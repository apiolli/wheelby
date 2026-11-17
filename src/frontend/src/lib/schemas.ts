import { z } from "zod";

import { PASSWORD_RULES } from "./password";

// Validaciones de formularios y de parámetros de URL, en un solo lugar. Los mensajes son los que
// ve la persona usuaria; las reglas coinciden con las del backend.

export const emailSchema = z
  .string()
  .trim()
  .pipe(z.email("Escribe un correo válido."));

export const passwordSchema = PASSWORD_RULES.reduce(
  (schema, rule) => schema.refine(rule.test, rule.label),
  z.string(),
);

export const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, "Escribe tu contraseña."),
});

export const registerSchema = z.object({
  name: z
    .string()
    .trim()
    .min(3, "Escribe tu nombre y apellido.")
    .max(100, "El nombre es demasiado largo."),
  email: emailSchema,
  password: passwordSchema,
  accepted: z.literal(true, "Acepta los términos para continuar."),
});

export const RESET_CODE_LENGTH = 6;

export const resetSchema = z
  .object({
    code: z
      .string()
      .regex(
        new RegExp(`^\\d{${RESET_CODE_LENGTH}}$`),
        `El código tiene ${RESET_CODE_LENGTH} dígitos.`,
      ),
    password: passwordSchema,
    confirm: z.string(),
  })
  .refine((v) => v.confirm.length > 0 && v.confirm === v.password, {
    path: ["confirm"],
    message: "Las contraseñas no coinciden.",
  });

export const changePasswordSchema = z
  .object({
    current: z.string().min(1, "Escribe tu contraseña actual."),
    password: passwordSchema,
    confirm: z.string(),
  })
  .refine((v) => v.password !== v.current, {
    path: ["password"],
    message: "Debe ser distinta de la actual.",
  })
  .refine((v) => v.confirm.length > 0 && v.confirm === v.password, {
    path: ["confirm"],
    message: "Las contraseñas no coinciden.",
  });

// Mínimo del motivo que acompaña un rechazo o una cancelación del anfitrión.
export const MIN_REASON = 10;
export const MAX_REASON = 500;
export const reasonSchema = z
  .string()
  .trim()
  .min(MIN_REASON, `Escribe al menos ${MIN_REASON} caracteres.`)
  .max(MAX_REASON, `El motivo no puede pasar de ${MAX_REASON} caracteres.`);

const CURRENT_YEAR = new Date().getFullYear();

// Mismas reglas que Vehicle.Create en el backend.
export const vehicleSchema = z.object({
  brand: z
    .string()
    .trim()
    .min(1, "Escribe la marca.")
    .max(60, "Máximo 60 caracteres."),
  model: z
    .string()
    .trim()
    .min(1, "Escribe el modelo.")
    .max(60, "Máximo 60 caracteres."),
  year: z.coerce
    .number<string>("Escribe el año.")
    .int("Escribe el año.")
    .min(1980, `El año debe estar entre 1980 y ${CURRENT_YEAR + 1}.`)
    .max(
      CURRENT_YEAR + 1,
      `El año debe estar entre 1980 y ${CURRENT_YEAR + 1}.`,
    ),
  plate: z
    .string()
    .trim()
    .toUpperCase()
    .regex(/^[A-Z0-9-]{3,10}$/, "De 3 a 10 letras, números o guiones."),
  pricePerDay: z.coerce
    .number<string>("Escribe el precio por día.")
    .positive("El precio debe ser mayor que cero.")
    .max(1_000_000, "El precio no puede pasar de 1,000,000."),
});

// ---------- Fechas (YYYY-MM-DD, en hora local) ----------

export const isoDay = (d: Date) => {
  const local = new Date(d.getTime() - d.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 10);
};

export const addDays = (day: string, n: number) => {
  const d = new Date(`${day}T12:00:00`);
  d.setDate(d.getDate() + n);
  return isoDay(d);
};

export const today = () => isoDay(new Date());

// Un día de calendario se envía como el inicio de ese día en hora local.
export const dayStartIso = (day: string) =>
  new Date(`${day}T00:00:00`).toISOString();

export const daysBetween = (from: string, to: string) =>
  Math.round(
    (new Date(`${to}T12:00:00`).getTime() -
      new Date(`${from}T12:00:00`).getTime()) /
      86_400_000,
  );

const daySchema = z.string().regex(/^\d{4}-\d{2}-\d{2}$/);

// Rango de un alquiler: empieza a partir de mañana (el backend exige un inicio futuro) y dura al
// menos un día.
export const rangeSchema = z
  .object({ from: daySchema, to: daySchema })
  .refine((r) => r.from > today(), {
    path: ["from"],
    message: "La fecha de inicio debe ser a partir de mañana.",
  })
  .refine((r) => r.to > r.from, {
    path: ["to"],
    message: "La devolución debe ser después de la recogida.",
  });

export const CURRENCIES = ["DOP", "USD", "EUR"] as const;
export type Currency = (typeof CURRENCIES)[number];

// Búsqueda del catálogo en la URL: ?desde=2026-12-01&hasta=2026-12-04&moneda=USD&pagina=2.
// Si algo falta o no vale, se usa un valor por defecto en vez de mostrar un error.
export function parseCatalogParams(params: URLSearchParams) {
  const defaults = { from: addDays(today(), 1), to: addDays(today(), 4) };
  const range = rangeSchema.safeParse({
    from: params.get("desde"),
    to: params.get("hasta"),
  });
  const currency = z.enum(CURRENCIES).catch("DOP").parse(params.get("moneda"));
  const page = z.coerce
    .number()
    .int()
    .min(1)
    .catch(1)
    .parse(params.get("pagina"));
  return {
    ...(range.success ? range.data : defaults),
    currency,
    page,
    custom: range.success,
  };
}

export const pageParam = (params: URLSearchParams, key = "pagina") =>
  z.coerce.number().int().min(1).catch(1).parse(params.get(key));

// Errores de un safeParse por campo (el primero de cada uno).
export function fieldErrors<T extends z.ZodType>(
  schema: T,
  values: unknown,
): Partial<Record<string, string>> {
  const result = schema.safeParse(values);
  if (result.success) return {};
  const errors: Partial<Record<string, string>> = {};
  for (const issue of result.error.issues) {
    const key = String(issue.path[0] ?? "");
    errors[key] ??= issue.message;
  }
  return errors;
}

// Mismo límite por defecto que el backend (Documents__MaxFileSizeBytes = 10 MB).
export const MAX_DOCUMENT_BYTES = 10 * 1024 * 1024;

export const documentSchema = z
  .instanceof(File, { message: "Elige un archivo." })
  .refine((f) => f.size > 0, "El archivo está vacío.")
  .refine((f) => f.size <= MAX_DOCUMENT_BYTES, "El archivo pasa de 10 MB.")
  .refine(
    (f) => f.name.length <= 255,
    "El nombre del archivo es demasiado largo.",
  );
