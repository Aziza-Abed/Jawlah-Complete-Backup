import { useEffect, useState } from "react";
import { getUsers, resetDeviceId, resetUserPassword, updateUserStatus, assignUserZones, updateUser, getUserZones } from "../api/users";
import { register } from "../api/auth";
import { getMunicipalities } from "../api/municipality";
import { getZones } from "../api/zones";
import { getDepartments, type Department } from "../api/departments";
import type { Municipality } from "../api/municipality";
import type { ZoneResponse } from "../types/zone";
import type { UserResponse, UserRole, UserStatus, WorkerType } from "../types/user";
import {
  Users,
  Search,
  Smartphone,
  KeyRound,
  UserCheck,
  UserX,
  Loader2,
  CheckCircle,
  AlertCircle,
  Plus,
  X,
  User as UserIcon,
  Briefcase,
  Edit2
} from "lucide-react";
import { useConfirm } from "../components/common/ConfirmDialog";

export default function AdminAccounts() {
  const [confirm, ConfirmDialog] = useConfirm();
  const [users, setUsers] = useState<UserResponse[]>([]);
  const [supervisors, setSupervisors] = useState<UserResponse[]>([]);
  const [municipalities, setMunicipalities] = useState<Municipality[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [zones, setZones] = useState<ZoneResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [roleFilter, setRoleFilter] = useState("all");
  const [actionLoading, setActionLoading] = useState<number | string | null>(null);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  // Modals
  const [resetModalUser, setResetModalUser] = useState<UserResponse | null>(null);
  const [newPassword, setNewPassword] = useState("");
  const [showUserModal, setShowUserModal] = useState(false);
  const [isEditMode, setIsEditMode] = useState(false);
  const [editUserId, setEditUserId] = useState<number | null>(null);

  // User Form State
  const [formData, setFormData] = useState({
    username: "",
    fullName: "",
    email: "",
    phoneNumber: "",
    password: "",
    role: "Worker" as UserRole,
    workerType: "Sanitation" as WorkerType,
    departmentId: "" as string | number,
    municipalityId: "",
    supervisorId: "",
    zoneIds: [] as number[],
  });

  // Device Management State
  const [deviceModalUser, setDeviceModalUser] = useState<UserResponse | null>(null);

  const openDeviceModal = (user: UserResponse) => {
    setDeviceModalUser(user);
  };

  const handleUnbindDevice = async (userId: number) => {
    if (!await confirm("هل أنت متأكد من فك ارتباط جهاز هذا المستخدم؟")) return;
    setActionLoading(userId);
    try {
      await resetDeviceId(userId);
      setUsers(users.map(u => u.userId === userId ? { ...u, deviceId: undefined } : u));
      setMessage({ type: "success", text: "تم فك ارتباط الجهاز بنجاح" });
      if (deviceModalUser?.userId === userId) {
          setDeviceModalUser(null);
      }
    } catch (err) {
      setMessage({ type: "error", text: "فشل فك ارتباط الجهاز" });
    } finally {
      setActionLoading(null);
    }
  };

  useEffect(() => {
    fetchInitialData();
  }, []);

  const fetchInitialData = async () => {
    try {
      setLoading(true);
      const [usersData, munisData, zonesData, deptsData] = await Promise.all([
        getUsers(1, 1000),
        getMunicipalities(),
        getZones(),
        getDepartments(true)
      ]);
      setUsers(usersData.items);
      setSupervisors(usersData.items.filter(u => u.role === "Supervisor"));
      setMunicipalities(munisData);
      setZones(zonesData);
      setDepartments(deptsData);

      if (munisData.length > 0) {
        setFormData(prev => ({ ...prev, municipalityId: munisData[0].municipalityId.toString() }));
      }
    } catch (err) {
      console.error("Failed to fetch data", err);
      setMessage({ type: "error", text: "فشل تحميل البيانات الأساسية" });
    } finally {
      setLoading(false);
    }
  };

  const handleStatusChange = async (userId: number, currentStatus: string) => {
    const newStatus = currentStatus === "Active" ? "Inactive" : "Active";
    const actionText = newStatus === "Active" ? "تفعيل" : "تعطيل";

    if (!await confirm(`هل أنت متأكد من ${actionText} هذا المستخدم؟`)) return;

    setActionLoading(userId);
    try {
      await updateUserStatus(userId, newStatus);
      setUsers(users.map(u => u.userId === userId ? { ...u, status: newStatus as UserStatus } : u));
      setMessage({ type: "success", text: `تم ${actionText} المستخدم بنجاح` });
    } catch (err) {
      setMessage({ type: "error", text: `فشل ${actionText} المستخدم` });
    } finally {
      setActionLoading(null);
    }
  };

  const isStrongPassword = (password: string): boolean => {
    const hasLetter = /[A-Za-z]/.test(password);
    const hasNumber = /\d/.test(password);
    const hasSpecial = /[^A-Za-z0-9]/.test(password);
    return password.length >= 8 && hasLetter && hasNumber && hasSpecial;
  };

  const handlePasswordReset = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resetModalUser) return;

    if (!isStrongPassword(newPassword)) {
      setMessage({ type: "error", text: "كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي على حرف ورقم ورمز خاص (@$!%*#?&)" });
      return;
    }

    setActionLoading(resetModalUser.userId);
    try {
      await resetUserPassword(resetModalUser.userId, newPassword);
      setMessage({ type: "success", text: "تم إعادة تعيين كلمة المرور بنجاح" });
      setResetModalUser(null);
      setNewPassword("");
    } catch (err) {
      setMessage({ type: "error", text: "فشل إعادة تعيين كلمة المرور" });
    } finally {
      setActionLoading(null);
    }
  };

  const openAddModal = () => {
      setIsEditMode(false);
      setEditUserId(null);
      setFormData({
        username: "",
        fullName: "",
        email: "",
        phoneNumber: "",
        password: "",
        role: "Worker",
        workerType: "Sanitation",
        departmentId: "",
        municipalityId: municipalities[0]?.municipalityId.toString() || "",
        supervisorId: "",
        zoneIds: []
      });
      setShowUserModal(true);
  };

  const openEditModal = async (user: UserResponse) => {
      setIsEditMode(true);
      setEditUserId(user.userId);

      let assignedZones: number[] = [];
      try {
          assignedZones = await getUserZones(user.userId);
      } catch (e) {
          console.error("Failed to fetch user zones", e);
      }

      setFormData({
        username: user.username,
        fullName: user.fullName,
        email: user.email || "",
        phoneNumber: user.phoneNumber,
        password: "",
        role: user.role,
        workerType: user.workerType || "Sanitation",
        departmentId: user.departmentId?.toString() || "",
        municipalityId: municipalities[0]?.municipalityId.toString() || "",
        supervisorId: user.role === 'Worker' ? user.supervisorId?.toString() || "" : "",
        zoneIds: assignedZones
      });
      setShowUserModal(true);
  };

  const handleUserSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if ((!isEditMode || formData.password) && formData.password && !isStrongPassword(formData.password)) {
      setMessage({ type: "error", text: "كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي على حرف ورقم ورمز خاص (@$!%*#?&)" });
      return;
    }

    setActionLoading("user-form");
    try {
      const payload = {
        ...formData,
        municipalityId: parseInt(formData.municipalityId),
        departmentId: formData.departmentId ? parseInt(formData.departmentId.toString()) : undefined,
        supervisorId: formData.role === "Worker" && formData.supervisorId ? parseInt(formData.supervisorId) : undefined
      };

      if (isEditMode && editUserId) {
          const { password, ...updateDataBase } = payload;
          const updateData = password ? { ...updateDataBase, password } : updateDataBase;

          await updateUser(editUserId, updateData);
          if (formData.zoneIds.length > 0) {
              await assignUserZones(editUserId, formData.zoneIds);
          }
          setMessage({ type: "success", text: "تم تحديث بيانات المستخدم بنجاح" });
      } else {
          const result = await register(payload);
          if (result.success && result.data?.userId) {
              if (formData.zoneIds.length > 0) {
                  await assignUserZones(result.data.userId, formData.zoneIds);
              }
              setMessage({ type: "success", text: "تم إضافة المستخدم بنجاح" });
          } else {
              throw new Error(result.message || "فشل إضافة المستخدم");
          }
      }

      setShowUserModal(false);
      fetchInitialData().catch(console.error);
    } catch (err) {
      const message = err instanceof Error ? err.message : "حدث خطأ أثناء حفظ البيانات";
      setMessage({ type: "error", text: message });
    } finally {
      setActionLoading(null);
    }
  };

  const filteredUsers = users.filter(user => {
    const search = searchTerm.toLowerCase();
    const matchesSearch = (user.fullName?.toLowerCase() || "").includes(search) ||
                         (user.username?.toLowerCase() || "").includes(search);
    const matchesRole = roleFilter === "all" || user.role === roleFilter;
    return matchesSearch && matchesRole;
  });

  const getSupervisorName = (supervisorId?: number | null) => {
    if (!supervisorId) return "-";
    const supervisor = supervisors.find(s => s.userId === supervisorId);
    return supervisor ? supervisor.fullName : "-";
  };

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#7895B2]/30 border-t-[#7895B2] rounded-full animate-spin"></div>
          <p className="text-[#6B7280] font-bold">جاري تحميل الحسابات...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1300px] mx-auto space-y-6">
          
          {/* Header Section */}
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 border-b border-black/5 pb-8">
            <div className="flex items-center gap-4">
              <div className="p-4 rounded-[20px] bg-white shadow-sm text-[#7895B2] border border-black/5">
                <Users size={32} />
              </div>
              <div className="text-right">
                <h1 className="text-4xl font-black text-[#2F2F2F] tracking-tight">إدارة الحسابات</h1>
                <p className="text-[#6B7280] text-[15px] mt-1 font-bold">إدارة وصلاحيات المستخدمين والمشرفين والعمال في الميدان</p>
              </div>
            </div>

            <button
              onClick={openAddModal}
              className="group flex items-center justify-center gap-3 bg-[#7895B2] hover:bg-[#647e99] text-white px-8 py-4 rounded-[18px] shadow-lg shadow-[#7895B2]/20 transition-all font-black text-[16px] active:scale-95"
            >
              <Plus size={20} />
              إضافة مستخدم جديد
            </button>
          </div>

          {/* Search & Statistics Overview */}
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
             {/* Search box (8 units) */}
             <div className="lg:col-span-8 bg-white rounded-[22px] p-2 shadow-sm border border-black/5 flex items-center">
                <div className="flex-1 relative">
                  <input
                    type="text"
                    placeholder="ابحث عن أي مستخدم بالاسم، اسم المستخدم، أو الدور..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="w-full h-[54px] pr-14 pl-6 bg-transparent text-right text-[15px] font-bold text-[#202020] placeholder:text-[#9CA3AF] outline-none"
                  />
                  <Search className="absolute right-5 top-1/2 -translate-y-1/2 text-[#9CA3AF]" size={22} />
                </div>
                <div className="w-[1px] h-8 bg-black/5 mx-2 hidden sm:block"></div>
                <select
                  value={roleFilter}
                  onChange={(e) => setRoleFilter(e.target.value)}
                  className="h-[54px] px-6 bg-transparent rounded-r-[18px] text-right text-[14px] font-black text-[#6B7280] outline-none cursor-pointer hover:text-[#7895B2] transition-colors"
                >
                  <option value="all">جميع الأدوار</option>
                  <option value="Admin">مدير نظام</option>
                  <option value="Supervisor">مشرف</option>
                  <option value="Worker">عامل</option>
                </select>
             </div>

             {/* Total counts (4 units) */}
             <div className="lg:col-span-4 bg-white rounded-[22px] px-8 py-2 shadow-sm border border-black/5 flex items-center justify-between">
                <div className="text-center">
                   <p className="text-[10px] text-[#AFAFAF] font-black uppercase">إجمالي الحسابات</p>
                   <p className="text-2xl font-black text-[#2F2F2F]">{users.length}</p>
                </div>
                <div className="w-[1px] h-8 bg-black/5"></div>
                <div className="text-center">
                   <p className="text-[10px] text-[#AFAFAF] font-black uppercase">نشطين الآن</p>
                   <p className="text-2xl font-black text-[#8FA36A]">{users.filter(u => u.status === 'Active').length}</p>
                </div>
             </div>
          </div>

          {/* Status Message */}
          {message && (
            <div className={`p-5 rounded-[20px] flex items-center justify-between gap-4 animate-in fade-in slide-in-from-top-4 duration-300 ${
              message.type === "success"
                ? "bg-[#8FA36A]/10 text-[#5a6b41] border border-[#8FA36A]/20"
                : "bg-[#C86E5D]/10 text-[#9b5143] border border-[#C86E5D]/20"
            }`}>
              <button onClick={() => setMessage(null)} className="opacity-40 hover:opacity-100 transition-opacity"><X size={18} /></button>
              <div className="flex items-center gap-3">
                 <span className="font-black text-[15px]">{message.text}</span>
                 {message.type === "success" ? <CheckCircle size={22} /> : <AlertCircle size={22} />}
              </div>
            </div>
          )}

          {/* Main Table Content */}
          <div className="bg-white rounded-[28px] shadow-[0_4px_30px_rgba(0,0,0,0.03)] border border-black/5 overflow-hidden">
            {/* Table Header */}
            <div className="hidden md:grid grid-cols-12 gap-4 px-8 py-5 bg-[#F9F8F6] border-b border-black/5 text-[#6B7280] text-[12px] font-black uppercase tracking-wider">
              <div className="col-span-4 text-right">معلومات المستخدم</div>
              <div className="col-span-3 text-right">الدور / القسم</div>
              <div className="col-span-2 text-right">المشرف المسؤول</div>
              <div className="col-span-1 text-center">الحالة</div>
              <div className="col-span-2 text-center">الإجراءات الخيارات</div>
            </div>

            {/* List Rows */}
            <div className="divide-y divide-black/5">
              {filteredUsers.map((user) => (
                <div key={user.userId} className="group hover:bg-[#F9F8F6] transition-colors px-8 py-6 md:grid md:grid-cols-12 md:gap-4 md:items-center">
                  
                  {/* Column 1: Identity */}
                  <div className="col-span-4 flex items-center gap-4 justify-start mb-4 md:mb-0">
                    <div className="relative">
                      <div className="w-14 h-14 rounded-[20px] bg-[#7895B2]/10 flex items-center justify-center text-[#7895B2] group-hover:bg-white transition-colors border border-[#7895B2]/5 shadow-sm">
                        <UserIcon size={24} />
                      </div>
                      <div className={`absolute -bottom-1 -right-1 w-4 h-4 rounded-full border-2 border-white ${user.status === 'Active' ? 'bg-[#8FA36A]' : 'bg-[#C86E5D]'}`}></div>
                    </div>
                    <div className="text-right">
                      <p className="font-black text-[#2F2F2F] text-[17px] leading-tight">{user.fullName}</p>
                    </div>
                  </div>

                  {/* Column 2: Role & Dept */}
                  <div className="col-span-3 flex md:flex-col items-center md:items-start justify-between md:justify-center mb-4 md:mb-0">
                    <span className="md:hidden text-[#6B7280] text-[11px] font-black uppercase">المنصب:</span>
                    <div className="flex flex-col items-end md:items-start text-right">
                      <span className={`inline-flex items-center justify-center px-4 py-1.5 rounded-xl text-[12px] font-black shadow-sm border ${
                        user.role === 'Admin' ? 'bg-[#7895B2] text-white border-[#7895B2]/20' :
                        user.role === 'Supervisor' ? 'bg-[#8FA36A] text-white border-[#8FA36A]/20' :
                        'bg-[#F5B300] text-white border-[#F5B300]/20'
                      }`}>
                        {user.role === 'Admin' ? 'مدير نظام' : user.role === 'Supervisor' ? 'مشرف ميداني' : 'عامل ميداني'}
                      </span>
                      <div className="mt-2 text-right">
                        {user.department ? (
                          <span className="text-[13px] text-[#2F2F2F] font-black flex items-center gap-1 justify-end md:justify-start">
                             <Briefcase size={14} className="text-[#AFAFAF]" />
                             {user.department}
                          </span>
                        ) : user.workerType ? (
                          <span className="text-[13px] text-[#7895B2] font-black flex items-center gap-1 justify-end md:justify-start">
                             <Briefcase size={14} className="text-[#AFAFAF]" />
                             {user.workerType === 'Sanitation' ? 'قسم الصحة والنظافة' : 
                              user.workerType === 'PublicWorks' ? 'قسم الأشغال' : 
                              user.workerType === 'Agriculture' ? 'قسم الزراعة' : 
                              user.workerType === 'Maintenance' ? 'قسم الصيانة' : user.workerType}
                          </span>
                        ) : (
                          <span className="text-[13px] text-[#AFAFAF] font-bold">غير محدد</span>
                        )}
                      </div>
                    </div>
                  </div>

                  {/* Column 3: Supervisor */}
                  <div className="col-span-2 flex md:flex-col items-center md:items-start justify-between md:justify-center mb-4 md:mb-0">
                    <span className="md:hidden text-[#6B7280] text-[11px] font-black uppercase">المسؤول:</span>
                    <div className="text-right">
                      {user.role === 'Worker' ? (
                        <div className="flex items-center gap-2">
                           <div className="w-1.5 h-6 bg-[#AFAFAF]/20 rounded-full hidden md:block"></div>
                           <span className="text-[14px] text-[#2F2F2F] font-bold">
                             {getSupervisorName(user.supervisorId)}
                           </span>
                        </div>
                      ) : (
                        <span className="text-[13px] text-[#AFAFAF] italic font-medium px-4">لا ينطبق</span>
                      )}
                    </div>
                  </div>

                  {/* Column 4: Status Indicator */}
                  <div className="col-span-1 flex md:flex-col items-center justify-between md:justify-center mb-6 md:mb-0">
                    <span className="md:hidden text-[#6B7280] text-[11px] font-black uppercase">الحالة:</span>
                    <div className="flex flex-col items-center">
                       <span className={`text-[11px] font-black uppercase tracking-widest ${user.status === 'Active' ? 'text-[#8FA36A]' : 'text-[#C86E5D]'}`}>
                          {user.status === 'Active' ? 'نشط' : 'معطل'}
                       </span>
                    </div>
                  </div>

                  {/* Column 5: Actions */}
                  <div className="col-span-2 flex items-center justify-center gap-2 border-t border-black/5 pt-6 md:pt-0 md:border-t-0">
                    <ActionBtn icon={Edit2} color="#7895B2" title="تعديل البيانات" onClick={() => openEditModal(user)} />
                    <ActionBtn icon={KeyRound} color="#F5B300" title="كلمة المرور" onClick={() => setResetModalUser(user)} />
                    <ActionBtn icon={Smartphone} color="#7895B2" title="الجهاز" onClick={() => openDeviceModal(user)} />
                    <ActionBtn 
                      icon={user.status === "Active" ? UserX : UserCheck} 
                      color={user.status === "Active" ? "#C86E5D" : "#8FA36A"} 
                      title={user.status === "Active" ? "تعطيل" : "تفعيل"} 
                      loading={actionLoading === user.userId}
                      onClick={() => handleStatusChange(user.userId, user.status)} 
                    />
                  </div>
                </div>
              ))}

              {filteredUsers.length === 0 && (
                <div className="p-12 text-center">
                  <div className="w-24 h-24 bg-[#F3F1ED] rounded-full flex items-center justify-center mx-auto mb-6">
                    <Users size={40} className="text-[#AFAFAF]" />
                  </div>
                  <h3 className="text-xl font-black text-[#2F2F2F]">لم نجد أي مطابقات</h3>
                  <p className="text-[#C86E5D] font-bold mt-2">جرب البحث بكلمة دور أخرى أو تأكد من الاسم المكتوب.</p>
                  <button 
                    onClick={() => { setSearchTerm(''); setRoleFilter('all'); }}
                    className="mt-6 text-[14px] font-black text-[#7895B2] hover:underline"
                  >
                    عرض كافة الحسابات
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Password Reset Modal */}
      {resetModalUser && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-[16px] w-full max-w-md p-6 shadow-xl text-right">
            <div className="flex items-center justify-between mb-6">
              <button onClick={() => setResetModalUser(null)} className="p-2 hover:bg-[#F3F1ED] rounded-full transition-colors text-[#6B7280]">
                <X size={20} />
              </button>
              <h3 className="text-[18px] font-black text-[#2F2F2F]">إعادة تعيين كلمة المرور</h3>
            </div>

            <p className="text-right text-[#6B7280] mb-6 text-[14px]">
              سيتم تعيين كلمة مرور جديدة للمستخدم <span className="font-bold text-[#2F2F2F]">{resetModalUser.fullName}</span>
            </p>

            <form onSubmit={handlePasswordReset}>
              <div className="mb-6 space-y-3">
                <label className="block text-right text-[12px] font-semibold text-[#6B7280]">كلمة المرور الجديدة</label>
                <input
                  type="password"
                  value={newPassword}
                  onChange={(e) => setNewPassword(e.target.value)}
                  className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  placeholder="مثال: Pass@123"
                  required
                  minLength={8}
                />
                <p className="text-[11px] text-[#6B7280] text-right">8 أحرف على الأقل + حرف + رقم + رمز خاص (@$!%*#?&)</p>
              </div>

              <div className="flex gap-3">
                <button
                  type="button"
                  onClick={() => setResetModalUser(null)}
                  className="flex-1 h-[46px] rounded-[12px] border border-[#E5E7EB] text-[#2F2F2F] hover:bg-[#F3F1ED] transition-all font-semibold text-[14px]"
                >
                  إلغاء
                </button>
                <button
                  type="submit"
                  disabled={!!actionLoading}
                  className="flex-1 h-[46px] rounded-[12px] bg-[#7895B2] hover:bg-[#6B87A3] text-white transition-all font-semibold text-[14px] disabled:opacity-50"
                >
                  {actionLoading === resetModalUser.userId ? "جاري الحفظ..." : "حفظ التغييرات"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Add/Edit User Modal */}
      {showUserModal && (
        <div className="fixed inset-0 bg-black/40 flex justify-center items-start pt-10 pb-20 p-4 z-50 overflow-y-auto">
          <div className="bg-white rounded-[16px] w-full max-w-3xl p-6 shadow-xl my-8 text-right">
            <div className="flex items-center justify-between mb-6">
              <button onClick={() => setShowUserModal(false)} className="p-2 hover:bg-[#F3F1ED] rounded-full transition-colors text-[#6B7280]">
                <X size={20} />
              </button>
              <div className="flex items-center gap-3">
                <h3 className="text-[18px] font-black text-[#2F2F2F]">{isEditMode ? "تعديل بيانات المستخدم" : "إضافة مستخدم جديد"}</h3>
                <div className="p-2 bg-[#7895B2]/10 rounded-[10px] text-[#7895B2]">
                  {isEditMode ? <Edit2 size={20} /> : <Plus size={20} />}
                </div>
              </div>
            </div>

            <form onSubmit={handleUserSubmit} className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">الاسم الكامل</label>
                  <input
                    required
                    type="text"
                    value={formData.fullName}
                    onChange={(e) => setFormData({...formData, fullName: e.target.value})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                    placeholder="مثال: أحمد محمد علي"
                  />
                </div>
                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">اسم المستخدم</label>
                  <input
                    required
                    disabled={isEditMode}
                    type="text"
                    value={formData.username}
                    onChange={(e) => setFormData({...formData, username: e.target.value.toLowerCase().replace(/\s/g, '')})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30 disabled:opacity-50 font-sans"
                    placeholder="username"
                  />
                </div>
                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">رقم الهاتف</label>
                  <input
                    required
                    type="tel"
                    value={formData.phoneNumber}
                    onChange={(e) => setFormData({...formData, phoneNumber: e.target.value})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                    placeholder="05xxxxxxx"
                  />
                </div>
                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">
                    {isEditMode ? "كلمة المرور (اتركها فارغة للتجاهل)" : "كلمة المرور"}
                  </label>
                  <input
                    required={!isEditMode}
                    type="password"
                    value={formData.password}
                    onChange={(e) => setFormData({...formData, password: e.target.value})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                    placeholder={isEditMode ? "********" : "مثال: Pass@123"}
                    minLength={8}
                  />
                </div>
                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">البريد الإلكتروني</label>
                  <input
                    required={formData.role !== 'Worker'}
                    type="email"
                    value={formData.email}
                    onChange={(e) => setFormData({...formData, email: e.target.value})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                    placeholder="example@municipality.ps"
                  />
                </div>
                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">القسم / الدائرة</label>
                  <select
                    value={formData.departmentId}
                    onChange={(e) => setFormData({...formData, departmentId: e.target.value})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  >
                    <option value="">-- اختر القسم --</option>
                    {departments.map((dept) => (
                      <option key={dept.departmentId} value={dept.departmentId}>{dept.name}</option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 pt-5 border-t border-black/5">
                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">الدور الوظيفي</label>
                  <select
                    value={formData.role}
                    onChange={(e) => setFormData({...formData, role: e.target.value as UserRole})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  >
                    <option value="Worker">عامل</option>
                    <option value="Supervisor">مشرف</option>
                    <option value="Admin">مدير نظام</option>
                  </select>
                </div>

                {formData.role === "Worker" && (
                  <div className="space-y-3">
                    <label className="block text-right text-[12px] font-semibold text-[#6B7280]">المشرف المسؤول</label>
                    <select
                      value={formData.supervisorId}
                      onChange={(e) => setFormData({...formData, supervisorId: e.target.value})}
                      className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                    >
                      <option value="">بدون مشرف</option>
                      {supervisors.map(s => (
                        <option key={s.userId} value={s.userId}>{s.fullName}</option>
                      ))}
                    </select>
                  </div>
                )}
              </div>

              {/* Show zones only for Supervisor - Admin assigns zones to supervisors, supervisors assign workers via MyWorkers page */}
              {formData.role === "Supervisor" && (
                <div className="pt-5 border-t border-black/5">
                  <label className="block text-right text-[12px] font-bold text-[#6B7280] mb-3">المناطق المسندة للمشرف</label>
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-2 max-h-48 overflow-y-auto p-3 bg-[#F3F1ED] rounded-2xl">
                    {zones.map(zone => (
                      <label key={zone.zoneId} className={`flex items-center justify-end gap-2 p-2.5 rounded-xl border cursor-pointer transition-all ${
                        formData.zoneIds.includes(zone.zoneId) ? "bg-[#7895B2]/10 border-[#7895B2]/40" : "bg-white border-black/5"
                      }`}>
                        <span className="text-[13px] font-bold">{zone.zoneName}</span>
                        <input
                          type="checkbox"
                          className="w-4 h-4 rounded border-gray-300 text-[#7895B2] focus:ring-[#7895B2]"
                          checked={formData.zoneIds.includes(zone.zoneId)}
                          onChange={(e) => {
                            if (e.target.checked) setFormData({...formData, zoneIds: [...formData.zoneIds, zone.zoneId]});
                            else setFormData({...formData, zoneIds: formData.zoneIds.filter(id => id !== zone.zoneId)});
                          }}
                        />
                      </label>
                    ))}
                  </div>
                </div>
              )}

              <div className="flex gap-3 pt-6">
                <button
                  type="button"
                  onClick={() => setShowUserModal(false)}
                  className="flex-1 h-14 rounded-2xl border border-black/10 text-[#2F2F2F] font-black hover:bg-[#F3F1ED] transition-all"
                >
                  إلغاء
                </button>
                <button
                  type="submit"
                  disabled={actionLoading === "user-form"}
                  className="flex-1 h-14 rounded-2xl bg-[#7895B2] text-white font-black hover:bg-[#647e99] transition-all shadow-lg shadow-[#7895B2]/20"
                >
                  {actionLoading === "user-form" ? "جاري الحفظ..." : (isEditMode ? "حفظ التعديلات" : "إضافة المستخدم")}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {ConfirmDialog}

      {/* Device Modal */}
      {deviceModalUser && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-[16px] w-full max-w-md p-6 shadow-xl text-right">
            <div className="flex items-center justify-between mb-6">
              <button onClick={() => setDeviceModalUser(null)} className="p-2 hover:bg-[#F3F1ED] rounded-full transition-colors text-[#6B7280]">
                <X size={20} />
              </button>
              <h3 className="text-[18px] font-black text-[#2F2F2F]">إدارة جهاز المستخدم</h3>
            </div>
            
            <div className="space-y-6">
               <div className="p-4 bg-[#F3F1ED] rounded-2xl text-center">
                  <Smartphone size={32} className="mx-auto text-[#7895B2] mb-2" />
                  <p className="font-black text-[#2F2F2F]">{deviceModalUser.fullName}</p>
                  <p className="font-sans text-[12px] text-[#6B7280] mt-1">{deviceModalUser.deviceId || 'لا يوجد جهاز مرتبط'}</p>
               </div>

               <div className="grid grid-cols-2 gap-3">
                  <button 
                    onClick={() => handleUnbindDevice(deviceModalUser.userId)}
                    className="h-12 rounded-xl border border-[#C86E5D] text-[#C86E5D] font-black text-sm hover:bg-[#C86E5D]/5 transition-colors"
                  >
                    فك الارتباط
                  </button>
                  <button 
                    onClick={() => setDeviceModalUser(null)}
                    className="h-12 rounded-xl bg-[#7895B2] text-white font-black text-sm transition-colors"
                  >
                    إغلاق
                  </button>
               </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

function ActionBtn({ icon: Icon, color, title, onClick, loading }: { icon: React.ComponentType<{ size?: number; style?: React.CSSProperties; className?: string }>; color: string; title: string; onClick: () => void; loading?: boolean }) {
  return (
    <button
      onClick={onClick}
      disabled={loading}
      className="w-10 h-10 rounded-xl flex items-center justify-center transition-all bg-[#F3F1ED] hover:bg-white hover:shadow-lg group shadow-sm border border-black/5 active:scale-90 disabled:opacity-50"
      title={title}
    >
      {loading ? (
        <Loader2 size={16} className="animate-spin text-gray-400" />
      ) : (
        <Icon size={18} style={{ color: color }} className="group-hover:scale-110 transition-transform" />
      )}
    </button>
  );
}
