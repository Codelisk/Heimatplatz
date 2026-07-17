import { tv } from "tailwind-variants";

export const textarea = tv({
  base: [
    // Opak statt transparent: die Bretterfugen des Tafel-Hintergrunds duerfen
    // nicht durch Eingabefelder scheinen (Light wie Dark, 1:1-Struktur)
    "border-input text-foreground ring-offset-background min-h-10 w-full rounded-md border bg-muted shadow-xs",
    "focus-visible:border-outline focus-visible:ring-outline/50 transition-[color,box-shadow] focus-visible:ring-3",
    "file:text-foreground file:border-0 file:bg-transparent file:text-sm file:font-medium",
    "disabled:cursor-not-allowed disabled:opacity-50",
    "aria-invalid:border-error aria-invalid:focus-visible:ring-error/40",
    "peer placeholder:text-muted-foreground",
  ],
  variants: {
    size: {
      sm: "min-h-9 px-2 py-1 text-sm",
      md: "min-h-10 px-3 py-2 text-base",
      lg: "min-h-12 px-4 py-3 text-lg",
    },
  },
  defaultVariants: { size: "md" },
});
