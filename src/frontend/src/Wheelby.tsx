import { Suspense } from "react";
import { RouterProvider } from "react-router";

import { Toaster } from "@/components/ui/sonner";
import SessionProvider from "./stores/SessionProvider";
import Splash from "./components/custom/Splash";
import { router } from "./router/app.router";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";

// Todo lo protegido va en chunks aparte: ni el código ni los datos del catálogo (fotos incluidas)
// se descargan hasta que GET /auth/me confirma la sesión.
// const AppLayout = lazy(() => import('@/components/layout/AppLayout'))
// const LandingPage = lazy(() => import('@/components/landing/LandingPage'))
// const ProfilePage = lazy(() => import('@/components/profile/ProfilePage'))
// const AdminPage = lazy(() => import('@/components/admin/AdminPage'))

const queryClient = new QueryClient();

export const Wheelby = () => {
  return (
    <>
      <QueryClientProvider client={queryClient}>
        <SessionProvider>
          <RouterProvider router={router} />
          <Toaster />
          {/* <ScrollToTop /> */}
          <Suspense fallback={<Splash />} />
        </SessionProvider>

        <ReactQueryDevtools />
      </QueryClientProvider>
    </>
  );
};

// Cada página nueva empieza arriba (el router declarativo no restaura el scroll).
// function ScrollToTop() {
//   const { pathname } = useLocation();
//   useEffect(() => {
//     window.scrollTo(0, 0);
//   }, [pathname]);
//   return null;
// }
