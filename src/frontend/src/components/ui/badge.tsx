import { mergeProps } from "@base-ui/react/merge-props"
import { useRender } from "@base-ui/react/use-render"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "cn"

const badgeVariants = cva(
  "inline-flex w-fit shrink-0 items-center justify-center gap-1 overflow-hidden rounded-full px-2.5 py-1 text-[11.5px] leading-tight font-bold whitespace-nowrap [&>svg]:pointer-events-none [&>svg]:size-3",
  {
    variants: {
      variant: {
        default: "bg-background/95 text-foreground shadow-xs",
        highlight: "bg-highlight-soft text-highlight-foreground",
        primary: "bg-primary text-primary-foreground",
        outline: "border border-border text-foreground",
        // Estados (reservas, cuentas, solicitudes): fondo suave y texto del mismo tono.
        soft: "bg-primary-soft text-primary",
        success: "bg-success-soft text-success",
        destructive: "bg-destructive-soft text-destructive",
        muted: "bg-secondary text-muted-foreground",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

function Badge({
  className,
  variant = "default",
  render,
  ...props
}: useRender.ComponentProps<"span"> & VariantProps<typeof badgeVariants>) {
  return useRender({
    defaultTagName: "span",
    props: mergeProps<"span">(
      {
        className: cn(badgeVariants({ variant }), className),
      },
      props
    ),
    render,
    state: {
      slot: "badge",
      variant,
    },
  })
}

export { Badge, badgeVariants }
