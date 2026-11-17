// Token de sesión del cliente. Se guarda en localStorage para sobrevivir a una recarga; si el
// almacenamiento no está disponible (modo privado estricto), la sesión dura lo que la pestaña.

const KEY = "wheelby.token";
let memory: string | null = null;

export function getToken() {
  try {
    return localStorage.getItem(KEY) ?? memory;
  } catch {
    return memory;
  }
}

export function setToken(token: string) {
  memory = token;
  try {
    localStorage.setItem(KEY, token);
  } catch {
    // Sin almacenamiento: queda solo en memoria.
  }
}

export function clearToken() {
  memory = null;
  try {
    localStorage.removeItem(KEY);
  } catch {
    // Nada que borrar.
  }
}
