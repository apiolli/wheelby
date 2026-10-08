import { createBrowserRouter, Navigate } from "react-router";
import AuthLayout from "@/features/auth/shared/layout/AuthLayout";
import { LoginPage } from "@/features/auth/login/LoginPage";
import { RegisterPage } from "@/features/auth/register/RegisterPage";
import { RecoverPage } from "@/features/auth/recover/RecoverPage";

// const SearchPage = lazy(() => import("@/herores/pages/search/SearchPage"));

export const router = createBrowserRouter([
  //   {
  //     path: "/",
  //     element: (
  //       <AuthenticatedRoute>
  //         <MenudoLayout />
  //       </AuthenticatedRoute>
  //     ),
  //     children: [
  //       {
  //         index: true,
  //         element: <Navigate to={"/dashboard"} />,
  //       },
  //       {
  //         path: "/dashboard",
  //         element: <DashboardPage />,
  //       },
  //       {
  //         path: "/expenses",
  //         element: <ExpensesPage />,
  //       },
  //       {
  //         path: "/categories",
  //         element: <CategoriesPage />,
  //       },
  //       {
  //         path: "/paymentMethods",
  //         element: <PaymentMethodsPage />,
  //       },
  //       {
  //         path: "/profile",
  //         element: <ProfilePage />,
  //       },
  //       {
  //         path: "/reports",
  //         element: <ReportsPage />,
  //       },
  //     ],
  //   },
  {
    path: "/auth",
    element: <AuthLayout />,
    children: [
      {
        index: true,
        element: <Navigate to={"/auth/login"} />,
      },
      {
        path: "login",
        element: <LoginPage />,
      },
      {
        path: "registro",
        element: <RegisterPage />,
      },
      {
        path: "recuperar",
        element: <RecoverPage />,
      },
    ],
  },
  {
    path: "*",
    element: <Navigate to="/auth/login" replace />,
  },
]);
