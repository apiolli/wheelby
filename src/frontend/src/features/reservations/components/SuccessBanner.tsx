import { m } from "motion/react";

// Aviso tras enviar la solicitud, con un check que se dibuja.
export default function SuccessBanner({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <m.div
      role="status"
      initial={{ opacity: 0, y: -8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.35 }}
      className="flex items-center gap-4 rounded-[18px] bg-success-soft px-5 py-4 text-[14.5px] text-success"
    >
      <svg viewBox="0 0 32 32" className="size-9 shrink-0" aria-hidden="true">
        <m.circle
          cx="16"
          cy="16"
          r="14"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          initial={{ pathLength: 0 }}
          animate={{ pathLength: 1 }}
          transition={{ duration: 0.45, ease: "easeOut" }}
        />
        <m.path
          d="M10 16.5 L14 20.5 L22 12"
          fill="none"
          stroke="currentColor"
          strokeWidth="2.5"
          strokeLinecap="round"
          strokeLinejoin="round"
          initial={{ pathLength: 0 }}
          animate={{ pathLength: 1 }}
          transition={{ duration: 0.3, delay: 0.4, ease: "easeOut" }}
        />
      </svg>
      <p className="font-semibold">{children}</p>
    </m.div>
  );
}
