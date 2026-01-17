import React, { useMemo, useState } from "react";
import { NavLink, useLocation } from "react-router-dom";
import logo from "../../assets/logo.png";
import { Bell, User, Settings, Search, Menu } from "lucide-react";

export default function Topbar({ onMenuClick }: { onMenuClick?: () => void }) {
  const location = useLocation();
  const [query, setQuery] = useState("");

  const unreadCount = 3;

  const isNotificationsActive = useMemo(() => {
    return location.pathname.startsWith("/notifications");
  }, [location.pathname]);

  const onSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // TODO: later: navigate(`/search?q=${encodeURIComponent(query)}`)
  };

  return (
    <header className="w-full bg-[#7895B2] px-3 sm:px-6 md:px-8 h-[76px] sm:h-[90px] md:h-[100px]">
      {/* ✅ RTL: first child appears on the RIGHT automatically */}
      <div className="h-full flex items-center justify-between gap-3">
        {/* ✅ RIGHT (Logo) */}
        <div className="flex items-center justify-end w-[90px] sm:w-[120px] md:w-[140px]">
          <img
            src={logo}
            alt="بلدية البيرة"
            className="w-[56px] h-[56px] sm:w-[68px] sm:h-[68px] md:w-[77px] md:h-[76px] object-contain"
          />
        </div>

        {/* ✅ CENTER (Search) */}
        <div className="flex-1 flex justify-center">
          {/* Desktop search */}
          <form
            onSubmit={onSearchSubmit}
            className="hidden sm:flex w-full max-w-[590px] h-[46px] sm:h-[50px] bg-[#F3F1ED] rounded-full items-center px-4 sm:px-5 gap-3"
          >
            <Search size={20} className="text-[#60778E]" />
            <input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="بحث..."
              className="w-full bg-transparent border-0 outline-none focus:outline-none focus:ring-0 text-right text-[14px] sm:text-[16px] placeholder:text-[#60778E]/70"
            />
          </form>

          {/* Mobile search icon only */}
          <button
            type="button"
            className="sm:hidden w-[44px] h-[44px] rounded-full bg-[#F3F1ED] grid place-items-center hover:opacity-95 transition"
            aria-label="بحث"
          >
            <Search size={20} className="text-[#60778E]" />
          </button>
        </div>

       {/* ✅ LEFT (Actions) */}
<div className="flex items-center justify-start gap-2 w-[170px] sm:w-[200px]">
  {/* Mobile actions: Menu + Notifications فقط */}
  <div className="md:hidden h-[46px] bg-[#F3F1ED] rounded-full px-2 flex items-center gap-2">
    <button
      type="button"
      onClick={onMenuClick}
      className="w-[44px] h-[44px] rounded-full grid place-items-center hover:bg-white/70 transition"
      aria-label="القائمة"
    >
      <Menu size={22} className="text-[#60778E]" />
    </button>

    <div className="w-[1px] h-6 bg-[#60778E]/20" />

    <NavLink
      to="/notifications"
      className={({ isActive }) =>
        [
          "relative",
          "w-[44px] h-[44px] rounded-full grid place-items-center",
          "transition",
          isActive || isNotificationsActive ? "bg-white/70" : "hover:bg-white/70",
        ].join(" ")
      }
      aria-label="الإشعارات"
    >
      <Bell size={22} className="text-[#60778E]" />
      {unreadCount > 0 && (
        <span className="absolute -top-1 -left-1 min-w-[18px] h-[18px] px-1 rounded-full bg-[#C86E5D] text-white text-[11px] font-sans font-bold flex items-center justify-center">
          {unreadCount > 99 ? "99+" : unreadCount}
        </span>
      )}
    </NavLink>
  </div>

  {/* Desktop actions: Notifications + Profile + Settings */}
  <div className="hidden md:flex h-[46px] sm:h-[50px] bg-[#F3F1ED] rounded-full px-2 items-center gap-2">
    <NavLink
      to="/notifications"
      className={({ isActive }) =>
        [
          "relative",
          "w-[44px] h-[44px] rounded-full grid place-items-center",
          "transition",
          isActive || isNotificationsActive ? "bg-white/70" : "hover:bg-white/70",
        ].join(" ")
      }
      aria-label="الإشعارات"
    >
      <Bell size={22} className="text-[#60778E]" />
      {unreadCount > 0 && (
        <span className="absolute -top-1 -left-1 min-w-[18px] h-[18px] px-1 rounded-full bg-[#C86E5D] text-white text-[11px] font-sans font-bold flex items-center justify-center">
          {unreadCount > 99 ? "99+" : unreadCount}
        </span>
      )}
    </NavLink>

    <div className="w-[1px] h-6 bg-[#60778E]/20" />

    <PillIcon ariaLabel="الملف الشخصي">
      <User size={22} className="text-[#60778E]" />
    </PillIcon>

    <div className="w-[1px] h-6 bg-[#60778E]/20" />

    <PillIcon ariaLabel="الإعدادات">
      <Settings size={22} className="text-[#60778E]" />
    </PillIcon>
  </div>
</div>
      </div>
    </header>
  );
}

function PillIcon({
  children,
  onClick,
  ariaLabel,
}: {
  children: React.ReactNode;
  onClick?: () => void;
  ariaLabel?: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-label={ariaLabel}
      className="w-[44px] h-[44px] rounded-full grid place-items-center bg-transparent border-0 outline-none hover:bg-white/70 transition"
    >
      {children}
    </button>
  );
}
