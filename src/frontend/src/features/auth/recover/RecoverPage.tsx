import { useSession } from "@/stores/session.store";
import { useRouteState } from "../shared/hooks/useRouteState";
import { useAuthMotion } from "../shared/motion.context";
import RecoverForm from "./components/RecoverForm";

export const RecoverPage = () => {
  const { key, state } = useRouteState();
  const { go } = useAuthMotion();
  const { showToast } = useSession();
  return (
    <RecoverForm
      key={key}
      email={state.email}
      onToast={showToast}
      onBack={() => go("/auth/login", { email: state.email })}
      onDone={(email) => go("/auth/login", { email, notice: "password-reset" })}
    />
  );
};
