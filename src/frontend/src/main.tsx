import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "./index.css";
import { Wheelby } from "./Wheelby.tsx";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <Wheelby />
  </StrictMode>,
);
