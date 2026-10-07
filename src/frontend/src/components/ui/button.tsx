import { Button as ButtonPrimitive } from "@base-ui/react/button"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "cn"

// Botones en píldora (999px), como pide la identidad visual.
const buttonVariants = cva(
  "group/button inline-flex shrink-0 items-center justify-center gap-2 rounded-full text-sm font-semibold whitespace-nowrap transition-[color,background-color,box-shadow,transform] outline-none select-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground hover:bg-primary-hover active:scale-[0.99]",
        outline: "border border-foreground bg-background hover:bg-accent",
        secondary: "bg-secondary text-secondary-foreground hover:bg-border-soft",
        ghost: "hover:bg-accent hover:text-accent-foreground",
        link: "rounded-none p-0 text-primary underline-offset-4 hover:underline",
        // Acciones destructivas: relleno en la confirmación final, borde en el disparador.
        destructive: "bg-destructive text-primary-foreground hover:bg-destructive/90 active:scale-[0.99]",
        "destructive-outline": "border border-destructive bg-background text-destructive hover:bg-destructive-soft",
      },
      size: {
        default: "h-10 px-4",
        xs: "h-7 px-2.5 text-xs [&_svg:not([class*='size-'])]:size-3",
        sm: "h-8 px-3 text-[13px]",
        lg: "h-12 px-6 text-[15px]",
        icon: "size-10",
        "icon-xs": "size-7 [&_svg:not([class*='size-'])]:size-3",
        "icon-sm": "size-8",
        "icon-lg": "size-12",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

function Button({
  className,
  variant = "default",
  size = "default",
  ...props
}: ButtonPrimitive.Props & VariantProps<typeof buttonVariants>) {
  return (
    <ButtonPrimitive
      data-slot="button"
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}

export { Button, buttonVariants }
