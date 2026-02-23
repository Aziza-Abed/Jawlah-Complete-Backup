import { useEffect, useState } from "react";
import { Navigate } from "react-router-dom";
import { apiClient } from "../api/client";
import { STORAGE_KEYS } from "../constants/storageKeys";

interface AdminRouteProps {
  children: React.ReactNode;
  allowedRoles?: string[]; // Default: ['admin'] only
}

/**
 * AdminRoute - Protects routes that require Admin (or specific) role
 *
 * Usage:
 *   <AdminRoute>...</AdminRoute>  // Admin only
 *   <AdminRoute allowedRoles={['admin', 'supervisor']}>...</AdminRoute>  // Admin or Supervisor
 */
export default function AdminRoute({
  children,
  allowedRoles = ['admin']
}: AdminRouteProps) {
  const [isValidating, setIsValidating] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isAuthorized, setIsAuthorized] = useState(false);
  const token = localStorage.getItem(STORAGE_KEYS.TOKEN);

  useEffect(() => {
    const validateAccess = async () => {
      // No token = not authenticated
      if (!token) {
        setIsAuthenticated(false);
        setIsAuthorized(false);
        setIsValidating(false);
        return;
      }

      try {
        // Validate token with server and get user data
        const response = await apiClient.get('/auth/me');

        if (response.data.success && response.data.data) {
          const userData = response.data.data;
          const role = userData.role?.toLowerCase() || '';

          setIsAuthenticated(true);

          // Check if user's role is in allowedRoles
          const hasAccess = allowedRoles.map(r => r.toLowerCase()).includes(role);
          setIsAuthorized(hasAccess);

          // Update user data in localStorage
          localStorage.setItem(STORAGE_KEYS.USER, JSON.stringify(userData));
        } else {
          // Token invalid
          localStorage.removeItem(STORAGE_KEYS.TOKEN);
          localStorage.removeItem(STORAGE_KEYS.USER);
          setIsAuthenticated(false);
          setIsAuthorized(false);
        }
      } catch (error) {
        // Token validation failed
        localStorage.removeItem(STORAGE_KEYS.TOKEN);
        localStorage.removeItem(STORAGE_KEYS.USER);
        setIsAuthenticated(false);
        setIsAuthorized(false);
      } finally {
        setIsValidating(false);
      }
    };

    validateAccess();
  }, [token, allowedRoles]);

  // Show loading while validating
  if (isValidating) {
    return (
      <div className="h-screen w-screen flex items-center justify-center bg-gradient-to-br from-gray-900 to-gray-800">
        <div className="text-white text-lg">جاري التحقق من الصلاحيات...</div>
      </div>
    );
  }

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // Show unauthorized message if authenticated but wrong role
  if (!isAuthorized) {
    return (
      <div className="h-screen w-screen flex items-center justify-center bg-gradient-to-br from-gray-900 to-gray-800">
        <div className="bg-white/10 backdrop-blur-sm rounded-2xl p-8 max-w-md text-center">
          <div className="text-6xl mb-4">🚫</div>
          <h1 className="text-2xl font-bold text-white mb-2">غير مصرح</h1>
          <p className="text-gray-300 mb-6">
            ليس لديك صلاحية للوصول إلى هذه الصفحة.
            <br />
            <span className="text-sm text-gray-400">
              هذه الصفحة متاحة فقط لـ: {allowedRoles.map(r =>
                r === 'admin' ? 'المدير' : r === 'supervisor' ? 'المشرف' : r
              ).join('، ')}
            </span>
          </p>
          <div className="flex gap-3 justify-center">
            <button
              onClick={() => window.history.back()}
              className="px-4 py-2 bg-gray-600 hover:bg-gray-500 text-white rounded-lg transition-colors"
            >
              العودة
            </button>
            <button
              onClick={() => window.location.href = '/dashboard'}
              className="px-4 py-2 bg-teal-600 hover:bg-teal-500 text-white rounded-lg transition-colors"
            >
              لوحة التحكم
            </button>
          </div>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
