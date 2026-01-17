import React from "react";
import { cn } from "../../utils/cn";

type ButtonVariant = "primary" | "secondary" | "ghost";
type ButtonSize = "sm" | "md" | "lg";

type Props = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant;
  size?: ButtonSize;
  isLoading?: boolean;
};

export default function Button({
  className,
  variant = "primary",
  size = "md",
  isLoading,
  disabled,
  children,
  ...props
}: Props) {
  const base =
    "inline-flex items-center justify-center rounded-full transition font-semibold select-none " +
    "focus:outline-none focus:ring-0 border-0";

  const variants: Record<ButtonVariant, string> = {
    primary: "bg-primary text-white hover:opacity-90 disabled:opacity-60",
    secondary:
      "bg-[#F3F1ED] text-text hover:bg-[#ebe7e1] disabled:opacity-60",
    ghost: "bg-transparent text-text hover:bg-black/5 disabled:opacity-60",
  };

  const sizes: Record<ButtonSize, string> = {
    sm: "h-9 px-4 text-sm",
    md: "h-10 px-5 text-[15px]",
    lg: "h-12 px-6 text-[16px]",
  };

  return (
    <button
      className={cn(base, variants[variant], sizes[size], className)}
      disabled={disabled || isLoading}
      {...props}
    >
      {isLoading ? "..." : children}
    </button>
  );
}
