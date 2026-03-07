import { useEffect, useState, useRef } from "react";
import { Navigate } from "react-router-dom";
import { apiClient } from "../api/client";
import { STORAGE_KEYS } from "../constants/storageKeys";

export default function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const token = localStorage.getItem(STORAGE_KEYS.TOKEN);
  const hasToken = !!token;

  // If token exists, assume authenticated until proven otherwise (no flash redirect)
  const [isValidating, setIsValidating] = useState(hasToken);
  const [isAuthenticated, setIsAuthenticated] = useState(hasToken);
  const didValidate = useRef(false);

  useEffect(() => {
    // No token = not authenticated, no need to validate
    if (!token) {
      setIsAuthenticated(false);
      setIsValidating(false);
      return;
    }

    // Only validate once per mount
    if (didValidate.current) return;
    didValidate.current = true;

    const validateToken = async () => {
      try {
        const response = await apiClient.get('/auth/me');

        if (response.data.success) {
          setIsAuthenticated(true);
          localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(response.data.data));
        } else {
          localStorage.removeItem(STORAGE_KEYS.TOKEN);
          localStorage.removeItem(STORAGE_KEYS.USER);
          setIsAuthenticated(false);
        }
      } catch {
        localStorage.removeItem(STORAGE_KEYS.TOKEN);
        localStorage.removeItem(STORAGE_KEYS.USER);
        setIsAuthenticated(false);
      } finally {
        setIsValidating(false);
      }
    };

    validateToken();
  }, [token]);

  // No token at all → redirect immediately
  if (!hasToken) {
    return <Navigate to="/login" replace />;
  }

  // Token exists, validating in background → show loading
  if (isValidating) {
    return (
      <div className="h-screen w-screen flex items-center justify-center bg-[#F3F1ED]">
        <div className="text-[#2F2F2F] text-lg font-semibold">جاري التحقق من الجلسة...</div>
      </div>
    );
  }

  // Validation done, not authenticated → redirect
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
