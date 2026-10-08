import type { AuthRouteState } from "@/types/types";
import { createContext, useContext } from "react";

export type StepDirection = "forward" | "back";

// Coreografía de las páginas de acceso. La implementa AuthLayout; los formularios la piden.
export interface AuthMotion {
  // Cambia de estado dentro de la misma página: anima la salida, aplica `update` y anima la entrada.
  swap: (update: () => void, direction?: StepDirection) => void;
  // Navega entre /login, /registro y /recuperar conservando ?redirect=: el panel se reorganiza con Flip.
  go: (pathname: string, state?: AuthRouteState) => void;
  // Sacudida breve del formulario (error de credenciales).
  shake: () => void;
  // Detiene / reanuda el vehículo de la escena (bloqueo por intentos).
  stopScene: () => void;
  resumeScene: () => void;
  // Éxito al iniciar sesión: el vehículo acelera y sale, el panel ocupa la pantalla. Resuelve al terminar.
  celebrate: () => Promise<void>;
  // "Revisa tu correo": un sobre se dibuja en el panel y sale volando hacia arriba.
  mailSent: () => void;
}

// Sin provider (p. ej. en pruebas) los cambios se aplican sin animación.
export const AuthMotionContext = createContext<AuthMotion>({
  swap: (update) => update(),
  go: () => {},
  shake: () => {},
  stopScene: () => {},
  resumeScene: () => {},
  celebrate: () => Promise.resolve(),
  mailSent: () => {},
});

export const useAuthMotion = () => useContext(AuthMotionContext);
