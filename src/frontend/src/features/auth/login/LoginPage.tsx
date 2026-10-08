import { useRouteState } from "../shared/hooks/useRouteState";
import { useAuthMotion } from "../shared/motion.context";
import { LoginForm } from "./components/LoginForm";

export const LoginPage = () => {
  const { key, state } = useRouteState();
  const { go } = useAuthMotion();
  return (
    <LoginForm
      key={key}
      email={state.email}
      notice={state.notice}
      onSwitch={() => go("/auth/registro")}
      onRecover={(email) => go("/auth/recuperar", { email })}
    />
  );
};
