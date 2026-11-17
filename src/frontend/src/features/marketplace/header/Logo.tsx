import { Link } from "react-router";
import { cn } from "cn";

import logoUrl from "@/assets/svg/wheelby-logo-principal.svg";
import symbolUrl from "@/assets/svg/wheelby-simbolo.svg";

type LogoVariant = "full" | "symbol" | "responsive";

interface LogoProps {
  // full: símbolo + wordmark · symbol: solo la rueda · responsive: símbolo por debajo de lg
  variant?: LogoVariant;
  to?: string;
  className?: string;
}

export default function Logo({
  variant = "full",
  to = "/",
  className,
}: LogoProps) {
  const showFull = variant !== "symbol";
  const showSymbol = variant !== "full";

  return (
    <Link
      to={to}
      aria-label="Wheelby, inicio"
      className={cn(
        "inline-flex shrink-0 items-center rounded-lg focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none",
        className,
      )}
    >
      {showFull && (
        <img
          src={logoUrl}
          alt=""
          width={162}
          height={40}
          className={cn(
            "h-10 w-auto",
            variant === "responsive" && "hidden lg:block",
          )}
        />
      )}
      {showSymbol && (
        <img
          src={symbolUrl}
          alt=""
          width={36}
          height={36}
          className={cn("size-9", variant === "responsive" && "lg:hidden")}
        />
      )}
    </Link>
  );
}
