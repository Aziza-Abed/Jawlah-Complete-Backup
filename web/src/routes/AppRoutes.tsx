import { lazy, Suspense } from "react";
import { Routes, Route, Navigate } from "react-router-dom";
import AppLayout from "../components/layout/AppLayout";
import ProtectedRoute from "./ProtectedRoute";
import AdminRoute from "./AdminRoute";
import { STORAGE_KEYS } from "../constants/storageKeys";

const Login = lazy(() => import("../pages/Login"));
const ForgotPassword = lazy(() => import("../pages/ForgotPassword"));
const VerifyOtp = lazy(() => import("../pages/VerifyOtp"));
const ResetPassword = lazy(() => import("../pages/ResetPassword"));
const SupervisorDashboard = lazy(() => import("../pages/SupervisorDashboard"));
const Reports = lazy(() => import("../pages/Reports"));
const Zones = lazy(() => import("../pages/Zones"));
const CreateTask = lazy(() => import("../pages/CreateTask"));
const Notifications = lazy(() => import("../pages/Notifications"));
const TaskDetails = lazy(() => import("../pages/TaskDetails"));
const IssueDetails = lazy(() => import("../pages/IssueDetails"));
const TasksList = lazy(() => import("../pages/Tasks"));
const SupervisorIssues = lazy(() => import("../pages/SupervisorIssues"));
const Profile = lazy(() => import("../pages/Profile"));
const Settings = lazy(() => import("../pages/Settings"));
const AdminZones = lazy(() => import("../pages/AdminZones"));
const AdminDepartments = lazy(() => import("../pages/AdminDepartments"));
const AdminTeams = lazy(() => import("../pages/AdminTeams"));
const AdminTasks = lazy(() => import("../pages/AdminTasks"));
const AdminAuditLogs = lazy(() => import("../pages/AdminAuditLogs"));
const AdminAccounts = lazy(() => import("../pages/AdminAccounts"));
const AdminDashboard = lazy(() => import("../pages/AdminDashboard"));
const AdminSupervisors = lazy(() => import("../pages/AdminSupervisors"));
const AdminIssues = lazy(() => import("../pages/AdminIssues"));
const SupervisorAppeals = lazy(() => import("../pages/SupervisorAppeals"));
const TaskTemplates = lazy(() => import("../pages/TaskTemplates"));
const MyWorkers = lazy(() => import("../pages/MyWorkers"));
const LocationHistory = lazy(() => import("../pages/LocationHistory"));
const NotFound = lazy(() => import("../pages/NotFound"));

function useUserRole(): string | null {
  try {
    const userStr = localStorage.getItem(STORAGE_KEYS.USER);
    if (userStr) {
      const user = JSON.parse(userStr);
      return user.role?.toLowerCase() || null;
    }
  } catch (err) {}
  return null;
}

// Wrapper components that read role at render time (not at mount time)
function DashboardSwitch() {
  const role = useUserRole();
  return role === "admin" ? <AdminDashboard /> : <SupervisorDashboard />;
}

function IssuesSwitch() {
  const role = useUserRole();
  return role === "admin" ? <AdminIssues /> : <SupervisorIssues />;
}

export default function AppRoutes() {

  return (
    <Suspense fallback={<div className="h-screen w-screen flex items-center justify-center bg-[#F3F1ED]" />}>
    <Routes>
      {/* Public */}
      <Route path="/login" element={<Login />} />
      <Route path="/forgot-password" element={<ForgotPassword />} />
      <Route path="/verify-otp" element={<VerifyOtp />} />
      <Route path="/reset-password" element={<ResetPassword />} />

      {/* Protected */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        {/* Default */}
        <Route index element={<Navigate to="/dashboard" replace />} />

        {/* Dashboard */}
        <Route path="dashboard" element={<DashboardSwitch />} />

        {/* Task Creation - Admin/Supervisor only */}
        <Route path="tasks/new" element={<AdminRoute allowedRoles={['admin', 'supervisor']}><CreateTask /></AdminRoute>} />

        {/* Reports - Admin/Supervisor only */}
        <Route path="reports" element={<AdminRoute allowedRoles={['admin', 'supervisor']}><Reports /></AdminRoute>} />

        {/* Zones (Live Map) - Admin/Supervisor only */}
        <Route path="zones" element={<AdminRoute allowedRoles={['admin', 'supervisor']}><Zones /></AdminRoute>} />

        <Route path="tasks/:id" element={<TaskDetails />} />
        <Route path="tasks" element={<TasksList />} />

        <Route path="issues/:id" element={<IssueDetails />} />
        <Route path="issues" element={<IssuesSwitch />} />

        {/* Appeals - supervisor only (workers appeal to their supervisor) */}
        <Route path="appeals" element={<AdminRoute allowedRoles={['supervisor']}><SupervisorAppeals /></AdminRoute>} />

        {/* Supervisor: My Workers and Location History */}
        <Route path="my-workers" element={<AdminRoute allowedRoles={['admin', 'supervisor']}><MyWorkers /></AdminRoute>} />
        <Route path="location-history/:workerId" element={<AdminRoute allowedRoles={['admin', 'supervisor']}><LocationHistory /></AdminRoute>} />

        {/* Notifications */}
        <Route path="notifications" element={<Notifications />} />

        {/* Profile & Settings */}
        <Route path="profile" element={<Profile />} />
        <Route path="settings" element={<Settings />} />

        {/* Admin only - Protected by role */}
        <Route path="accounts" element={<AdminRoute><AdminAccounts /></AdminRoute>} />
        <Route path="departments" element={<AdminRoute><AdminDepartments /></AdminRoute>} />
        <Route path="teams" element={<AdminRoute><AdminTeams /></AdminRoute>} />
        <Route path="zones-admin" element={<AdminRoute><AdminZones /></AdminRoute>} />
        <Route path="task-oversight" element={<AdminRoute><AdminTasks /></AdminRoute>} />
        <Route path="task-templates" element={<AdminRoute allowedRoles={['admin', 'supervisor']}><TaskTemplates /></AdminRoute>} />
        <Route path="supervisors" element={<AdminRoute><AdminSupervisors /></AdminRoute>} />
        <Route path="audit-logs" element={<AdminRoute><AdminAuditLogs /></AdminRoute>} />
      </Route>

      {/* 404 */}
      <Route path="*" element={<NotFound />} />
    </Routes>
    </Suspense>
  );
}
