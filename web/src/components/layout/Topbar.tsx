import React, { useMemo } from "react";
import { NavLink, useLocation, useNavigate } from "react-router-dom";
import logo from "../../assets/logo.png";
import { Bell, User, Settings, Menu, LogOut } from "lucide-react";
import { logout } from "../../api/auth";
import { useMunicipality } from "../../contexts/MunicipalityContext";

export default function Topbar({ onMenuClick }: { onMenuClick?: () => void }) {
  const location = useLocation();
  const navigate = useNavigate();
  const { municipalityName } = useMunicipality();

  const unreadCount = 3;

  const handleLogout = async () => {
    try {
      await logout();
    } catch (err) {
      console.error("Logout failed:", err);
    } finally {
      // Clear local storage
      localStorage.removeItem("followup_token");
      localStorage.removeItem("followup_user");
      // Navigate to login
      navigate("/login");
    }
  };

  const isNotificationsActive = useMemo(() => {
    return location.pathname.startsWith("/notifications");
  }, [location.pathname]);



  return (
    <header className="w-full bg-primary px-4 h-[76px] sm:h-[90px] md:h-[100px] shadow-md relative z-30">
      {/* ✅ RTL: first child appears on the RIGHT automatically */}
      <div className="h-full flex items-center justify-between gap-3">
        {/* ✅ RIGHT (Logo & Branding) */}
        <div className="flex items-center justify-start gap-4 flex-shrink-0">
          <img
            src={logo}
            alt={municipalityName}
            className="w-[48px] h-[48px] sm:w-[56px] sm:h-[56px] md:w-[64px] md:h-[64px] object-contain drop-shadow-md"
          />
          <div className="flex flex-col items-start border-r-2 border-white/20 pr-4 leading-none">
            <span className="text-2xl md:text-3xl font-black text-white tracking-tight">
              FollowUp
            </span>
            <span className="text-[10px] md:text-xs text-background-paper font-bold mt-1 uppercase tracking-widest opacity-90">
              نظام متابعة العمل الميداني
            </span>
          </div>
        </div>

        {/* ✅ CENTER (Empty) */}
        <div className="flex-1" />

        {/* ✅ LEFT (Actions) */}
        <div className="flex items-center justify-end gap-2 shrink-0">
  {/* Mobile actions: Menu + Notifications only */}
  <div className="md:hidden h-[46px] bg-white/10 backdrop-blur-md rounded-full px-2 flex items-center gap-2 border border-white/10">
    <button
      type="button"
      onClick={onMenuClick}
      className="w-[44px] h-[44px] rounded-full grid place-items-center hover:bg-white/20 transition"
      aria-label="القائمة"
    >
      <Menu size={22} className="text-white" />
    </button>

    <div className="w-[1px] h-6 bg-white/20" />

    <NavLink
      to="/notifications"
      className={({ isActive }) =>
        [
          "relative",
          "w-[44px] h-[44px] rounded-full grid place-items-center",
          "transition",
          isActive || isNotificationsActive ? "bg-white/30" : "hover:bg-white/20",
        ].join(" ")
      }
      aria-label="الإشعارات"
    >
      <Bell size={22} className="text-white" />
      {unreadCount > 0 && (
        <span className="absolute -top-1 -right-1 min-w-[18px] h-[18px] px-1 rounded-full bg-accent text-white text-[11px] font-sans font-bold flex items-center justify-center border border-white/20 shadow-lg">
          {unreadCount > 99 ? "99+" : unreadCount}
        </span>
      )}
    </NavLink>
  </div>

  {/* Desktop actions: Notifications + Profile + Settings */}
  <div className="hidden md:flex h-[46px] sm:h-[50px] bg-white/10 backdrop-blur-md rounded-full px-2 items-center gap-2 border border-white/10 shadow-lg">
    <NavLink
      to="/notifications"
      className={({ isActive }) =>
        [
          "relative",
          "w-[44px] h-[44px] rounded-full grid place-items-center",
          "transition",
          isActive || isNotificationsActive ? "bg-white/30" : "hover:bg-white/20",
        ].join(" ")
      }
      aria-label="الإشعارات"
    >
      <Bell size={22} className="text-white" />
      {unreadCount > 0 && (
        <span className="absolute -top-1 -right-1 min-w-[18px] h-[18px] px-1 rounded-full bg-accent text-white text-[11px] font-sans font-bold flex items-center justify-center shadow-lg shadow-accent/20 border border-white/20">
          {unreadCount > 99 ? "99+" : unreadCount}
        </span>
      )}
    </NavLink>

    <div className="w-[1px] h-6 bg-white/20" />

    <PillIcon ariaLabel="الملف الشخصي" onClick={() => navigate("/profile")}>
      <User size={22} className="text-white" />
    </PillIcon>

    <div className="w-[1px] h-6 bg-white/20" />

    <PillIcon ariaLabel="الإعدادات" onClick={() => navigate("/settings")}>
      <Settings size={22} className="text-white" />
    </PillIcon>

    <div className="w-[1px] h-6 bg-white/20" />

    <PillIcon ariaLabel="تسجيل الخروج" onClick={handleLogout}>
      <LogOut size={22} className="text-accent" />
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
      className="w-[44px] h-[44px] rounded-full grid place-items-center bg-transparent border-0 outline-none hover:bg-white/50 transition"
    >
      {children}
    </button>
  );
}
