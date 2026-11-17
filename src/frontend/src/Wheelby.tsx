import { Suspense } from "react";
import { RouterProvider } from "react-router";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { isAxiosError } from "axios";

import Splash from "@/components/custom/Splash";
import { Toaster } from "@/components/ui/sonner";
import { router } from "./router/app.router";

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      // Un 4xx no mejora reintentando (sesión, permisos, validación); un fallo de red, quizá sí.
      retry: (count, error) =>
        count < 2 &&
        !(isAxiosError(error) && (error.response?.status ?? 500) < 500),
    },
  },
});

export const Wheelby = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <Suspense fallback={<Splash />}>
        <RouterProvider router={router} />
      </Suspense>
      <Toaster />
      <ReactQueryDevtools />
    </QueryClientProvider>
  );
};
