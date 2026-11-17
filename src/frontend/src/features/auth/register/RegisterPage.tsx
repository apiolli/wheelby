import { showToast } from "@/stores/session.store";
import { useRouteState } from "../shared/hooks/useRouteState";
import { useAuthMotion } from "../shared/motion.context";
import RegisterForm from "./components/RegisterForm";

export const RegisterPage = () => {
  const { key } = useRouteState();
  const { go } = useAuthMotion();
  return (
    <RegisterForm
      key={key}
      onToast={showToast}
      onSwitch={(email) => go("/auth/login", { email })}
    />
  );
};
