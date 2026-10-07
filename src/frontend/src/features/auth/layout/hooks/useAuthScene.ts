import type { ContextSafeFunc } from "@gsap/react";
import type { StageRefs } from "../types";
import { DURATION, gsap, prefersReducedMotion } from "@/lib/gsap";
import { ALONG, isDesktop, LEG } from "../scene";

export const useAuthScene = (
  contextSafe: ContextSafeFunc,
  { stopped, loop, panelRef, columnRef, contentRef }: StageRefs,
) => {
  // Bloqueo por intentos: el vehículo frena hasta detenerse; al vencer, sigue su camino.
  const stopScene = () =>
    contextSafe(() => {
      stopped.current = true;
      const scene = loop.current;
      if (scene)
        gsap.to(scene, {
          timeScale: 0,
          duration: 0.6,
          ease: "power2.out",
          onComplete: () => void scene.pause(),
        });
    })();

  const resumeScene = () =>
    contextSafe(() => {
      stopped.current = false;
      const scene = loop.current;
      if (!scene) return;
      scene.resume();
      gsap.to(scene, { timeScale: 1, duration: 0.6, ease: "power2.in" });
    })();

  // Éxito al iniciar sesión (~700 ms): el vehículo en ruta acelera y sale, el formulario se
  // desvanece y el panel ocupa toda la pantalla. La navegación la hace la sesión al confirmarse.
  const celebrate = () =>
    new Promise<void>((resolve) => {
      contextSafe(() => {
        const panel = panelRef.current;
        const column = columnRef.current;
        if (!panel || !column || prefersReducedMotion()) {
          gsap.to(contentRef.current, {
            opacity: 0,
            duration: DURATION.reduced,
            onComplete: resolve,
          });
          return;
        }
        const tl = gsap.timeline({ onComplete: resolve });
        const scene = loop.current;
        if (scene) {
          const legs = gsap.utils.toArray<SVGGElement>("[data-vehicle]");
          const t = scene.time() % (LEG * legs.length);
          const leg = Math.floor(t / LEG);
          const start = gsap.parseEase("sine.inOut")((t - leg * LEG) / LEG);
          scene.pause();
          tl.to(
            legs[leg],
            {
              motionPath: { ...ALONG, autoRotate: true, start, end: 1 },
              duration: 0.6,
              ease: "power3.in",
            },
            0,
          );
        }
        tl.to(column, { opacity: 0, duration: 0.25, ease: "power1.out" }, 0);
        if (isDesktop())
          tl.to(
            panel,
            { width: "100%", duration: 0.7, ease: "power3.inOut" },
            0,
          );
        else
          tl.to(
            panel,
            { height: window.innerHeight, duration: 0.7, ease: "power3.inOut" },
            0,
          );
      })();
    });

  return {
    scene: {
      stopScene,
      resumeScene,
      celebrate,
    },
  };
};
