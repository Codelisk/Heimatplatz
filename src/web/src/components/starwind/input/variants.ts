import { tv } from "tailwind-variants";

export const input = tv({
  base: [
    // Opak statt transparent: die Bretterfugen des Tafel-Hintergrunds duerfen
    // nicht durch Eingabefelder scheinen (Light wie Dark, 1:1-Struktur)
    "border-input text-foreground w-full rounded-md border bg-muted shadow-xs",
    "focus-visible:border-outline focus-visible:ring-outline/50 transition-[color,box-shadow] focus-visible:ring-3",
    "file:text-foreground file:my-auto file:mr-4 file:h-full file:border-0 file:bg-transparent file:text-sm file:font-medium",
    "disabled:cursor-not-allowed disabled:opacity-50",
    "aria-invalid:border-error aria-invalid:focus-visible:ring-error/40",
    "peer placeholder:text-muted-foreground",
  ],
  variants: {
    size: { sm: "h-9 px-2 text-sm", md: "h-11 px-3 text-base", lg: "h-12 px-4 text-lg" },
  },
  defaultVariants: { size: "md" },
});
