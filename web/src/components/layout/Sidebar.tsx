import React from "react";
import { NavLink } from "react-router-dom";
import { LogOut } from "lucide-react";
import { type UserRole, getNavItems } from "./navItems";

export default function Sidebar({ role = "supervisor", onLogout }: { role?: UserRole; onLogout?: () => void }) {
  const items = getNavItems(role);

  return (
    <aside className="h-full flex flex-col bg-gradient-to-b from-[#7895B2] to-[#647e99] w-full shadow-[-10px_0_30px_rgba(0,0,0,0.02)] border-l border-white/5">
      <div className="h-[20px] sm:h-[30px]" />

      <nav className="sidebar-nav px-3 py-4 flex flex-col gap-2 overflow-y-auto flex-1">
        {items.map((it) => (
          <SideItem
            key={it.to}
            to={it.to}
            icon={it.icon}
            end={it.end}
          >
            {it.label}
          </SideItem>
        ))}

      </nav>

      {/* Logout (always at bottom - desktop sidebar) */}
      <div className="px-3 pb-4 hidden md:block">
        <button
          type="button"
          onClick={onLogout}
          className="w-full rounded-[12px] px-4 py-2.5 font-sans font-semibold text-[14px] md:text-[15px] transition-colors duration-200 flex items-center gap-3 text-white hover:bg-white/15"
          aria-label="تسجيل الخروج"
        >
          <LogOut size={18} className="shrink-0" />
          <span className="flex-1 text-right">تسجيل الخروج</span>
        </button>
      </div>

    </aside>
  );
}

function SideItem({
  to,
  icon: Icon,
  end,
  children,
}: {
  to: string;
  icon?: React.ElementType;
  end?: boolean;
  children: React.ReactNode;
}) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) =>
        [
          "w-full rounded-[14px] px-5 py-3.5",
          "font-sans font-bold text-[15px] md:text-[16px]",
          "transition-colors duration-200",
          "flex items-center gap-3",
          isActive
            ? "bg-[#F3F1ED] text-[#2F2F2F] shadow-md active:scale-[0.98]"
            : "text-white hover:bg-white/10",
        ].join(" ")
      }
    >
      {Icon && <Icon size={18} className="shrink-0" />}
      <span className="flex-1 text-right">{children}</span>
    </NavLink>
  );
}
