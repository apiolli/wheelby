import {
  Bike,
  Car,
  LayoutGrid,
  Motorbike,
  type LucideIcon,
} from "lucide-react";

// Datos de ejemplo para el prototipo. Fotos de Unsplash; si alguna no carga,
// la tarjeta muestra una ilustración de respaldo (ver VehicleCard).

export type VehicleType = "auto" | "moto" | "ligero";
export type CategoryId = "all" | VehicleType;
export type Badge = "favorite" | "verified";

export interface Vehicle {
  id: string;
  type: VehicleType;
  title: string;
  city: string;
  model: string;
  price: number;
  rating: number;
  trips: number;
  badge?: Badge;
  photo: string;
}

export interface Section {
  id: string;
  category: VehicleType;
  title: string;
  subtitle: string;
  items: Vehicle[];
}

type RawVehicle = Omit<Vehicle, "id" | "title">;
type Seed = Omit<RawVehicle, "type" | "city"> & { city?: string };

const u = (id: string) =>
  `https://images.unsplash.com/${id}?auto=format&fit=crop&w=640&h=480&q=70`;

export const CATEGORIES: { id: CategoryId; label: string; icon: LucideIcon }[] =
  [
    { id: "all", label: "Todo", icon: LayoutGrid },
    { id: "auto", label: "Autos", icon: Car },
    { id: "moto", label: "Motos", icon: Motorbike },
    { id: "ligero", label: "Ligeros", icon: Bike },
  ];

export const TYPE_LABEL: Record<VehicleType, string> = {
  auto: "Auto",
  moto: "Moto",
  ligero: "Bicicleta",
};

const autosSantiago: RawVehicle[] = (
  [
    {
      model: "Toyota Corolla 2022",
      price: 3800,
      rating: 4.92,
      trips: 128,
      badge: "favorite",
      photo: u("photo-1549317661-bd32c8ce0db2"),
    },
    {
      model: "Honda Civic 2021",
      price: 3500,
      rating: 4.88,
      trips: 96,
      badge: "verified",
      photo: u("photo-1494976388531-d1058494cdd8"),
    },
    {
      model: "Hyundai Tucson 2023",
      price: 5200,
      rating: 4.95,
      trips: 64,
      badge: "favorite",
      photo: u("photo-1503376780353-7e6692767b70"),
    },
    {
      model: "Kia Picanto 2020",
      price: 2400,
      rating: 4.79,
      trips: 211,
      photo: u("photo-1552519507-da3b142c6e3d"),
    },
    {
      model: "Mazda CX-5 2022",
      price: 5600,
      rating: 4.9,
      trips: 47,
      badge: "verified",
      photo: u("photo-1542362567-b07e54358753"),
    },
    {
      model: "Nissan Sentra 2021",
      price: 3200,
      rating: 4.83,
      trips: 88,
      photo: u("photo-1580273916550-e323be2ae537"),
    },
    {
      model: "Toyota Hilux 2022",
      price: 6900,
      rating: 4.97,
      trips: 35,
      badge: "favorite",
      photo: u("photo-1605559424843-9e4c228bf1c2"),
    },
    {
      model: "Suzuki Swift 2023",
      price: 2800,
      rating: 4.86,
      trips: 72,
      photo: u("photo-1502877338535-766e1452684a"),
    },
    {
      model: "Jeep Wrangler 2019",
      price: 7800,
      rating: 4.91,
      trips: 53,
      photo: u("photo-1533473359331-0135ef1b58bf"),
    },
  ] satisfies Seed[]
).map((v) => ({ ...v, type: "auto", city: "Santiago" }));

const motosFinde: RawVehicle[] = (
  [
    {
      model: "Honda CB190R 2022",
      price: 1500,
      rating: 4.9,
      trips: 77,
      badge: "favorite",
      city: "Santiago",
      photo: u("photo-1558981806-ec527fa84c39"),
    },
    {
      model: "Yamaha MT-03 2023",
      price: 2600,
      rating: 4.95,
      trips: 41,
      badge: "verified",
      city: "Santo Domingo",
      photo: u("photo-1568772585407-9361f9bf3a87"),
    },
    {
      model: "Suzuki Gixxer 150",
      price: 1200,
      rating: 4.81,
      trips: 132,
      city: "La Vega",
      photo: u("photo-1449426468159-d96dbf08f19f"),
    },
    {
      model: "Vespa Primavera 150",
      price: 1800,
      rating: 4.93,
      trips: 58,
      badge: "favorite",
      city: "Puerto Plata",
      photo: u("photo-1502744688674-c619d1586c9e"),
    },
    {
      model: "KTM Duke 390 2022",
      price: 3100,
      rating: 4.87,
      trips: 29,
      city: "Santiago",
      photo: u("photo-1609630875171-b1321377ee65"),
    },
    {
      model: "Kawasaki Z400 2021",
      price: 2900,
      rating: 4.84,
      trips: 36,
      badge: "verified",
      city: "Santo Domingo",
      photo: u("photo-1591637333184-19aa84b3e01f"),
    },
    {
      model: "Honda Navi 2023",
      price: 950,
      rating: 4.78,
      trips: 190,
      city: "Jarabacoa",
      photo: u("photo-1547549082-6bc09f2049ae"),
    },
    {
      model: "Royal Enfield Classic 350",
      price: 2700,
      rating: 4.96,
      trips: 22,
      city: "Santiago",
      photo: u("photo-1580310614729-ccd69652491d"),
    },
  ] satisfies Seed[]
).map((v) => ({ ...v, type: "moto", city: v.city! }));

