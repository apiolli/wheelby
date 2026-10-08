import { Car, Motorbike, Scooter, type LucideIcon } from "lucide-react";

// Escena del panel de marca: cada vehículo recorre la ruta por turnos y la palabra del titular
// cambia con él. Ilustración genérica (íconos de Lucide), sin marcas ni modelos.
export const SCENE: { word: string; icon: LucideIcon }[] = [
  { word: "auto", icon: Car },
  { word: "viaje", icon: Motorbike },
  { word: "fin de semana", icon: Scooter },
];

// Ruta en coordenadas del viewBox 800×900: entra por la izquierda y sale por arriba a la derecha.
export const ROUTE_D =
  "M -60 470 C 120 430 200 300 380 320 S 630 280 650 170 S 770 30 880 0";
export const ROUTE_ID = "auth-route";

// Paradas decorativas sobre la ruta (progreso de 0 a 1).
export const STOPS = [0.22, 0.5, 0.8];

// Segundos que tarda cada vehículo en recorrer la ruta.
export const LEG = 6;

const ROUTE = `#${ROUTE_ID}`;
export const ALONG = {
  path: ROUTE,
  align: ROUTE,
  alignOrigin: [0.5, 0.5] as [number, number],
};

export const isDesktop = () => window.matchMedia("(min-width: 768px)").matches;
