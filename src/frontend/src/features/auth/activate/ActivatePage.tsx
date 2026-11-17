import { useQuery } from "@tanstack/react-query";
import { CircleAlert, LoaderCircle } from "lucide-react";
import { useNavigate, useSearchParams } from "react-router";
import { z } from "zod";

import * as authApi from "@/api/auth.api";
import { errorMessage } from "@/api/errors";
import { Alert, AlertDescription } from "@/components/ui/alert";
import type { AuthRouteState } from "@/types/types";
import AuthPanel from "../shared/components/AuthPanel";
import { ModalFoot } from "../shared/components/ModalFoot";
import { SubmitButton } from "../shared/components/SubmitButton";
import { TextLink } from "../shared/components/TextLink";

const linkSchema = z.object({
  userId: z.uuid(),
  token: z.string().min(1),
});

// /auth/activar?userId=…&token=…: el enlace del correo de activación. Se activa al abrirlo y,
// si sale bien, lleva al login con el aviso de cuenta activa.
export const ActivatePage = () => {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const link = linkSchema.safeParse({
    userId: params.get("userId"),
    token: params.get("token"),
  });

  const activation = useQuery({
    queryKey: ["activate", params.get("userId"), params.get("token")],
    queryFn: () =>
      link.success
        ? authApi.activate(link.data.userId, link.data.token).then(() => true)
        : Promise.reject(new Error("invalid-link")),
    enabled: link.success,
    retry: false,
    staleTime: Infinity,
  });

  const toLogin = (state?: AuthRouteState) =>
    navigate("/auth/login", { state, replace: true });

  if (link.success && activation.isPending) {
    return (
      <AuthPanel title="Activando tu cuenta">
        <p
          role="status"
          className="flex items-center gap-2.5 text-[15px] text-muted-foreground"
        >
          <LoaderCircle className="size-4 animate-spin" />
          Un momento…
        </p>
      </AuthPanel>
    );
  }

  if (activation.isSuccess) {
    return (
      <AuthPanel
        title="Tu cuenta está activa"
        greeting="Te damos la bienvenida a Wheelby"
      >
        <p className="mb-6 text-[15px] text-muted-foreground">
          Ya puedes iniciar sesión con tu correo y tu contraseña.
        </p>
        <SubmitButton
          type="button"
          loading={false}
          onClick={() => toLogin({ notice: "activated" })}
        >
          Iniciar sesión
        </SubmitButton>
      </AuthPanel>
    );
  }

  return (
    <AuthPanel title="No pudimos activar tu cuenta">
      <Alert variant="destructive" className="mb-6">
        <CircleAlert />
        <AlertDescription>
          {link.success
            ? errorMessage(activation.error)
            : "El enlace de activación no es válido o está incompleto."}
        </AlertDescription>
      </Alert>
      <p className="text-[14.5px] text-muted-foreground">
        Inicia sesión con tu correo: si la cuenta sigue pendiente, podrás pedir
        un enlace nuevo.
      </p>
      <ModalFoot>
        <TextLink onClick={() => toLogin()}>Ir a iniciar sesión</TextLink>
      </ModalFoot>
    </AuthPanel>
  );
};
