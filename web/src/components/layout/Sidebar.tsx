import React from "react";
import { NavLink } from "react-router-dom";

type UserRole = "manager" | "supervisor";

type NavItem = {
  to: string;
  label: string;
  end?: boolean;
};

const supervisorItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", end: true },
  { to: "/tasks/new", label: "تعيين مهمة جديدة" },
  { to: "/zones", label: "الخريطة الحية" },
  { to: "/reports", label: "التقارير" },
];

const managerItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", end: true },
  { to: "/reports", label: "التقارير" },
  { to: "/accounts", label: "إدارة الحسابات" },
];

export default function Sidebar({ role = "supervisor" }: { role?: UserRole }) {
  // TODO: Replace with role from backend/auth token or /me endpoint
  const items = role === "manager" ? managerItems : supervisorItems;

  return (
    <aside className="h-full shrink-0 w-52 sm:w-56 md:w-[250px] flex flex-col bg-[#7895B2]">
      <div className="h-24" />

      {/* Added pb to keep last item away from bottom */}
      <nav className="px-2.5 pt-14 pb-10 flex flex-col gap-10">
        {items.map((it) => (
          <SideItem key={it.to} to={it.to} end={it.end}>
            {it.label}
          </SideItem>
        ))}
      </nav>

      <div className="flex-1" />
    </aside>
  );
}

function SideItem({
  to,
  end,
  children,
}: {
  to: string;
  end?: boolean;
  children: React.ReactNode;
}) {
  return (
    <NavLink
      to={to}
      end={end}
      className={({ isActive }) =>
        [
          "block w-full",
          "select-none font-sans font-semibold",
          "text-[18px] sm:text-[20px] md:text-[24px]",
          "py-4",
          isActive
            ? [
                // Active: card attached to the content side (left in RTL layout)
                "bg-[#F3F1ED] text-[#2F2F2F]",
                "rounded-tr-[10px] rounded-br-[10px]",
                "px-4 text-center",
                "mr-3",
              ].join(" ")
            : "text-[#A7ACB1] px-3 text-right",
        ].join(" ")
      }
    >
      {children}
    </NavLink>
  );
}
