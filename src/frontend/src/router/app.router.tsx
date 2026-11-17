import { createBrowserRouter, Navigate, Outlet } from "react-router";

import OfflineBanner from "@/components/custom/OfflineBanner";
import MotionRoot from "@/components/layout/MotionRoot";
import ProtectedRoute from "@/components/layout/ProtectedRoute";
import ScrollToTop from "@/components/layout/ScrollToTop";
import { ActivatePage } from "@/features/auth/activate/ActivatePage";
import { LoginPage } from "@/features/auth/login/LoginPage";
import { RecoverPage } from "@/features/auth/recover/RecoverPage";
import { RegisterPage } from "@/features/auth/register/RegisterPage";
import AuthLayout from "@/features/auth/shared/layout/AuthLayout";
import { NotFoundPage } from "@/features/errors/ErrorPages";
import RouteError from "@/features/errors/RouteError";
import {
  AdminPage,
  AppLayout,
  CatalogPage,
  HostReservationsPage,
  HostRoute,
  LegalPage,
  MyVehiclesPage,
  ProfilePage,
  VehicleFormPage,
  ReservationPage,
  ReservePage,
  SearchPage,
  VehiclePage,
} from "./pages";

// Navegación por URL: cada pantalla, sección, filtro, página y diálogo vive en la ruta o en su
// query string, así que todo se puede enlazar, recargar y recorrer con atrás/adelante.
export const router = createBrowserRouter([
  {
    element: (
      <MotionRoot>
        <ScrollToTop />
        <OfflineBanner />
        <Outlet />
      </MotionRoot>
    ),
    // Un error inesperado al pintar cualquier ruta: página de error con reintento.
    errorElement: (
      <MotionRoot>
        <RouteError />
      </MotionRoot>
    ),
    children: [
      // Públicas para todos (con o sin sesión): textos legales.
      { path: "terminos", element: <LegalPage doc="terminos" /> },
      { path: "privacidad", element: <LegalPage doc="privacidad" /> },
      // Públicas: solo las páginas de acceso.
      {
        path: "/auth",
        element: <AuthLayout />,
        children: [
          { index: true, element: <Navigate to="/auth/login" replace /> },
          { path: "login", element: <LoginPage /> },
          { path: "registro", element: <RegisterPage /> },
          { path: "recuperar", element: <RecoverPage /> },
          { path: "activar", element: <ActivatePage /> },
        ],
      },
      // Todo lo demás exige sesión válida.
      {
        element: <ProtectedRoute />,
        children: [
          {
            element: <AppLayout />,
            children: [
              // El catálogo es solo del rol Estándar: el Administrador va a su panel.
              {
                element: <ProtectedRoute requireStandard />,
                children: [
                  { index: true, element: <CatalogPage /> },
                  { path: "buscar", element: <SearchPage /> },
                  { path: "vehiculos/:id", element: <VehiclePage /> },
                  { path: "reservar/:vehiculoId", element: <ReservePage /> },
                  // Anfitrión: exige el permiso de publicar (si no, 403).
                  {
                    path: "anfitrion",
                    element: <HostRoute />,
                    children: [
                      {
                        index: true,
                        element: <Navigate to="vehiculos" replace />,
                      },
                      { path: "vehiculos", element: <MyVehiclesPage /> },
                      { path: "vehiculos/nuevo", element: <VehicleFormPage /> },
                      { path: "vehiculos/:id", element: <VehicleFormPage /> },
                      { path: "reservas", element: <HostReservationsPage /> },
                    ],
                  },
                ],
              },
              // Lo ven las dos partes de la reserva y el Administrador.
              { path: "reservas/:id", element: <ReservationPage /> },
              // Cualquier otra ruta: 404 dentro del layout, con el header visible.
              { path: "*", element: <NotFoundPage /> },
              { path: "perfil/*", element: <ProfilePage /> },
            ],
          },
          // Panel de administración: exige además el rol; layout de consola propio.
          {
            element: <ProtectedRoute requireAdmin />,
            children: [{ path: "admin/*", element: <AdminPage /> }],
          },
        ],
      },
    ],
  },
]);
