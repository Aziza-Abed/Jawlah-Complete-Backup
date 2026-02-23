import { useNavigate } from "react-router-dom";
import logo from "../../assets/logo.png";
import { Menu, Bell, User, Settings } from "lucide-react";
import { useNotifications } from "../../contexts/NotificationContext";

export default function Topbar({ onMenuClick }: { onMenuClick?: () => void }) {
  const navigate = useNavigate();
  const { unreadCount } = useNotifications();



  return (
    <header className="w-full bg-[#7895B2] px-6 sm:px-8 md:px-10 h-[85px] sm:h-[105px] shadow-lg relative z-30">
      <div className="h-full flex items-center justify-between">
        
        {/* Right Side: Logo and Title (The Start in RTL) */}
        <div className="flex items-center gap-4 lg:gap-6">
          <div className="relative group">
            <div className="absolute -inset-1 bg-white/20 rounded-full blur opacity-0 group-hover:opacity-100 transition duration-500"></div>
            <img
              src={logo}
              alt="FollowUp"
              className="relative w-[50px] h-[50px] sm:w-[60px] sm:h-[60px] object-contain drop-shadow-md"
            />
          </div>
          <div className="text-right">
            <h1 className="text-white font-black text-[24px] sm:text-[32px] tracking-tight leading-tight">FollowUp</h1>
            <p className="text-white/80 text-[13px] sm:text-[16px] font-bold tracking-wide">نظام متابعة العمل الميداني</p>
          </div>
        </div>

        {/* Left Side: Premium Icon Pill (The End in RTL) */}
        <div className="flex items-center gap-4">
          {/* Desktop Icon Pill */}
          <div className="hidden md:flex items-center bg-white/95 backdrop-blur-md rounded-xl px-2 py-1.5 shadow-xl border border-white/50">
            {/* Notifications */}
            <button
              type="button"
              onClick={() => navigate("/notifications")}
              className="w-[36px] h-[36px] rounded-lg grid place-items-center hover:bg-gray-100 transition-all text-[#6B7280] relative hover:scale-105 active:scale-95"
              title="الإشعارات"
            >
              <Bell size={18} />
              {unreadCount > 0 && (
                <span className="absolute top-0 right-0 min-w-[17px] h-[17px] bg-[#C86E5D] text-white text-[9px] font-black rounded-full flex items-center justify-center px-1 shadow-sm border border-white">
                  {unreadCount > 99 ? "99+" : unreadCount}
                </span>
              )}
            </button>

            {/* Profile */}
            <button
              type="button"
              onClick={() => navigate("/profile")}
              className="w-[36px] h-[36px] rounded-lg grid place-items-center hover:bg-gray-100 transition-all text-[#6B7280] hover:scale-105 active:scale-95"
              title="الملف الشخصي"
            >
              <User size={18} />
            </button>

            {/* Settings */}
            <button
              type="button"
              onClick={() => navigate("/settings")}
              className="w-[36px] h-[36px] rounded-lg grid place-items-center hover:bg-gray-100 transition-all text-[#6B7280] hover:scale-105 active:scale-95"
              title="الإعدادات"
            >
              <Settings size={18} />
            </button>
          </div>

          {/* Mobile Menu Button */}
          <button
            type="button"
            onClick={onMenuClick}
            className="md:hidden w-[40px] h-[40px] rounded-xl bg-white/10 grid place-items-center hover:bg-white/20 transition-all active:scale-90"
            aria-label="القائمة"
          >
            <Menu size={20} className="text-white" />
          </button>
        </div>
      </div>
    </header>
  );
}
