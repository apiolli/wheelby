// @refresh reset
import { useRef } from "react";
import { useLocation } from "react-router";
import { useAuthFeedback } from "./useAuthFeedback";
import { useAuthScene } from "./useAuthScene";
import { useAuthMotionForms } from "./useAuthMotionForms";
import { useAuthTransition } from "./useAuthTransition";
import type { StageRefs } from "../types";

export const useAuthStage = () => {
  const { pathname } = useLocation();
  const side = pathname.startsWith("/registro") ? "right" : "left";

  // Cada useRef va suelto en el nivel superior: dentro del literal del objeto, React Compiler los
  // mete en un bloque memoizado y en el segundo render se saltan (cambia el orden de los hooks).
  const rootRef = useRef<HTMLDivElement>(null);
  const panelRef = useRef<HTMLElement>(null);
  const columnRef = useRef<HTMLElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);
  const loop = useRef<gsap.core.Timeline | null>(null);
  const stopped = useRef(false);
  const busy = useRef(false);
  const pendingGo = useRef<{
    flip: Flip.FlipState | null;
    reduce: boolean;
  } | null>(null);

  const refs: StageRefs = {
    rootRef,
    panelRef,
    columnRef,
    contentRef,
    loop,
    stopped,
    busy,
    pendingGo,
  };

  const { contextSafe, forms } = useAuthMotionForms(refs, side);
  const { feedback } = useAuthFeedback(contextSafe, refs);
  const { scene } = useAuthScene(contextSafe, refs);
  const {
    transition: { swap, go },
  } = useAuthTransition(refs, contextSafe);

  return { ...feedback, ...scene, swap, go, ...refs, ...forms, side };
};
