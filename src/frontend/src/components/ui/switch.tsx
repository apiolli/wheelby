import { Switch as SwitchPrimitive } from "@base-ui/react/switch";
import { cn } from "cn";

// Interruptor de encendido. El thumb se desliza con transform (CSS), sin Motion.
function Switch({ className, ...props }: SwitchPrimitive.Root.Props) {
  return (
    <SwitchPrimitive.Root
      data-slot="switch"
      className={cn(
        "relative inline-flex h-6 w-10.5 shrink-0 cursor-pointer items-center rounded-full bg-input transition-colors outline-none",
        "focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2",
        "data-checked:bg-primary data-disabled:cursor-not-allowed data-disabled:opacity-50",
        className,
      )}
      {...props}
    >
      <SwitchPrimitive.Thumb className="block size-5 translate-x-0.5 rounded-full bg-background shadow-xs transition-transform data-checked:translate-x-5" />
    </SwitchPrimitive.Root>
  );
}

export { Switch };
