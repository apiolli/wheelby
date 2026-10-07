import { useAuthStage } from "../hooks/useAuthStage";
import { AuthMotionContext } from "../../motion.context";
import { Outlet } from "react-router";
import BrandPanel from "./BrandPanel";

// Pantalla dividida: panel de marca (55 %) y formulario (45 %). El panel va a la izquierda en
// /login y /recuperar y a la derecha en /registro; en móvil es una franja superior.
export const AuthStage = () => {
  const {
    rootRef,
    panelRef,
    columnRef,
    contentRef,
    side,
    mailSent,
    shake,
    stopScene,
    resumeScene,
    celebrate,
    swap,
    go,
  } = useAuthStage();

  return (
    <AuthMotionContext
      value={{ swap, go, shake, stopScene, resumeScene, celebrate, mailSent }}
    >
      <div
        ref={rootRef}
        className="flex min-h-dvh flex-col overflow-x-clip bg-background md:flex-row"
      >
        <BrandPanel
          ref={panelRef}
          className={side === "right" ? "md:order-last" : undefined}
        />
        <main
          ref={columnRef}
          className="flex min-w-0 flex-1 justify-center px-5 pt-8 pb-12 md:items-center md:px-10 md:py-12"
        >
          <div ref={contentRef} className="w-full max-w-100">
            <Outlet />
          </div>
        </main>
      </div>
    </AuthMotionContext>
  );
};
