import * as React from "react";
import { Popover as PopoverPrimitive } from "@base-ui/react/popover";
import { cn } from "cn";

function Popover({ ...props }: PopoverPrimitive.Root.Props) {
  return <PopoverPrimitive.Root data-slot="popover" {...props} />;
}

function PopoverTrigger({ ...props }: PopoverPrimitive.Trigger.Props) {
  return <PopoverPrimitive.Trigger data-slot="popover-trigger" {...props} />;
}

function PopoverContent({
  className,
  sideOffset = 8,
  align = "start",
  children,
  ...props
}: PopoverPrimitive.Popup.Props & {
  sideOffset?: number;
  align?: "start" | "center" | "end";
}) {
  return (
    <PopoverPrimitive.Portal>
      <PopoverPrimitive.Positioner
        sideOffset={sideOffset}
        align={align}
        className="z-50"
      >
        <PopoverPrimitive.Popup
          data-slot="popover-content"
          className={cn(
            "rounded-3xl border border-border-soft bg-popover p-4 text-popover-foreground shadow-menu outline-none",
            "transition-[opacity,transform] duration-150 data-ending-style:scale-95 data-ending-style:opacity-0 data-starting-style:scale-95 data-starting-style:opacity-0",
            className,
          )}
          {...props}
        >
          {children}
        </PopoverPrimitive.Popup>
      </PopoverPrimitive.Positioner>
    </PopoverPrimitive.Portal>
  );
}

export { Popover, PopoverContent, PopoverTrigger };
export type PopoverProps = React.ComponentProps<typeof Popover>;
