import { cn } from "cn";
import type { ComponentProps } from "react";

export const TextLink = ({ className, ...props }: ComponentProps<"button">) => {
  return (
    <button
      type="button"
      className={cn(
        "cursor-pointer font-bold text-primary hover:underline focus-visible:underline focus-visible:outline-none disabled:cursor-default disabled:opacity-60",
        className,
      )}
      {...props}
    />
  );
};
