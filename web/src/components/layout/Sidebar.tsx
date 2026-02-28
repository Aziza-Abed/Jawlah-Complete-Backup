import React from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { STORAGE_KEYS } from "../../constants/storageKeys";
import { logout } from "../../api/auth";
import {
  User,
  Settings as SettingsIcon,
  LogOut,
} from "lucide-react";
import { useNotifications } from "../../contexts/NotificationContext";
import { type UserRole, getNavItems } from "./navItems";

export default function Sidebar({ role = "supervisor" }: { role?: UserRole }) {
  const navigate = useNavigate();
  const { unreadCount } = useNotifications();
  const items = getNavItems(role);

  const handleLogout = async () => {
    try {
      await logout();
    } catch (_) {
      // even if backend fails, clear local data
    }
    localStorage.removeItem(STORAGE_KEYS.TOKEN);
    localStorage.removeItem(STORAGE_KEYS.REFRESH_TOKEN);
    localStorage.removeItem(STORAGE_KEYS.USER);
    navigate("/login");
  };

  return (
    <aside className="h-full flex flex-col bg-gradient-to-b from-[#7895B2] to-[#647e99] w-full shadow-[-10px_0_30px_rgba(0,0,0,0.02)] border-l border-white/5">
      <div className="h-[20px] sm:h-[30px]" />

      <nav className="px-3 py-4 flex flex-col gap-2 overflow-y-auto flex-1">
        {items.map((it) => (
          <SideItem
            key={it.to}
            to={it.to}
            icon={it.icon}
            end={it.end}
            badge={it.to === "/notifications" ? unreadCount : undefined}
          >
            {it.label}
          </SideItem>
        ))}

        {/* Desktop-only secondary links */}
        <div className="hidden md:block mt-auto pt-4">
          <div className="h-[1px] bg-white/10 mb-4 mx-2" />
          <div className="flex flex-col gap-2">
            <SideItem to="/profile" icon={User}>الملف الشخصي</SideItem>
            <SideItem to="/settings" icon={SettingsIcon}>الإعدادات</SideItem>
          </div>
        </div>
      </nav>

      {/* Logout (always at bottom - desktop sidebar) */}
      <div className="px-3 pb-4 hidden md:block">
        <button
          type="button"
          onClick={handleLogout}
          className="w-full rounded-[12px] px-4 py-2.5 font-sans font-semibold text-[14px] md:text-[15px] transition-colors duration-200 flex items-center gap-3 text-white hover:bg-white/15"
          aria-label="تسجيل الخروج"
        >
          <LogOut size={18} className="shrink-0" />
          <span className="flex-1 text-right">تسجيل الخروج</span>
        </button>
      </div>

      {/* Mobile-only: Profile + Settings inside Drawer */}
      <div className="md:hidden px-3 pb-6 mt-auto">
        <div className="h-[1px] bg-white/10 mb-4" />
        <div className="flex flex-col gap-2">
          <SideItem to="/profile" icon={User}>الملف الشخصي</SideItem>
          <SideItem to="/settings" icon={SettingsIcon}>الإعدادات</SideItem>
        </div>
      </div>
    </aside>
  );
}

function SideItem({
  to,
  icon: Icon,
  end,
  badge,
  children,
}: {
  to: string;
  icon?: React.ElementType;
  end?: boolean;
  badge?: number;
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
      {badge != null && badge > 0 && (
        <span className="min-w-[20px] h-[20px] bg-[#C86E5D] text-white text-[11px] font-black rounded-full flex items-center justify-center px-1.5 shadow-sm">
          {badge > 99 ? "99+" : badge}
        </span>
      )}
    </NavLink>
  );
}
