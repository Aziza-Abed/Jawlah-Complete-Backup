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
const Dashboard = lazy(() => import("../pages/Dashboard"));
const Reports = lazy(() => import("../pages/Reports"));
const Zones = lazy(() => import("../pages/Zones"));
const CreateTask = lazy(() => import("../pages/CreateTask"));
const Notifications = lazy(() => import("../pages/Notifications"));
const TaskDetails = lazy(() => import("../pages/TaskDetails"));
const IssueDetails = lazy(() => import("../pages/IssueDetails"));
const TasksList = lazy(() => import("../pages/Tasks"));
const Issues = lazy(() => import("../pages/Issues"));
const Profile = lazy(() => import("../pages/Profile"));
const Settings = lazy(() => import("../pages/Settings"));
const AdminZones = lazy(() => import("../pages/AdminZones"));
const AdminDepartments = lazy(() => import("../pages/AdminDepartments"));
const AdminTeams = lazy(() => import("../pages/AdminTeams"));
const AdminTasks = lazy(() => import("../pages/AdminTasks"));
const AdminMonitoring = lazy(() => import("../pages/AdminMonitoring"));
const AdminAuditLogs = lazy(() => import("../pages/AdminAuditLogs"));
const AdminAccounts = lazy(() => import("../pages/AdminAccounts"));
const AdminDashboard = lazy(() => import("../pages/AdminDashboard"));
const AdminSupervisors = lazy(() => import("../pages/AdminSupervisors"));
const AdminIssues = lazy(() => import("../pages/AdminIssues"));
const AdminAppeals = lazy(() => import("../pages/AdminAppeals"));
const AdminTaskTemplates = lazy(() => import("../pages/AdminTaskTemplates"));
const MyWorkers = lazy(() => import("../pages/MyWorkers"));
const LocationHistory = lazy(() => import("../pages/LocationHistory"));
const NotFound = lazy(() => import("../pages/NotFound"));

export default function AppRoutes() {
  const getUserRole = (): string | null => {
    try {
      const userStr = localStorage.getItem(STORAGE_KEYS.USER);
      if (userStr) {
        const user = JSON.parse(userStr);
        return user.role?.toLowerCase() || null;
      }
    } catch (err) {}
    // FIX: Return null instead of "supervisor" to prevent privilege escalation
    // Unauthenticated users should not get supervisor privileges
    return null;
  };

  const role = getUserRole();

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
        <Route path="dashboard" element={(role === "admin" || role === "manager") ? <AdminDashboard /> : <Dashboard />} />

        {/* Task Creation */}
        <Route path="tasks/new" element={<CreateTask />} />

        {/* Reports */}
        <Route path="reports" element={<Reports />} />

        {/* Zones */}
        <Route path="zones" element={<Zones />} />

        <Route path="tasks/:id" element={<TaskDetails />} />
        <Route path="tasks" element={<TasksList />} />

        <Route path="issues/:id" element={<IssueDetails />} />
        <Route path="issues" element={role === "admin" ? <AdminIssues /> : <Issues />} />

        {/* Appeals - for supervisors and admin */}
        <Route path="appeals" element={<AdminAppeals />} />

        {/* Supervisor: My Workers and Location History */}
        <Route path="my-workers" element={<MyWorkers />} />
        <Route path="location-history/:workerId" element={<LocationHistory />} />

        {/* Notifications */}
        <Route path="notifications" element={<Notifications />} />

        {/* Profile & Settings */}
        <Route path="profile" element={<Profile />} />
        <Route path="settings" element={<Settings />} />

        {/* Admin only - Protected by role */}
        <Route path="accounts" element={<AdminRoute><AdminAccounts /></AdminRoute>} />
        <Route path="departments" element={<AdminRoute><AdminDepartments /></AdminRoute>} />
        <Route path="teams" element={<AdminRoute allowedRoles={['admin', 'supervisor']}><AdminTeams /></AdminRoute>} />
        <Route path="zones-admin" element={<AdminRoute><AdminZones /></AdminRoute>} />
        <Route path="task-oversight" element={<AdminRoute><AdminTasks /></AdminRoute>} />
        <Route path="task-templates" element={<AdminRoute><AdminTaskTemplates /></AdminRoute>} />
        <Route path="monitoring" element={<AdminRoute><AdminMonitoring /></AdminRoute>} />
        <Route path="supervisors" element={<AdminRoute><AdminSupervisors /></AdminRoute>} />
        <Route path="audit-logs" element={<AdminRoute><AdminAuditLogs /></AdminRoute>} />
      </Route>

      {/* 404 */}
      <Route path="*" element={<NotFound />} />
    </Routes>
    </Suspense>
  );
}
