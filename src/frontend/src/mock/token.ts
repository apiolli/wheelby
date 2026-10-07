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

// Respuesta 401 en cualquier llamada: quien gestiona la sesión se suscribe y redirige al login.
const unauthorizedListeners = new Set<() => void>();

export function onUnauthorized(listener: () => void) {
  unauthorizedListeners.add(listener);
  return () => {
    unauthorizedListeners.delete(listener);
  };
}

export function notifyUnauthorized() {
  unauthorizedListeners.forEach((l) => l());
}
