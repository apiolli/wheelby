import type { AuthRouteState } from "@/types/types";
import { useLocation } from "react-router";

export const useRouteState = () => {
  const location = useLocation();
  return { key: location.key, state: (location.state ?? {}) as AuthRouteState };
};
