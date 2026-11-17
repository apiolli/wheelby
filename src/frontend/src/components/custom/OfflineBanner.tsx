import { useSyncExternalStore } from "react";
import { AnimatePresence, m } from "motion/react";
import { WifiOff } from "lucide-react";

const subscribe = (notify: () => void) => {
  window.addEventListener("online", notify);
  window.addEventListener("offline", notify);
  return () => {
    window.removeEventListener("online", notify);
    window.removeEventListener("offline", notify);
  };
};

// Franja superior "Sin conexión" mientras el navegador no tiene red. Encima del header.
export default function OfflineBanner() {
  const online = useSyncExternalStore(
    subscribe,
    () => navigator.onLine,
    () => true,
  );
  return (
    <AnimatePresence>
      {!online && (
        <m.div
          role="status"
          initial={{ y: "-100%" }}
          animate={{ y: 0 }}
          exit={{ y: "-100%" }}
          transition={{ type: "spring", bounce: 0, duration: 0.35 }}
          className="fixed inset-x-0 top-0 z-60 flex items-center justify-center gap-2 bg-foreground px-4 pt-[max(0.5rem,env(safe-area-inset-top))] pb-2 text-[13px] font-semibold text-background"
        >
          <WifiOff className="size-4" strokeWidth={2} />
          Sin conexión. Revisa tu red; lo que cargues fallará hasta que vuelva.
        </m.div>
      )}
    </AnimatePresence>
  );
}
