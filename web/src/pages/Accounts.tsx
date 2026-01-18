import React, { useEffect, useState } from "react";
import { getUsers, resetDeviceId, resetUserPassword, updateUserStatus } from "../api/users";
import type { UserResponse } from "../types/user";
import {
  Users,
  Search,
  MoreVertical,
  Smartphone,
  KeyRound,
  Shield,
  UserCheck,
  UserX,
  Loader2,
  CheckCircle,
  AlertCircle
} from "lucide-react";

export default function Accounts() {
  const [users, setUsers] = useState<UserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [roleFilter, setRoleFilter] = useState("all");
  const [actionLoading, setActionLoading] = useState<number | null>(null);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  // Modal for password reset
  const [resetModalUser, setResetModalUser] = useState<UserResponse | null>(null);
  const [newPassword, setNewPassword] = useState("");

  useEffect(() => {
    fetchUsers();
  }, []);

  const fetchUsers = async () => {
    try {
      const data = await getUsers(1, 100);
      setUsers(data.items);
    } catch (err) {
      console.error("Failed to fetch users", err);
      setMessage({ type: "error", text: "فشل تحميل قائمة المستخدمين" });
    } finally {
      setLoading(false);
    }
  };

  const handleResetDevice = async (userId: number) => {
    if (!window.confirm("هل أنت متأكد من رغبتك في إعادة تعيين جهاز هذا المستخدم؟")) return;

    setActionLoading(userId);
    try {
      await resetDeviceId(userId);
      setMessage({ type: "success", text: "تم فك ارتباط الجهاز بنجاح" });
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      setMessage({ type: "error", text: "فشل إعادة تعيين الجهاز" });
    } finally {
      setActionLoading(null);
    }
  };

  const handleStatusChange = async (userId: number, currentStatus: string) => {
    const newStatus = currentStatus === "Active" ? "Inactive" : "Active";
    const actionText = newStatus === "Active" ? "تفعيل" : "تعطيل";

    if (!window.confirm(`هل أنت متأكد من ${actionText} هذا المستخدم؟`)) return;

    setActionLoading(userId);
    try {
      await updateUserStatus(userId, newStatus);
      setUsers(users.map(u => u.userId === userId ? { ...u, status: newStatus as any } : u));
      setMessage({ type: "success", text: `تم ${actionText} المستخدم بنجاح` });
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      setMessage({ type: "error", text: `فشل ${actionText} المستخدم` });
    } finally {
      setActionLoading(null);
    }
  };

  const handlePasswordReset = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resetModalUser) return;

    setActionLoading(resetModalUser.userId);
    try {
      await resetUserPassword(resetModalUser.userId, newPassword);
      setMessage({ type: "success", text: "تم إعادة تعيين كلمة المرور بنجاح" });
      setResetModalUser(null);
      setNewPassword("");
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      setMessage({ type: "error", text: "فشل إعادة تعيين كلمة المرور" });
    } finally {
      setActionLoading(null);
    }
  };

  const filteredUsers = users.filter(user => {
    const matchesSearch = user.fullName.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         user.username.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesRole = roleFilter === "all" || user.role === roleFilter;
    return matchesSearch && matchesRole;
  });

  if (loading) {
    return (
      <div className="h-full w-full flex items-center justify-center bg-[#D9D9D9]">
        <div className="text-[#2F2F2F] flex items-center gap-2">
          <Loader2 className="animate-spin" /> جاري التحميل...
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto p-4 sm:p-6 md:p-8">
      <div className="max-w-[1200px] mx-auto">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
          <h1 className="text-right font-sans font-semibold text-[24px] text-[#2F2F2F]">
            إدارة الحسابات
          </h1>
          
          <div className="flex flex-col sm:flex-row gap-3">
            <select
              value={roleFilter}
              onChange={(e) => setRoleFilter(e.target.value)}
              className="h-10 rounded-[10px] border border-black/10 px-3 text-right bg-white outline-none focus:ring-2 focus:ring-[#7895B2]/50 font-sans"
            >
              <option value="all">جميع الأدوار</option>
              <option value="Admin">مدير نظام</option>
              <option value="Supervisor">مشرف</option>
              <option value="Worker">عامل</option>
            </select>

            <div className="relative">
              <input
                type="text"
                placeholder="بحث عن مستخدم..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="h-10 w-full sm:w-[250px] rounded-[10px] border border-black/10 px-3 pr-9 text-right bg-white outline-none focus:ring-2 focus:ring-[#7895B2]/50 font-sans"
              />
              <Search className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400" size={18} />
            </div>
          </div>
        </div>

        {message && (
          <div className={`mb-4 p-3 rounded-[10px] flex items-center justify-end gap-2 text-right ${
            message.type === "success" ? "bg-green-100 text-green-700" : "bg-red-100 text-red-700"
          }`}>
            <span>{message.text}</span>
            {message.type === "success" ? <CheckCircle size={18} /> : <AlertCircle size={18} />}
          </div>
        )}

        <div className="bg-white rounded-[16px] border border-[#E5E7EB] shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-right">
              <thead className="bg-[#F9FAFB] border-b border-[#E5E7EB]">
                <tr>
                  <th className="px-6 py-4 font-semibold text-[#6B7280] text-sm">الإجراءات</th>
                  <th className="px-6 py-4 font-semibold text-[#6B7280] text-sm">الحالة</th>
                  <th className="px-6 py-4 font-semibold text-[#6B7280] text-sm">الدور</th>
                  <th className="px-6 py-4 font-semibold text-[#6B7280] text-sm">المستخدم</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[#E5E7EB]">
                {filteredUsers.map((user) => (
                  <tr key={user.userId} className="hover:bg-[#F9FAFB] transition-colors">
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-2">
                        {/* Reset Password Button */}
                        <button
                          onClick={() => setResetModalUser(user)}
                          className="p-2 text-gray-500 hover:text-[#7895B2] hover:bg-gray-100 rounded-lg transition-colors"
                          title="تغيير كلمة المرور"
                        >
                          <KeyRound size={18} />
                        </button>

                        {/* Reset Device Button */}
                        <button
                          onClick={() => handleResetDevice(user.userId)}
                          disabled={actionLoading === user.userId}
                          className="p-2 text-gray-500 hover:text-orange-500 hover:bg-orange-50 rounded-lg transition-colors disabled:opacity-50"
                          title="فك ارتباط الجهاز"
                        >
                          <Smartphone size={18} />
                        </button>

                        {/* Toggle Status Button */}
                        <button
                          onClick={() => handleStatusChange(user.userId, user.status)}
                          disabled={actionLoading === user.userId}
                          className={`p-2 rounded-lg transition-colors disabled:opacity-50 ${
                            user.status === "Active" 
                              ? "text-gray-500 hover:text-red-500 hover:bg-red-50" 
                              : "text-gray-500 hover:text-green-500 hover:bg-green-50"
                          }`}
                          title={user.status === "Active" ? "تعطيل الحساب" : "تفعيل الحساب"}
                        >
                          {user.status === "Active" ? <UserX size={18} /> : <UserCheck size={18} />}
                        </button>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                        user.status === "Active" 
                          ? "bg-green-100 text-green-800"
                          : "bg-red-100 text-red-800"
                      }`}>
                        {user.status === "Active" ? "نشط" : "معطل"}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-sm text-[#374151]">
                      {user.role}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex flex-col items-end">
                        <span className="font-medium text-[#111827]">{user.fullName}</span>
                        <span className="text-sm text-[#6B7280]">{user.username}</span>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {filteredUsers.length === 0 && (
            <div className="p-8 text-center text-[#6B7280]">
              لا يوجد مستخدمين مطابقين للبحث
            </div>
          )}
        </div>
      </div>

      {/* Password Reset Modal */}
      {resetModalUser && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50 backdrop-blur-sm">
          <div className="bg-white rounded-[20px] p-6 w-full max-w-md shadow-2xl">
            <h3 className="text-xl font-bold text-right mb-4">إعادة تعيين كلمة المرور</h3>
            <p className="text-right text-gray-600 mb-6">
              سيتم تعيين كلمة مرور جديدة للمستخدم <span className="font-bold">{resetModalUser.fullName}</span>
            </p>
            
            <form onSubmit={handlePasswordReset}>
              <div className="mb-6">
                <label className="block text-right text-sm font-semibold text-[#6B7280] mb-2">كلمة المرور الجديدة</label>
                <input
                  type="text"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="w-full h-10 rounded-[10px] border border-[#E5E7EB] px-3 text-right outline-none focus:ring-2 focus:ring-[#7895B2]/50 font-sans"
                  placeholder="أدخل كلمة المرور الجديدة"
                  required
                  minLength={6}
                />
              </div>

              <div className="flex gap-3 flex-row-reverse">
                <button
                  type="button"
                  onClick={() => setResetModalUser(null)}
                  className="flex-1 h-10 rounded-[10px] border border-[#E5E7EB] text-[#374151] hover:bg-gray-50 transition-colors font-medium"
                >
                  إلغاء
                </button>
                <button
                  type="submit"
                  disabled={!!actionLoading}
                  className="flex-1 h-10 rounded-[10px] bg-[#2F2F2F] text-white hover:bg-black transition-colors font-medium disabled:opacity-50"
                >
                  {actionLoading ? "جاري الحفظ..." : "حفظ"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
