import {
  BadgeCheck,
  CalendarCheck,
  Coins,
  type LucideIcon,
} from "lucide-react";

const ITEMS: { icon: LucideIcon; title: string; text: string }[] = [
  {
    icon: BadgeCheck,
    title: "Anfitriones verificados",
    text: "Solo publica vehículos quien pidió ser anfitrión y fue aprobado por nuestro equipo.",
  },
  {
    icon: CalendarCheck,
    title: "Reservas con confirmación",
    text: "El anfitrión confirma cada reserva y las fechas quedan bloqueadas para que nadie más las tome.",
  },
  {
    icon: Coins,
    title: "Precios en tu moneda",
    text: "Los precios se publican en pesos dominicanos y puedes verlos en dólares o euros al cambio del día.",
  },
];

export default function TrustSection() {
  return (
    <section
      className="mx-gutter mt-12 rounded-[28px] bg-secondary px-[clamp(20px,5vw,64px)] py-12"
      aria-labelledby="trust-title"
    >
      <h2
        id="trust-title"
        className="mb-8 text-center text-[26px] font-bold tracking-tight"
      >
        Alquila con tranquilidad
      </h2>
      <div className="mx-auto grid max-w-270 gap-6 md:grid-cols-3 md:gap-8">
        {ITEMS.map(({ icon: Icon, title, text }) => (
          <div key={title}>
            <div className="mb-4 grid size-13 place-items-center rounded-2xl bg-background text-primary shadow-float">
              <Icon className="size-6.5" strokeWidth={1.5} />
            </div>
            <h3 className="mb-1.5 text-[17px] font-bold">{title}</h3>
            <p className="text-[15px] text-muted-foreground">{text}</p>
          </div>
        ))}
      </div>
    </section>
  );
}
