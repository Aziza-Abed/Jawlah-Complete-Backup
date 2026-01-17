import { Routes, Route, Navigate } from "react-router-dom";
import AppLayout from "../components/layout/AppLayout";

import Login from "../pages/Login";
import Dashboard from "../pages/Dashboard";
import Reports from "../pages/Reports";
import Zones from "../pages/Zones";
import CreateTask from "../pages/CreateTask";

import ProtectedRoute from "./ProtectedRoute";

export default function AppRoutes() {
  return (
    <Routes>
      {/* Public */}
      <Route path="/login" element={<Login />} />

      {/* Protected */}
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />

        <Route path="dashboard" element={<Dashboard />} />

        {/* Task Creation */}
        <Route path="tasks/new" element={<CreateTask />} />

        {/* Reports */}
        <Route path="reports" element={<Reports />} />

        {/* Zones */}
        <Route path="zones" element={<Zones />} />
      </Route>

      {/* Fallback */}
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
