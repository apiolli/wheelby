import { lazy } from "react";

// Todo lo protegido va en chunks aparte: ni su código ni sus datos se descargan hasta que
// GET /auth/me confirma la sesión.
export const AppLayout = lazy(() => import("@/components/layout/AppLayout"));
export const CatalogPage = lazy(
  () => import("@/features/marketplace/catalog/CatalogPage"),
);
export const ProfilePage = lazy(() => import("@/features/profile/ProfilePage"));
export const AdminPage = lazy(() => import("@/features/admin/AdminPage"));
export const SearchPage = lazy(
  () => import("@/features/marketplace/search/SearchPage"),
);
export const VehiclePage = lazy(
  () => import("@/features/marketplace/vehicle/VehiclePage"),
);
export const ReservePage = lazy(
  () => import("@/features/marketplace/reserve/ReservePage"),
);
export const ReservationPage = lazy(
  () => import("@/features/reservations/ReservationPage"),
);
export const HostRoute = lazy(() => import("@/features/host/HostRoute"));
export const MyVehiclesPage = lazy(
  () => import("@/features/host/vehicles/MyVehiclesPage"),
);
export const VehicleFormPage = lazy(
  () => import("@/features/host/vehicles/VehicleFormPage"),
);
export const HostReservationsPage = lazy(
  () => import("@/features/host/reservations/HostReservationsPage"),
);
