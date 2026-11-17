import * as React from "react";
import { cn } from "cn";

function Textarea({ className, ...props }: React.ComponentProps<"textarea">) {
  return (
    <textarea
      data-slot="textarea"
      className={cn(
        "flex min-h-24 w-full rounded-lg border border-input bg-background px-3.5 py-3 text-[15px] text-foreground transition-[border-color,box-shadow] outline-none placeholder:text-muted-foreground",
        "hover:border-foreground focus-visible:border-foreground focus-visible:ring-1 focus-visible:ring-foreground",
        "aria-invalid:border-destructive aria-invalid:focus-visible:ring-destructive",
        "disabled:cursor-not-allowed disabled:opacity-50",
        className,
      )}
      {...props}
    />
  );
}

export { Textarea };
