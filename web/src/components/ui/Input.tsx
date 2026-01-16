import React from "react";
import { cn } from "../../utils/cn";

type Props = React.InputHTMLAttributes<HTMLInputElement> & {
  label?: string;
};

export default function Input({ className, label, ...props }: Props) {
  return (
    <label className="block w-full">
      {label && (
        <span className="block mb-2 text-right text-sm text-muted">{label}</span>
      )}
      <input
        className={cn(
          "w-full h-[50px] rounded-full bg-[#F3F1ED] px-5 text-right",
          "border-0 outline-none focus:outline-none focus:ring-0",
          "placeholder:text-muted/70",
          className
        )}
        {...props}
      />
    </label>
  );
}
