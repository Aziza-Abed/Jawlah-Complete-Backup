import React from "react";
import { cn } from "../../utils/cn";

export default function Spinner({ className }: { className?: string }) {
  return (
    <div
      className={cn(
        "w-5 h-5 rounded-full border-2 border-black/20 border-t-black/60 animate-spin",
        className
      )}
      aria-label="loading"
    />
  );
}