const ligeros: RawVehicle[] = (
  [
    {
      model: "Trek Marlin 7 · Montaña",
      price: 900,
      rating: 4.94,
      trips: 84,
      badge: "favorite",
      city: "Jarabacoa",
      photo: u("photo-1485965120184-e220f721d03e"),
    },
    {
      model: "Specialized Sirrus · Urbana",
      price: 750,
      rating: 4.88,
      trips: 61,
      city: "Santo Domingo",
      photo: u("photo-1532298229144-0ec0c57515c7"),
    },
    {
      model: "Brompton C Line · Plegable",
      price: 1100,
      rating: 4.97,
      trips: 33,
      badge: "verified",
      city: "Santo Domingo",
      photo: u("photo-1507035895480-2b3156c31fc8"),
    },
    {
      model: "Cruiser de playa",
      price: 600,
      rating: 4.85,
      trips: 142,
      city: "Cabarete",
      photo: u("photo-1571068316344-75bc76f77890"),
    },
    {
      model: "Giant Escape 3 · Híbrida",
      price: 700,
      rating: 4.8,
      trips: 57,
      city: "Santiago",
      photo: u("photo-1511994298241-608e28f14fde"),
    },
    {
      model: "Triciclo de carga",
      price: 850,
      rating: 4.9,
      trips: 18,
      badge: "favorite",
      city: "Santiago",
      photo: u("photo-1541625602330-2277a4c46182"),
    },
    {
      model: "Cannondale Trail 5",
      price: 950,
      rating: 4.86,
      trips: 40,
      city: "Constanza",
      photo: u("photo-1576435728678-68d0fbf94e91"),
    },
    {
      model: "Tándem clásico",
      price: 1000,
      rating: 4.92,
      trips: 25,
      city: "Puerto Plata",
      photo: u("photo-1558981285-6f0c94958bb6"),
    },
  ] satisfies Seed[]
).map((v) => ({ ...v, type: "ligero", city: v.city! }));

const playaPuertoPlata: RawVehicle[] = (
  [
    {
      model: "Jeep Renegade 2022",
      price: 5900,
      rating: 4.93,
      trips: 38,
      badge: "favorite",
      photo: u("photo-1519641471654-76ce0107ad1b"),
    },
    {
      model: "Toyota RAV4 2021",
      price: 6200,
      rating: 4.89,
      trips: 71,
      badge: "verified",
      photo: u("photo-1568605117036-5fe5e7bab0b7"),
    },
    {
      model: "Mini Cooper Cabrio 2020",
      price: 7400,
      rating: 4.96,
      trips: 26,
      photo: u("photo-1489824904134-891ab64532f1"),
    },
    {
      model: "Hyundai Accent 2022",
      price: 2900,
      rating: 4.8,
      trips: 115,
      photo: u("photo-1541899481282-d53bffe3c35d"),
    },
    {
      model: "Ford Bronco Sport 2023",
      price: 8900,
      rating: 4.98,
      trips: 19,
      badge: "favorite",
      photo: u("photo-1606664515524-ed2f786a0bd6"),
    },
    {
      model: "Kia Soul 2021",
      price: 3400,
      rating: 4.82,
      trips: 63,
      photo: u("photo-1606152421802-db97b9c7a11b"),
    },
    {
      model: "Chevrolet Tracker 2022",
      price: 4700,
      rating: 4.87,
      trips: 44,
      photo: u("photo-1583121274602-3e2820c69888"),
    },
    {
      model: "Volkswagen Amarok 2021",
      price: 8200,
      rating: 4.9,
      trips: 31,
      badge: "verified",
      photo: u("photo-1617814076367-b759c7d7e738"),
    },
  ] satisfies Seed[]
).map((v) => ({ ...v, type: "auto", city: "Puerto Plata" }));

const withIds = (list: RawVehicle[], prefix: string): Vehicle[] =>
  list.map((v, i) => ({
    ...v,
    id: `${prefix}-${i}`,
    title: `${TYPE_LABEL[v.type]} en ${v.city}`,
  }));

export const SECTIONS: Section[] = [
  {
    id: "autos-santiago",
    category: "auto",
    title: "Autos populares en Santiago de los Caballeros",
    subtitle: "Los más reservados por arrendatarios este mes",
    items: withIds(autosSantiago, "as"),
  },
  {
    id: "motos-finde",
    category: "moto",
    title: "Motos disponibles este fin de semana",
    subtitle: "Recógelas el viernes, devuélvelas el domingo",
    items: withIds(motosFinde, "mf"),
  },
  {
    id: "ligeros",
    category: "ligero",
    title: "Ligeros para moverte sin motor",
    subtitle: "Bicicletas y más, para la ciudad o la montaña",
    items: withIds(ligeros, "lg"),
  },
  {
    id: "playa",
    category: "auto",
    title: "Para escaparte a la playa en Puerto Plata",
    subtitle: "Espacio para el equipaje y aire acondicionado",
    items: withIds(playaPuertoPlata, "pp"),
  },
];

export const ALL_VEHICLES = SECTIONS.flatMap((s) => s.items);
export const findVehicle = (id: string) =>
  ALL_VEHICLES.find((v) => v.id === id);

export const formatPrice = (n: number) => `$${n.toLocaleString("en-US")}`;
