import type { Transition } from "motion/react";

// Transiciones compartidas de Motion.

// Spring sin rebote: cambios de forma o altura grandes (morph de la búsqueda, altura del header).
export const HEADER_SPRING: Transition = {
  type: "spring",
  bounce: 0,
  duration: 0.45,
};

// Elementos pequeños que se deslizan (segmento activo, subrayado de tabs).
export const SEGMENT_SPRING: Transition = {
  type: "spring",
  bounce: 0.15,
  duration: 0.35,
};

// El contenido aparece cuando la forma ya casi terminó de transformarse y desaparece enseguida,
// para que el texto nunca se vea estirado.
export const CONTENT_FADE = {
  initial: { opacity: 0 },
  animate: { opacity: 1, transition: { duration: 0.2, delay: 0.12 } },
  exit: { opacity: 0, transition: { duration: 0.08 } },
};

// Entrada de contenido: fade + 16 px hacia arriba.
export const ENTER = {
  initial: { opacity: 0, y: 16 },
  animate: { opacity: 1, y: 0 },
  transition: { duration: 0.45, ease: [0.22, 1, 0.36, 1] },
} as const;

// Entrada escalonada de listas (tarjetas del catálogo, reservas…).
export const STAGGER_LIST = {
  initial: "hidden",
  animate: "show",
  variants: {
    hidden: {},
    show: { transition: { staggerChildren: 0.05 } },
  },
} as const;

export const STAGGER_ITEM = {
  variants: {
    hidden: { opacity: 0, y: 20 },
    show: {
      opacity: 1,
      y: 0,
      transition: { duration: 0.45, ease: [0.22, 1, 0.36, 1] },
    },
  },
} as const;

export const SEARCH_LAYOUT_ID = "header-search";
