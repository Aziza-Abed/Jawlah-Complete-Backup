import {
  LayoutDashboard,
  ClipboardList,
  AlertCircle,
  PlusCircle,
  Map,
  BarChart3,
  Users,
  UserCog,
  Building,
  ShieldCheck,
  Scale,
  Bell,
  FileText,
} from "lucide-react";

export type UserRole = "admin" | "manager" | "supervisor";

export type NavItem = {
  to: string;
  label: string;
  icon: React.ElementType;
  end?: boolean;
};

export const supervisorItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", icon: LayoutDashboard, end: true },
  { to: "/my-workers", label: "العمال التابعين لي", icon: Users },
  { to: "/tasks", label: "المهام", icon: ClipboardList, end: true },
  { to: "/issues", label: "البلاغات", icon: AlertCircle },
  { to: "/tasks/new", label: "تعيين مهمة جديدة", icon: PlusCircle },
  { to: "/zones", label: "الخريطة الحية", icon: Map },
  { to: "/reports", label: "التقارير", icon: BarChart3 },
  { to: "/notifications", label: "الإشعارات", icon: Bell },
];

export const adminItems: NavItem[] = [
  { to: "/dashboard", label: "لوحة التحكم", icon: LayoutDashboard, end: true },
  { to: "/accounts", label: "إدارة الحسابات", icon: Users },
  { to: "/supervisors", label: "إدارة المشرفين", icon: UserCog },
  { to: "/departments", label: "إدارة الأقسام", icon: Building },
  { to: "/teams", label: "إدارة الفرق", icon: Users },
  { to: "/zones-admin", label: "إدارة المناطق", icon: Map },
  { to: "/task-oversight", label: "الرقابة على المهام", icon: ShieldCheck },
  { to: "/task-templates", label: "قوالب المهام", icon: FileText },
  { to: "/appeals", label: "مركز المراجعة", icon: Scale },
  { to: "/reports", label: "التقارير", icon: BarChart3 },
];

export function getNavItems(role: UserRole): NavItem[] {
  return (role === "admin" || role === "manager") ? adminItems : supervisorItems;
}
