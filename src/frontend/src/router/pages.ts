import { lazy } from "react";

// Todo lo protegido va en chunks aparte: ni su código ni sus datos se descargan hasta que
// GET /auth/me confirma la sesión.
export const AppLayout = lazy(() => import("@/components/layout/AppLayout"));
export const CatalogPage = lazy(
  () => import("@/features/marketplace/catalog/CatalogPage"),
);
export const ProfilePage = lazy(() => import("@/features/profile/ProfilePage"));
export const AdminPage = lazy(() => import("@/features/admin/AdminPage"));
