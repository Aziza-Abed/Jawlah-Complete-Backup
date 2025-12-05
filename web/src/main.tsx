import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter, Routes, Route } from "react-router-dom";

import App from "./App";
import "./index.css";

import Login from "./pages/Login";
import Dashboard from "./pages/Dashboard";
import Tasks from "./pages/Tasks";
import Zones from "./pages/Zones";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <BrowserRouter>
      <Routes>
        {/* صفحة اللوجين بدون الـ layout */}
        <Route path="/login" element={<Login />} />

        {/* باقي الصفحات جوّا الـ layout */}
        <Route path="/" element={<App />}>
          {/* index route: لما نروح على "/" يفتح Dashboard */}
          <Route index element={<Dashboard />} />
          <Route path="dashboard" element={<Dashboard />} />
          <Route path="tasks" element={<Tasks />} />
          <Route path="zones" element={<Zones />} />
        </Route>
      </Routes>
    </BrowserRouter>
  </React.StrictMode>
);
