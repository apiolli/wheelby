// @refresh reset
import { useRef } from "react";
import { useLocation } from "react-router";
import { useAuthFeedback } from "./useAuthFeedback";
import { useAuthScene } from "./useAuthScene";
import { useAuthMotionForms } from "./useAuthMotionForms";
import { useAuthTransition } from "./useAuthTransition";

export const useAuthStage = () => {
  const { pathname } = useLocation();
  const side = pathname.startsWith("/registro") ? "right" : "left";

  const refs = {
    rootRef: useRef<HTMLDivElement>(null),
    panelRef: useRef<HTMLElement>(null),
    columnRef: useRef<HTMLElement>(null),
    contentRef: useRef<HTMLDivElement>(null),
    loop: useRef<gsap.core.Timeline | null>(null),
    stopped: useRef(false),
    busy: useRef(false),
    pendingGo: useRef<{
      flip: Flip.FlipState | null;
      reduce: boolean;
    } | null>(null),
  };

  const { contextSafe, forms } = useAuthMotionForms(refs, side);
  const { feedback } = useAuthFeedback(contextSafe, refs);
  const { scene } = useAuthScene(contextSafe, refs);
  const {
    transition: { swap, go },
  } = useAuthTransition(refs, contextSafe);

  return { ...feedback, ...scene, swap, go, ...refs, ...forms, side };
};
