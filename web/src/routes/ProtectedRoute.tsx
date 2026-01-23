import { useEffect, useState } from "react";
import { Navigate } from "react-router-dom";
import { apiClient } from "../api/client";

export default function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const [isValidating, setIsValidating] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const token = localStorage.getItem("followup_token");

  useEffect(() => {
    const validateToken = async () => {
      // No token = not authenticated
      if (!token) {
        setIsAuthenticated(false);
        setIsValidating(false);
        return;
      }

      try {
        // SECURITY FIX: Validate token with server
        // This prevents fake tokens from localStorage bypass
        const response = await apiClient.get('/auth/me');

        if (response.data.success) {
          setIsAuthenticated(true);
          // Update user data in localStorage from server
          localStorage.setItem("followup_user", JSON.stringify(response.data.data));
        } else {
          // Token invalid
          localStorage.removeItem("followup_token");
          localStorage.removeItem("followup_user");
          setIsAuthenticated(false);
        }
      } catch (error) {
        // Token validation failed
        localStorage.removeItem("followup_token");
        localStorage.removeItem("followup_user");
        setIsAuthenticated(false);
      } finally {
        setIsValidating(false);
      }
    };

    validateToken();
  }, [token]);

  // Show loading while validating
  if (isValidating) {
    return (
      <div className="h-screen w-screen flex items-center justify-center bg-gradient-to-br from-gray-900 to-gray-800">
        <div className="text-white text-lg">جاري التحقق من الجلسة...</div>
      </div>
    );
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
