import { Routes, Route, Navigate } from "react-router-dom";
import AppLayout from "../components/layout/AppLayout";

import Login from "../pages/Login";
import Dashboard from "../pages/Dashboard";
import Reports from "../pages/Reports";
import Zones from "../pages/Zones";
import CreateTask from "../pages/CreateTask";
import Notifications from "../pages/Notifications";
import TaskDetails from "../pages/TaskDetails";
import IssueDetails from "../pages/IssueDetails"
import TasksList from "../pages/Tasks";;
import ProtectedRoute from "./ProtectedRoute";
import Issues from "../pages/Issues";
import ForgotPassword from "../pages/ForgotPassword";
import ResetPassword from "../pages/ResetPassword";

export default function AppRoutes() {
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
        <Route path="dashboard" element={<Dashboard />} />

        {/* Task Creation */}
        <Route path="tasks/new" element={<CreateTask />} />

        {/* Reports */}
        <Route path="reports" element={<Reports />} />

        {/* Zones */}
        <Route path="zones" element={<Zones />} />

        <Route path="tasks/:id" element={<TaskDetails />} />
        <Route path="tasks" element={<TasksList />} />

        <Route path="issues/:id" element={<IssueDetails />} />
        <Route path="issues" element={<Issues />} />
        {/* Notifications */}
        <Route path="notifications" element={<Notifications />} />

      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
