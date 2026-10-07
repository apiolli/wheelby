import { useSession } from "@/stores/session.store";
import type { AuthRouteState } from "@/types/types";
import { useLocation } from "react-router";
import LoginForm from "./login/LoginPage";
import { useAuthMotion } from "./motion.context";

// Páginas públicas de acceso. Viven dentro de AuthLayout, que pone el panel de marca y la
// coreografía; aquí solo se conectan los formularios con la navegación.

function useRouteState() {
  const location = useLocation();
  return { key: location.key, state: (location.state ?? {}) as AuthRouteState };
}

export const LoginPage = () => {
  const { key, state } = useRouteState();
  const { go } = useAuthMotion();
  const { showToast } = useSession();
  return (
    <LoginForm
      key={key}
      email={state.email}
      notice={state.notice}
      onToast={showToast}
      onSwitch={() => go("/registro")}
      onRecover={(email) => go("/recuperar", { email })}
    />
  );
};

// export function RegisterPage() {
//   const { key } = useRouteState();
//   const { go } = useAuthMotion();
//   const { showToast } = useSession();
//   return (
//     <RegisterForm
//       key={key}
//       onToast={showToast}
//       onSwitch={(email) => go("/login", { email })}
//     />
//   );
// }

// export function RecoverPage() {
//   const { key, state } = useRouteState();
//   const { go } = useAuthMotion();
//   const { showToast } = useSession();
//   return (
//     <RecoverForm
//       key={key}
//       email={state.email}
//       onToast={showToast}
//       onBack={() => go("/login", { email: state.email })}
//       onDone={(email) => go("/login", { email, notice: "password-reset" })}
//     />
//   );
// }
