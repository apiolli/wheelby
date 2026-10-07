import * as React from "react"
import { Input as InputPrimitive } from "@base-ui/react/input"
import { cn } from "cn"

function Input({ className, type, ...props }: React.ComponentProps<"input">) {
  return (
    <InputPrimitive
      type={type}
      data-slot="input"
      className={cn(
        "flex h-12 w-full min-w-0 rounded-lg border border-input bg-background px-3.5 text-[15px] text-foreground transition-[border-color,box-shadow] outline-none placeholder:text-muted-foreground",
        "hover:border-foreground focus-visible:border-foreground focus-visible:ring-1 focus-visible:ring-foreground",
        "aria-invalid:border-destructive aria-invalid:bg-destructive-soft aria-invalid:focus-visible:ring-destructive",
        "disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50",
        className
      )}
      {...props}
    />
  )
}

export { Input }
