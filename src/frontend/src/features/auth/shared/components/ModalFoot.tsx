import type { ReactNode } from "react";

export const ModalFoot = ({ children }: { children: ReactNode }) => {
  return (
    <p className="mt-5 border-t border-border-soft pt-4.5 text-center text-sm text-muted-foreground">
      {children}
    </p>
  );
};
