import { createBrowserRouter, Navigate } from "react-router";
import AuthLayout from "@/features/auth/layout/AuthLayout";
import { LoginPage } from "@/features/auth/pages";

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
    ],
  },
  {
    path: "*",
    element: <Navigate to="/auth/login" replace />,
  },
  {
    path: "/prueba",
    element: (
      <>
        <h1 className="text-3xl font-bold">ESTO ES UNA PRUEBA GRANDE</h1>
      </>
    ),
  },
]);
