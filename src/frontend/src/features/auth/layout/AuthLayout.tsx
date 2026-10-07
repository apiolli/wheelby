import { Navigate, useLocation } from "react-router";
import { DrawSVGPlugin } from "gsap/DrawSVGPlugin";
import { Flip } from "gsap/Flip";
import { MotionPathPlugin } from "gsap/MotionPathPlugin";
import { SplitText } from "gsap/SplitText";

import { gsap } from "@/lib/gsap";
import { safeRedirect } from "@/lib/redirect";
import Splash from "@/components/custom/Splash";
import { AuthStage } from "./components/AuthStage";
import { useSession } from "@/stores/session.store";

gsap.registerPlugin(DrawSVGPlugin, Flip, MotionPathPlugin, SplitText);

// Páginas públicas de acceso (/login, /registro, /recuperar). Con sesión llevan directo a la
// ruta de ?redirect= (o a /); mientras se valida un token guardado, pantalla neutra.
export default function AuthLayout() {
  const { status } = useSession();
  const { search } = useLocation();

  if (status === "checking") return <Splash />;
  if (status === "authenticated")
    return <Navigate to={safeRedirect(search)} replace />;
  return <AuthStage />;
}
