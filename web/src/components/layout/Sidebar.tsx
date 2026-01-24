import React from "react";
import { NavLink, useNavigate } from "react-router-dom";
import {
  LayoutDashboard,
  ClipboardList,
  AlertCircle,
  PlusCircle,
  Map,
  BarChart3,
  Users,
  LogOut,
} from "lucide-react";

type UserRole = "manager" | "supervisor";

type NavItem = {
  to: string;
  label: string;
  icon: React.ElementType;
  end?: boolean;
};

const supervisorItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", icon: LayoutDashboard, end: true },
  { to: "/tasks", label: "المهام", icon: ClipboardList, end: true },
  { to: "/issues", label: "البلاغات", icon: AlertCircle },
  { to: "/tasks/new", label: "تعيين مهمة جديدة", icon: PlusCircle },
  { to: "/zones", label: "الخريطة الحية", icon: Map },
  { to: "/reports", label: "التقارير", icon: BarChart3 },
];

const managerItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", icon: LayoutDashboard, end: true },
  { to: "/reports", label: "التقارير", icon: BarChart3 },
  { to: "/accounts", label: "إدارة الحسابات", icon: Users },
];

export default function Sidebar({ role = "supervisor" }: { role?: UserRole }) {
  const navigate = useNavigate();
  const items = role === "manager" ? managerItems : supervisorItems;

  const handleLogout = () => {
    // TODO backend: call logout endpoint when available
    localStorage.removeItem("jawlah_token");
    localStorage.removeItem("jawlah_user");
    navigate("/login");
  };

  return (
    <aside className="h-full flex flex-col bg-[#7895B2] w-full shadow-[-10px_0_30px_rgba(0,0,0,0.02)] border-l border-white/5">
      <div className="h-[76px]" />

      <nav className="px-3 py-4 flex flex-col gap-2">
        {items.map((it) => (
          <SideItem key={it.to} to={it.to} icon={it.icon} end={it.end}>
            {it.label}
          </SideItem>
        ))}
      </nav>

      <div className="flex-1" />

      {/* Logout (always at bottom - desktop sidebar) */}
      <div className="px-3 pb-4">
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
  icon: React.ElementType;
  end?: boolean;
  children: React.ReactNode;
}) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) =>
        [
          "w-full rounded-[12px] px-4 py-2.5",
          "font-sans font-semibold text-[14px] md:text-[15px]",
          "transition-colors duration-200",
          "flex items-center gap-3", // icon then label
          isActive ? "bg-[#F3F1ED] text-[#2F2F2F]" : "text-white hover:bg-white/15",
        ].join(" ")
      }
    >
      <Icon size={18} className="shrink-0" />
      <span className="flex-1 text-right">{children}</span>
    </NavLink>
  );
}
