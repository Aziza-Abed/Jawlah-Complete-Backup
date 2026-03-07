import { useMemo } from "react";
import { Navigate, useNavigate } from "react-router-dom";
import { STORAGE_KEYS } from "../constants/storageKeys";

interface AdminRouteProps {
  children: React.ReactNode;
  allowedRoles?: string[]; // Default: ['admin'] only
}

// Protects routes that require Admin (or specific) role
// Uses cached user data from localStorage (already validated by ProtectedRoute)
// This avoids a redundant /auth/me API call since ProtectedRoute already validated the token
export default function AdminRoute({
  children,
  allowedRoles = ['admin']
}: AdminRouteProps) {
  const navigate = useNavigate();
  const token = localStorage.getItem(STORAGE_KEYS.TOKEN);

  const { isAuthenticated, isAuthorized } = useMemo(() => {
    if (!token) {
      return { isAuthenticated: false, isAuthorized: false, userRole: '' };
    }

    try {
      const userStr = localStorage.getItem(STORAGE_KEYS.USER);
      if (!userStr) {
        return { isAuthenticated: false, isAuthorized: false, userRole: '' };
      }

      const userData = JSON.parse(userStr);
      const role = userData.role?.toLowerCase() || '';
      const hasAccess = allowedRoles.map(r => r.toLowerCase()).includes(role);

      return { isAuthenticated: true, isAuthorized: hasAccess, userRole: role };
    } catch {
      return { isAuthenticated: false, isAuthorized: false, userRole: '' };
    }
  }, [token, allowedRoles.join(',')]);

  // Redirect to login if not authenticated
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // Show unauthorized message if authenticated but wrong role
  if (!isAuthorized) {
    return (
      <div className="h-full w-full flex items-center justify-center bg-[#F3F1ED]">
        <div className="bg-white rounded-[20px] p-8 max-w-md text-center shadow-[0_4px_25px_rgba(0,0,0,0.06)] border border-black/5">
          <div className="text-6xl mb-4">🚫</div>
          <h1 className="text-2xl font-bold text-[#2F2F2F] mb-2">غير مصرح</h1>
          <p className="text-[#6B7280] mb-6">
            ليس لديك صلاحية للوصول إلى هذه الصفحة.
            <br />
            <span className="text-sm text-[#6B7280]">
              هذه الصفحة متاحة فقط لـ: {allowedRoles.map(r =>
                r === 'admin' ? 'المدير' : r === 'supervisor' ? 'المشرف' : r
              ).join('، ')}
            </span>
          </p>
          <div className="flex gap-3 justify-center">
            <button
              onClick={() => navigate(-1)}
              className="px-4 py-2 bg-[#F3F1ED] hover:bg-[#E8E6E2] text-[#2F2F2F] rounded-[12px] font-semibold transition-colors"
            >
              العودة
            </button>
            <button
              onClick={() => navigate('/dashboard')}
              className="px-4 py-2 bg-[#7895B2] hover:bg-[#6A849F] text-white rounded-[12px] font-semibold transition-colors"
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
