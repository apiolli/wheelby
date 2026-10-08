// Reglas compartidas por registro y restablecimiento de contraseña.
export const PASSWORD_RULES = [
  {
    id: "len",
    label: "Mínimo 8 caracteres",
    test: (p: string) => p.length >= 8,
  },
  {
    id: "letter",
    label: "Al menos una letra",
    test: (p: string) => /[a-zA-ZáéíóúñÁÉÍÓÚÑ]/.test(p),
  },
  {
    id: "number",
    label: "Al menos un número",
    test: (p: string) => /\d/.test(p),
  },
];

export const passwordChecks = (password: string) =>
  PASSWORD_RULES.map((r) => r.test(password));

export const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
