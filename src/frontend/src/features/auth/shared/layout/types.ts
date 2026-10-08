import type { RefObject } from "react";

export interface StageRefs {
  rootRef: RefObject<HTMLDivElement | null>;
  panelRef: RefObject<HTMLElement | null>;
  columnRef: RefObject<HTMLElement | null>;
  contentRef: RefObject<HTMLDivElement | null>;
  loop: RefObject<gsap.core.Timeline | null>;
  stopped: RefObject<boolean>;
  busy: RefObject<boolean>;
  pendingGo: RefObject<{
    flip: Flip.FlipState | null;
    reduce: boolean;
  } | null>;
}
