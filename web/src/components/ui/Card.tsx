import React from "react";
import { cn } from "../../utils/cn";

type Props = React.HTMLAttributes<HTMLDivElement> & {
  title?: string;
};

export default function Card({ className, title, children, ...props }: Props) {
  return (
    <div
      className={cn(
        "bg-surface rounded-[12px] shadow-sm",
        "border border-black/5",
        className
      )}
      {...props}
    >
      {title && (
        <div className="px-6 pt-6 text-right text-xl font-semibold text-text">
          {title}
        </div>
      )}
      <div className={cn("px-6 pb-6", title ? "pt-4" : "pt-6")}>
        {children}
      </div>
    </div>
  );
}
