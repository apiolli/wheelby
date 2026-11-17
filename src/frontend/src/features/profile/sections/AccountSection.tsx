import type { ReactNode } from "react";
import { ShieldCheck, UserRound } from "lucide-react";

import SectionHeading from "@/components/custom/SectionHeading";
import { Badge } from "@/components/ui/badge";
import { initial } from "@/lib/format";
import { ROLE_LABEL } from "@/lib/labels";
import { isAdmin, useSession } from "@/stores/session.store";

// Solo lectura: la API no permite editar el nombre ni el correo.
export default function AccountSection() {
  const user = useSession((s) => s.user);
  if (!user) return null;
  const admin = isAdmin(user);

  return (
    <>
      <SectionHeading
        title="Cuenta"
        description="Los datos con los que te registraste en Wheelby."
      />
      <section className="rounded-2xl border border-border p-6 md:p-8">
        <div className="flex items-center gap-4">
          <span className="grid size-16 shrink-0 place-items-center rounded-full bg-primary text-2xl font-bold text-primary-foreground">
            {initial(user.fullName)}
          </span>
          <div className="min-w-0">
            <h2 className="truncate text-xl leading-tight font-bold tracking-tight">
              {user.fullName}
            </h2>
            <Badge variant={admin ? "primary" : "soft"} className="mt-1.5">
              {admin ? (
                <ShieldCheck strokeWidth={2.2} />
              ) : (
                <UserRound strokeWidth={2.2} />
              )}
              {ROLE_LABEL[user.role]}
            </Badge>
          </div>
        </div>

        <dl className="mt-6 grid gap-x-8 gap-y-5 border-t border-border-soft pt-6 sm:grid-cols-2">
          <Field label="Nombre completo">{user.fullName}</Field>
          <Field label="Correo electrónico">
            <span className="break-all">{user.email}</span>
          </Field>
          <Field label="Rol">{ROLE_LABEL[user.role]}</Field>
        </dl>
      </section>
    </>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <dt className="text-[13px] text-muted-foreground">{label}</dt>
      <dd className="mt-0.5 font-semibold">{children}</dd>
    </div>
  );
}
