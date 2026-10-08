import type { StageRefs } from "../types";
import { Flip } from "gsap/Flip";
import { SplitText } from "gsap/SplitText";
import {
  DURATION,
  EASE,
  gsap,
  prefersReducedMotion,
  useGSAP,
} from "@/lib/gsap";
import { useLocation } from "react-router";
import { ALONG, isDesktop, LEG, STOPS } from "../scene";

export const useAuthMotionForms = (refs: StageRefs, side: string) => {
  const { key } = useLocation();

  // Campos del formulario visible en orden, con el botón principal aparte para que entre al final.
  const formTargets = () => {
    const nodes = [
      ...(refs.contentRef.current?.querySelectorAll<HTMLElement>(
        "[data-enter], form > *",
      ) ?? []),
    ];
    const isPrimary = (el: HTMLElement) =>
      el.matches('button[type="submit"]') ||
      !!el.querySelector('button[type="submit"]');
    return {
      fields: nodes.filter((el) => !isPrimary(el)),
      primary: nodes.filter(isPrimary),
    };
  };

  // Entrada del formulario: suben 16 px con fade en stagger y el botón principal al final. Solo
  // opacidad y transform: se puede escribir y enviar desde el primer momento.
  const enterForm = (tl: gsap.core.Timeline, at: number) => {
    const { fields, primary } = formTargets();
    const from = { opacity: 0, y: 16 };
    const to = {
      opacity: 1,
      y: 0,
      duration: 0.4,
      ease: EASE.enter,
      clearProps: "opacity,transform",
    };
    if (fields.length) tl.fromTo(fields, from, { ...to, stagger: 0.05 }, at);
    if (primary.length)
      tl.fromTo(
        primary,
        from,
        to,
        at + Math.max(fields.length - 1, 0) * 0.05 + 0.1,
      );
  };

  useGSAP(
    () => {
      const pending = refs.pendingGo.current;
      const content = refs.contentRef.current;
      if (!pending || !content) return;
      refs.pendingGo.current = null;
      gsap.set(content, { opacity: 1 });
      const tl = gsap.timeline({
        onComplete: () => void (refs.busy.current = false),
      });
      if (pending.reduce) {
        tl.from(content, { opacity: 0, duration: DURATION.reduced });
        return;
      }
      if (pending.flip)
        tl.add(
          Flip.from(pending.flip, { duration: 0.6, ease: "power3.inOut" }),
          0,
        );
      enterForm(tl, 0.15);
    },
    { dependencies: [key], scope: refs.rootRef },
  );

  const { contextSafe } = useGSAP(
    () => {
      const reduce = prefersReducedMotion();
      const vehicles = gsap.utils.toArray<SVGGElement>("[data-vehicle]");
      const words = gsap.utils.toArray<HTMLElement>("[data-word]");

      gsap.utils.toArray<SVGCircleElement>("[data-stop]").forEach((stop, i) => {
        gsap.set(stop, {
          motionPath: { ...ALONG, start: STOPS[i], end: STOPS[i] },
        });
      });

      // Movimiento reducido: escena estática (un vehículo a mitad de ruta) y fades de 150 ms.
      if (reduce) {
        gsap.set(vehicles[0], {
          opacity: 1,
          motionPath: { ...ALONG, autoRotate: true, start: 0.55, end: 0.55 },
        });
        gsap.from(refs.contentRef.current, {
          opacity: 0,
          duration: DURATION.reduced,
        });
        return;
      }

      // Escena en bucle: cada vehículo recorre la ruta (~6 s) y la palabra del titular cambia con él.
      const scene = gsap.timeline({ repeat: -1, paused: true });
      vehicles.forEach((vehicle, i) => {
        const at = i * LEG;
        const prev = words[(i + words.length - 1) % words.length];
        scene
          .set(vehicle, { opacity: 1 }, at)
          .to(
            vehicle,
            {
              motionPath: { ...ALONG, autoRotate: true },
              duration: LEG,
              ease: "sine.inOut",
              immediateRender: false,
            },
            at,
          )
          .set(vehicle, { opacity: 0 }, at + LEG)
          .to(
            prev,
            { yPercent: -100, opacity: 0, duration: 0.45, ease: "power2.in" },
            at,
          )
          .fromTo(
            words[i],
            { yPercent: 100, opacity: 0 },
            { yPercent: 0, opacity: 1, duration: 0.45, ease: EASE.enter },
            at + 0.2,
          );
      });
      refs.loop.current = scene;

      // Coreografía de entrada (~1.2 s): barrido del panel desde su lado → la ruta se dibuja →
      // el titular por caracteres → los campos en stagger → el botón principal.
      const desktop = isDesktop();
      const hidden = !desktop
        ? "inset(0% 0% 100% 0%)"
        : side === "right"
          ? "inset(0% 0% 0% 100%)"
          : "inset(0% 100% 0% 0%)";
      // words + chars: los caracteres se animan sueltos, pero una palabra nunca se parte entre líneas.
      const split = SplitText.create("[data-split]", { type: "words,chars" });
      const intro = gsap.timeline({ defaults: { ease: EASE.enter } });
      intro
        .fromTo(
          refs.panelRef.current,
          { clipPath: hidden },
          {
            clipPath: "inset(0% 0% 0% 0%)",
            duration: 0.5,
            ease: "power3.inOut",
            clearProps: "clipPath",
          },
          0,
        )
        .from(
          "[data-route]",
          { drawSVG: "0%", duration: 0.6, ease: "power2.inOut" },
          0.3,
        )
        .from(
          "[data-stop]",
          {
            scale: 0,
            transformOrigin: "50% 50%",
            duration: 0.3,
            stagger: 0.08,
          },
          0.6,
        )
        .from(
          split.chars,
          { opacity: 0, y: 14, duration: 0.35, stagger: 0.012 },
          0.4,
        )
        .from(
          ["[data-brand-logo]", "[data-trust]", words[0]],
          { opacity: 0, duration: 0.4 },
          0.45,
        )
        // El bucle arranca con la ruta ya dibujada, pasado el cambio de palabra inicial.
        .add(() => void scene.play(0.65), 0.9);
      enterForm(intro, 0.25);

      // La escena se pausa con la pestaña oculta.
      const onVisibility = () => {
        if (document.hidden) scene.pause();
        else if (!refs.stopped.current && intro.progress() === 1)
          scene.resume();
      };
      document.addEventListener("visibilitychange", onVisibility);
      return () =>
        document.removeEventListener("visibilitychange", onVisibility);
    },
    { scope: refs.rootRef },
  );

  return {
    forms: {
      formTargets,
      enterForm,
    },
    contextSafe,
  };
};
