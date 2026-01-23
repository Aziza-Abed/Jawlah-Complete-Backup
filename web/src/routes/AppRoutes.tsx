import { Routes, Route, Navigate } from "react-router-dom";
import AppLayout from "../components/layout/AppLayout";

import Login from "../pages/Login";
import Dashboard from "../pages/Dashboard";
import Reports from "../pages/Reports";
import Zones from "../pages/Zones";
import CreateTask from "../pages/CreateTask";
import Notifications from "../pages/Notifications";
import TaskDetails from "../pages/TaskDetails";
import IssueDetails from "../pages/IssueDetails";
import TasksList from "../pages/Tasks";
import ProtectedRoute from "./ProtectedRoute";
import AdminRoute from "./AdminRoute";
import Issues from "../pages/Issues";
import ForgotPassword from "../pages/ForgotPassword";
import ResetPassword from "../pages/ResetPassword";
import Profile from "../pages/Profile";
import Settings from "../pages/Settings";
import AdminSystemSettings from "../pages/AdminSystemSettings";
import AdminZones from "../pages/AdminZones";
import AdminTasks from "../pages/AdminTasks";
import AdminMonitoring from "../pages/AdminMonitoring";
import AdminAuditLogs from "../pages/AdminAuditLogs";
import AdminAccounts from "../pages/AdminAccounts";
import AdminDashboard from "../pages/AdminDashboard";
import AdminSupervisors from "../pages/AdminSupervisors";
import AdminIssues from "../pages/AdminIssues";
import AdminAppeals from "../pages/AdminAppeals";

export default function AppRoutes() {
  const getUserRole = (): string | null => {
    try {
      const userStr = localStorage.getItem("followup_user");
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
    <Routes>
      {/* Public */}
<Route path="/login" element={<Login />} />
  <Route path="/forgot-password" element={<ForgotPassword />} />
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

        {/* Notifications */}
        <Route path="notifications" element={<Notifications />} />

        {/* Profile & Settings */}
        <Route path="profile" element={<Profile />} />
        <Route path="settings" element={<Settings />} />

        {/* Admin only - Protected by role */}
        <Route path="accounts" element={<AdminRoute><AdminAccounts /></AdminRoute>} />
        <Route path="system-settings" element={<AdminRoute><AdminSystemSettings /></AdminRoute>} />
        <Route path="zones-admin" element={<AdminRoute><AdminZones /></AdminRoute>} />
        <Route path="zones_admin" element={<AdminRoute><AdminZones /></AdminRoute>} />
        <Route path="task-oversight" element={<AdminRoute><AdminTasks /></AdminRoute>} />
        <Route path="monitoring" element={<AdminRoute><AdminMonitoring /></AdminRoute>} />
        <Route path="supervisors" element={<AdminRoute><AdminSupervisors /></AdminRoute>} />
        <Route path="audit-logs" element={<AdminRoute><AdminAuditLogs /></AdminRoute>} />
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
