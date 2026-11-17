import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { FilterOption } from "../filters";

interface FilterSelectProps<T> {
  label: string;
  value: string;
  options: FilterOption<T>[];
  onChange: (slug: string) => void;
}

// Filtro en píldora; el valor es el slug que se guarda en la URL.
export default function FilterSelect<T>({
  label,
  value,
  options,
  onChange,
}: FilterSelectProps<T>) {
  return (
    <Select
      value={value}
      items={options.map((o) => ({ value: o.slug, label: o.label }))}
      onValueChange={(v) => onChange(String(v))}
    >
      <SelectTrigger
        aria-label={label}
        className="min-w-0 flex-1 bg-background sm:flex-none [&>span]:truncate"
      >
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        {options.map((o) => (
          <SelectItem key={o.slug} value={o.slug}>
            {o.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
