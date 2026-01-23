import { useEffect, useState } from "react";
import { getUsers, resetDeviceId, updateDeviceId, resetUserPassword, updateUserStatus, assignUserZones, updateUser, getUserZones } from "../api/users";
import { register } from "../api/auth";
import { getMunicipalities } from "../api/municipality";
import { getZones } from "../api/zones";
import type { Municipality } from "../api/municipality";
import type { ZoneResponse } from "../types/zone";
import type { UserResponse, UserRole, WorkerType } from "../types/user";
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
  Edit2,
  Save,
  MapPin
} from "lucide-react";
import GlassCard from "../components/UI/GlassCard";

export default function AdminAccounts() {
  const [users, setUsers] = useState<UserResponse[]>([]);
  const [supervisors, setSupervisors] = useState<UserResponse[]>([]);
  const [municipalities, setMunicipalities] = useState<Municipality[]>([]);
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
    department: "",
    municipalityId: "",
    supervisorId: "",
    zoneIds: [] as number[],
  });

  // Device Management State
  const [deviceModalUser, setDeviceModalUser] = useState<UserResponse | null>(null);
  const [deviceAction, setDeviceAction] = useState<'view' | 'edit'>('view');
  const [targetDeviceId, setTargetDeviceId] = useState("");

  const openDeviceModal = (user: UserResponse) => {
    setDeviceModalUser(user);
    setTargetDeviceId(user.deviceId || "");
    setDeviceAction('view');
  };

  const handleUpdateDevice = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!deviceModalUser) return;

    setActionLoading(deviceModalUser.userId);
    try {
        await updateDeviceId(deviceModalUser.userId, targetDeviceId);
        setMessage({ type: "success", text: "تم تحديث معرف الجهاز بنجاح" });
        setUsers(users.map(u => u.userId === deviceModalUser.userId ? { ...u, deviceId: targetDeviceId } : u));
        setDeviceModalUser(null);
    } catch (err) {
        setMessage({ type: "error", text: "فشل تحديث معرف الجهاز" });
    } finally {
        setActionLoading(null);
    }
  };

  const handleUnbindDevice = async (userId: number) => {
    if (!window.confirm("هل أنت متأكد من فك ارتباط جهاز هذا المستخدم؟")) return;
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
      const [usersData, supervisorsData, munisData, zonesData] = await Promise.all([
        getUsers(1, 100),
        getUsers(1, 1000, "Supervisor"),
        getMunicipalities(),
        getZones()
      ]);
      setUsers(usersData.items);
      setSupervisors(supervisorsData.items);
      setMunicipalities(munisData);
      setZones(zonesData);
      
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

    if (!window.confirm(`هل أنت متأكد من ${actionText} هذا المستخدم؟`)) return;

    setActionLoading(userId);
    try {
      await updateUserStatus(userId, newStatus);
      setUsers(users.map(u => u.userId === userId ? { ...u, status: newStatus as any } : u));
      setMessage({ type: "success", text: `تم ${actionText} المستخدم بنجاح` });
    } catch (err) {
      setMessage({ type: "error", text: `فشل ${actionText} المستخدم` });
    } finally {
      setActionLoading(null);
    }
  };

  // Validate password strength - must have 8+ chars, letter, number, and special character
  // Matches backend InputSanitizer.IsStrongPassword logic
  const isStrongPassword = (password: string): boolean => {
    const hasLetter = /[A-Za-z]/.test(password);
    const hasNumber = /\d/.test(password);
    const hasSpecial = /[^A-Za-z0-9]/.test(password); // Any non-alphanumeric character
    return password.length >= 8 && hasLetter && hasNumber && hasSpecial;
  };

  const handlePasswordReset = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!resetModalUser) return;

    // Validate password strength
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
        department: "",
        municipalityId: municipalities[0]?.municipalityId.toString() || "",
        supervisorId: "",
        zoneIds: []
      });
      setShowUserModal(true);
  };

  const openEditModal = async (user: UserResponse) => {
      setIsEditMode(true);
      setEditUserId(user.userId);
      
      // Fetch assigned zones
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
        password: "", // Password key only sent if updated
        role: user.role,
        workerType: user.workerType || "Sanitation",
        department: user.department || "",
        municipalityId: municipalities[0]?.municipalityId.toString() || "",
        // Note: user.supervisorId is assumed to be available or derived from somewhere
        supervisorId: user.role === 'Worker' ? (user as any).supervisorId?.toString() || "" : "",
        zoneIds: assignedZones
      });
      setShowUserModal(true);
  };

  const handleUserSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Validate password for new users or when password is being changed
    if ((!isEditMode || formData.password) && formData.password && !isStrongPassword(formData.password)) {
      setMessage({ type: "error", text: "كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي على حرف ورقم ورمز خاص (@$!%*#?&)" });
      return;
    }

    setActionLoading("user-form");
    try {
      const payload: any = {
        ...formData,
        municipalityId: parseInt(formData.municipalityId),
        supervisorId: formData.role === "Worker" && formData.supervisorId ? parseInt(formData.supervisorId) : null
      };

      if (isEditMode && editUserId) {
          // Update
          const { password, ...updateData } = payload;
          if (password) updateData.password = password; // Only send if changed
          
          await updateUser(editUserId, updateData);
          if (formData.zoneIds.length > 0) {
              await assignUserZones(editUserId, formData.zoneIds);
          }
          setMessage({ type: "success", text: "تم تحديث بيانات المستخدم بنجاح" });
      } else {
          // Create
          const result = await register(payload);
          if (result.success && result.data?.userId) {
              // Assign zones if any
              if (formData.zoneIds.length > 0) {
                  await assignUserZones(result.data.userId, formData.zoneIds);
              }
              setMessage({ type: "success", text: "تم إضافة المستخدم بنجاح" });
          } else {
              throw new Error(result.message || "فشل إضافة المستخدم");
          }
      }

      setShowUserModal(false);
      fetchInitialData(); 
    } catch (err: any) {
      setMessage({ type: "error", text: err.message || "حدث خطأ أثناء حفظ البيانات" });
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

  const getSupervisorName = (supervisorId?: number | null) => {
    if (!supervisorId) return "-";
    const supervisor = supervisors.find(s => s.userId === supervisorId);
    return supervisor ? supervisor.fullName : "-";
  };

  if (loading) {
    return (
      <div className="h-full w-full flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
             <div className="w-12 h-12 border-4 border-primary/30 border-t-primary rounded-full animate-spin"></div>
             <p className="text-text-secondary font-medium">جاري تحديث البيانات...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8 pb-10">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
          <div className="flex flex-col items-start">
            <h1 className="text-3xl font-extrabold text-text-primary">
                إدارة الحسابات
            </h1>
            <p className="text-right text-text-secondary mt-2 font-medium">إدارة المستخدمين، المشرفين، والعمال في النظام</p>
          </div>
          
          <button 
            onClick={openAddModal}
            className="flex items-center gap-2 bg-primary hover:bg-primary-dark text-white px-6 py-3 rounded-xl transition-all shadow-lg shadow-primary/20 hover:shadow-primary/40 active:transform active:scale-95 font-bold"
          >
            <Plus size={20} />
            إضافة مستخدم جديد
          </button>
        </div>

        <GlassCard className="flex flex-col sm:flex-row gap-4">
            <div className="flex-1 relative group">
                <input
                    type="text"
                    placeholder="بحث عن مستخدم بالاسم أو اسم المستخدم..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="glass-input w-full h-12 pr-11 text-right focus:bg-primary/5 text-text-primary"
                />
                <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-text-muted group-focus-within:text-primary transition-colors" size={20} />
            </div>

            <select
                value={roleFilter}
                onChange={(e) => setRoleFilter(e.target.value)}
                className="glass-input h-12 w-full sm:w-[200px] text-right appearance-none cursor-pointer focus:bg-primary/5 text-text-primary [&>option]:text-black"
            >
                <option value="all">جميع الأدوار</option>
                <option value="Admin">مدير نظام</option>
                <option value="Supervisor">مشرف</option>
                <option value="Worker">عامل</option>
            </select>
        </GlassCard>

        {message && (
          <div className={`p-4 rounded-xl flex items-center justify-end gap-3 text-right animate-fade-in ${
            message.type === "success" ? "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20" : "bg-red-500/10 text-red-400 border border-red-500/20"
          }`}>
            <span className="font-bold">{message.text}</span>
            {message.type === "success" ? <CheckCircle size={20} /> : <AlertCircle size={20} />}
          </div>
        )}

        <div className="space-y-4">
            <div className="hidden md:grid grid-cols-12 gap-4 px-6 text-text-muted text-sm font-bold opacity-70">
                <div className="col-span-4 text-right">المستخدم</div>
                <div className="col-span-3 text-right">الدور / التخصص</div>
                <div className="col-span-2 text-right">المشرف</div>
                <div className="col-span-1 text-center">الحالة</div>
                <div className="col-span-2 text-center">الإجراءات</div>
            </div>

            {filteredUsers.map((user) => (
                <GlassCard key={user.userId} variant="hover" noPadding className="p-4 md:grid md:grid-cols-12 md:gap-4 md:items-center">
                    <div className="col-span-4 flex items-center gap-3 justify-start mb-4 md:mb-0">
                        <div className="flex-shrink-0 w-12 h-12 rounded-full bg-primary/10 flex items-center justify-center text-primary border border-primary/5">
                          <UserIcon size={24} />
                        </div>
                        <div className="text-right">
                          <p className="font-bold text-text-primary text-lg">{user.fullName}</p>
                          <div className="flex items-center gap-2 justify-start mt-1">
                              {user.department && <span className="text-[10px] bg-primary/10 px-2 py-0.5 rounded text-text-secondary">{user.department}</span>}
                              <p className="text-xs text-text-muted font-mono bg-primary/5 px-2 py-0.5 rounded-md">@{user.username}</p>
                          </div>
                        </div>
                    </div>

                    <div className="col-span-3 flex md:flex-col items-center md:items-end justify-between md:justify-center mb-2 md:mb-0">
                        <span className="md:hidden text-text-muted text-xs font-bold">الدور:</span>
                        <div className="flex flex-col items-end">
                            <span className={`inline-flex items-center justify-center px-3 py-1 rounded-full text-xs font-bold ${
                              user.role === 'Admin' ? 'bg-primary/20 text-primary border border-primary/30' :
                              user.role === 'Supervisor' ? 'bg-secondary/20 text-secondary border border-secondary/30' :
                              'bg-warning/20 text-warning border border-warning/30'
                            }`}>
                              {user.role}
                            </span>
                            {user.workerType && (
                              <span className="mt-1 text-[10px] text-text-muted flex items-center gap-1">
                                {user.workerType} <Briefcase size={10} />
                              </span>
                            )}
                        </div>
                    </div>

                    <div className="col-span-2 flex md:flex-col items-center md:items-end justify-between md:justify-center mb-2 md:mb-0">
                        <span className="md:hidden text-text-muted text-xs font-bold">المشرف:</span>
                        <span className="text-sm text-text-secondary font-medium">
                            {user.role === 'Worker' ? getSupervisorName((user as any).supervisorId) : '-'}
                        </span>
                    </div>

                    <div className="col-span-1 flex md:flex-col items-center justify-between md:justify-center mb-4 md:mb-0">
                        <span className="md:hidden text-text-muted text-xs font-bold">الحالة:</span>
                        <div className={`w-2.5 h-2.5 rounded-full ${user.status === 'Active' ? 'bg-secondary' : 'bg-accent'}`} title={user.status === 'Active' ? 'نشط' : 'معطل'} />
                    </div>

                    <div className="col-span-2 flex items-center justify-center gap-2 border-t border-primary/5 pt-4 md:pt-0 md:border-t-0">
                         <button
                          onClick={() => openEditModal(user)}
                          className="p-2 text-text-secondary hover:text-white hover:bg-white/10 rounded-lg transition-colors"
                          title="تعديل المستخدم"
                        >
                          <Edit2 size={18} />
                        </button>
                        <button
                          onClick={() => setResetModalUser(user)}
                          className="p-2 text-text-secondary hover:text-white hover:bg-white/10 rounded-lg transition-colors"
                          title="تغيير كلمة المرور"
                        >
                          <KeyRound size={18} />
                        </button>

                        <button
                          onClick={() => openDeviceModal(user)}
                          className="p-2 text-text-secondary hover:text-blue-400 hover:bg-blue-500/10 rounded-lg transition-colors"
                          title="إدارة الجهاز"
                        >
                          <Smartphone size={18} />
                        </button>

                        <button
                          onClick={() => handleStatusChange(user.userId, user.status)}
                          disabled={actionLoading === user.userId}
                          className={`p-2 rounded-lg transition-colors disabled:opacity-50 ${
                            user.status === "Active" 
                              ? "text-text-secondary hover:text-red-400 hover:bg-red-500/10" 
                              : "text-text-secondary hover:text-emerald-400 hover:bg-emerald-500/10"
                          }`}
                          title={user.status === "Active" ? "تعطيل الحساب" : "تفعيل الحساب"}
                        >
                          {user.status === "Active" ? <UserX size={18} /> : <UserCheck size={18} />}
                        </button>
                    </div>
                </GlassCard>
            ))}

            {filteredUsers.length === 0 && (
                <GlassCard className="py-16 text-center flex flex-col items-center">
                    <Users size={64} className="text-primary/10 mb-4" />
                    <p className="text-text-muted text-lg font-medium">لا يوجد مستخدمين مطابقين للبحث</p>
                </GlassCard>
            )}
        </div>
      
        {/* Password Reset Modal */}
         {resetModalUser && (
            <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 z-50 animate-fade-in">
            <GlassCard variant="panel" className="w-full max-w-md !bg-background-paper !border-primary/10 shadow-xl">
                <div className="flex items-center justify-between mb-6 border-b border-primary/5 pb-4">
                    <button onClick={() => setResetModalUser(null)} className="p-2 hover:bg-primary/5 rounded-full transition-colors text-text-primary">
                        <X size={20} />
                    </button>
                    <h3 className="text-xl font-bold text-text-primary text-right">إعادة تعيين كلمة المرور</h3>
                </div>
                
                <p className="text-right text-text-secondary mb-8 leading-relaxed">
                سيتم تعيين كلمة مرور جديدة للمستخدم <span className="font-bold text-text-primary">{resetModalUser.fullName}</span>
                </p>
                
                <form onSubmit={handlePasswordReset}>
                <div className="mb-8 space-y-3">
                    <label className="block text-right text-sm font-bold text-text-muted">كلمة المرور الجديدة</label>
                    <input
                    type="text"
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    className="glass-input w-full h-12 text-right !bg-primary/5 text-text-primary"
                    placeholder="مثال: Pass@123"
                    required
                    minLength={8}
                    pattern="^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$"
                    title="يجب أن تحتوي كلمة المرور على 8 أحرف على الأقل، حرف، رقم، ورمز خاص"
                    />
                    <p className="text-xs text-text-muted text-right">8 أحرف على الأقل + حرف + رقم + رمز خاص (@$!%*#?&)</p>
                </div>

                <div className="flex gap-4">
                    <button
                        type="button"
                        onClick={() => setResetModalUser(null)}
                        className="flex-1 h-12 rounded-xl border border-primary/10 text-text-primary hover:bg-primary/5 transition-all font-bold"
                    >
                    إلغاء
                    </button>
                    <button
                        type="submit"
                        disabled={!!actionLoading}
                        className="flex-1 h-12 rounded-xl bg-primary hover:bg-primary-dark text-white transition-all font-bold shadow-lg shadow-primary/20 disabled:opacity-50"
                    >
                    {actionLoading === resetModalUser.userId ? "جاري الحفظ..." : "حفظ التغييرات"}
                    </button>
                </div>
                </form>
            </GlassCard>
            </div>
        )}

        {/* Add/Edit User Modal */}
        {showUserModal && (
            <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex justify-center items-start pt-10 pb-20 p-4 z-50 animate-fade-in overflow-y-auto">
            <GlassCard variant="panel" className="w-full max-w-3xl !bg-background-paper !border-primary/10 shadow-xl my-8 relative">
                <div className="flex items-center justify-between mb-8 border-b border-primary/5 pb-4">
                    <div className="flex items-center gap-3">
                        <h3 className="text-2xl font-bold text-text-primary text-right">{isEditMode ? "تعديل بيانات المستخدم" : "إضافة مستخدم جديد"}</h3>
                        <div className="p-2 bg-primary/10 rounded-xl text-primary">
                            {isEditMode ? <Edit2 size={24} /> : <Plus size={24} />}
                        </div>
                    </div>
                    <button onClick={() => setShowUserModal(false)} className="p-2 hover:bg-primary/5 rounded-full transition-colors text-text-primary">
                        <X size={20} />
                    </button>
                </div>

                <form onSubmit={handleUserSubmit} className="space-y-6">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        <div className="space-y-2">
                            <label className="block text-right text-sm font-bold text-text-muted">الاسم الكامل</label>
                            <input
                                required
                                type="text"
                                value={formData.fullName}
                                onChange={(e) => setFormData({...formData, fullName: e.target.value})}
                                className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary"
                                placeholder="مثال: أحمد محمد علي"
                            />
                        </div>
                        <div className="space-y-2">
                            <label className="block text-right text-sm font-bold text-text-muted">اسم المستخدم</label>
                            <input
                                required
                                disabled={isEditMode}
                                type="text"
                                value={formData.username}
                                onChange={(e) => setFormData({...formData, username: e.target.value.toLowerCase().replace(/\s/g, '')})}
                                className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary disabled:opacity-50"
                                placeholder="username"
                            />
                        </div>
                        <div className="space-y-2">
                            <label className="block text-right text-sm font-bold text-text-muted">رقم الهاتف</label>
                            <input
                                required
                                type="tel"
                                value={formData.phoneNumber}
                                onChange={(e) => setFormData({...formData, phoneNumber: e.target.value})}
                                className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary"
                                placeholder="05xxxxxxx"
                            />
                        </div>
                        <div className="space-y-2">
                            <label className="block text-right text-sm font-bold text-text-muted">{isEditMode ? "كلمة المرور (اتركها فارغة للتجاهل)" : "كلمة المرور"}</label>
                            <input
                                required={!isEditMode}
                                type="password"
                                value={formData.password}
                                onChange={(e) => setFormData({...formData, password: e.target.value})}
                                className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary"
                                placeholder={isEditMode ? "********" : "مثال: Pass@123"}
                                minLength={8}
                                pattern="^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*#?&])[A-Za-z\d@$!%*#?&]{8,}$"
                                title="يجب أن تحتوي كلمة المرور على 8 أحرف على الأقل، حرف، رقم، ورمز خاص"
                            />
                            <p className="text-xs text-text-muted text-right">8 أحرف على الأقل + حرف + رقم + رمز خاص (@$!%*#?&)</p>
                        </div>
                        <div className="space-y-2">
                            <label className="block text-right text-sm font-bold text-text-muted">البريد الإلكتروني {formData.role !== 'Worker' && <span className="text-red-400">*</span>}</label>
                            <input
                                required={formData.role !== 'Worker'}
                                type="email"
                                value={formData.email}
                                onChange={(e) => setFormData({...formData, email: e.target.value})}
                                className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary"
                                placeholder={formData.role !== 'Worker' ? "admin@municipality.ps" : "اختياري"}
                            />
                        </div>
                        <div className="space-y-2">
                             <label className="block text-right text-sm font-bold text-text-muted">القسم / الدائرة</label>
                             <input
                                 type="text"
                                 value={formData.department}
                                 onChange={(e) => setFormData({...formData, department: e.target.value})}
                                 className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary"
                                 placeholder="مثال: قسم الصحة والبيئة"
                             />
                        </div>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6 pt-6 border-t border-primary/5">
                        <div className="space-y-2">
                            <label className="block text-right text-sm font-bold text-text-muted">الدور الوظيفي</label>
                            <select
                                value={formData.role}
                                onChange={(e) => setFormData({...formData, role: e.target.value as UserRole})}
                                className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary [&>option]:text-black"
                            >
                                <option value="Worker">عامل</option>
                                <option value="Supervisor">مشرف</option>
                                <option value="Admin">مدير نظام</option>
                            </select>
                        </div>

                        <div className="space-y-2">
                            <label className="block text-right text-sm font-bold text-text-muted">البلدية</label>
                            <select
                                required
                                value={formData.municipalityId}
                                onChange={(e) => setFormData({...formData, municipalityId: e.target.value})}
                                className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary [&>option]:text-black"
                            >
                                {municipalities.map(m => (
                                    <option key={m.municipalityId} value={m.municipalityId}>{m.name}</option>
                                ))}
                            </select>
                        </div>

                        {formData.role === "Worker" && (
                            <>
                                <div className="space-y-2">
                                    <label className="block text-right text-sm font-bold text-text-muted">نوع العمل</label>
                                    <select
                                        value={formData.workerType}
                                        onChange={(e) => setFormData({...formData, workerType: e.target.value as WorkerType})}
                                        className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary [&>option]:text-black"
                                    >
                                        <option value="Sanitation">صحة/نظافة</option>
                                        <option value="PublicWorks">أشغال</option>
                                        <option value="Agriculture">زراعة</option>
                                        <option value="Maintenance">صيانة</option>
                                    </select>
                                </div>

                                <div className="space-y-2">
                                    <label className="block text-right text-sm font-bold text-text-muted">المشرف المسؤول</label>
                                    <select
                                        value={formData.supervisorId}
                                        onChange={(e) => setFormData({...formData, supervisorId: e.target.value})}
                                        className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary [&>option]:text-black"
                                    >
                                        <option value="">بدون مشرف</option>
                                        {supervisors.map(s => (
                                            <option key={s.userId} value={s.userId}>{s.fullName}</option>
                                        ))}
                                    </select>
                                </div>
                            </>
                        )}
                    </div>
                    
                    {/* Zones Assignment Multi-Select */}
                    <div className="pt-6 border-t border-white/5">
                        <label className="block text-right text-sm font-bold text-text-muted mb-3 flex items-center justify-end gap-2">
                             المناطق المسندة
                             <MapPin size={16} />
                        </label>
                        <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 max-h-48 overflow-y-auto p-2 border border-primary/10 rounded-xl bg-primary/5">
                            {zones.map(zone => (
                                 <label key={zone.zoneId} className={`flex items-center justify-end gap-3 p-3 rounded-lg border cursor-pointer transition-all ${
                                    formData.zoneIds.includes(zone.zoneId) 
                                      ? "bg-primary/20 border-primary/50" 
                                      : "bg-background-paper border-primary/5 hover:bg-primary/5"
                                }`}>
                                    <span className="text-sm font-medium text-text-primary">{zone.zoneName}</span>
                                    <div className={`w-5 h-5 rounded border flex items-center justify-center ${
                                        formData.zoneIds.includes(zone.zoneId) ? "bg-primary border-primary" : "border-primary/20"
                                    }`}>
                                        {formData.zoneIds.includes(zone.zoneId) && <CheckCircle size={14} className="text-white" />}
                                    </div>
                                    <input 
                                        type="checkbox" 
                                        className="hidden" 
                                        checked={formData.zoneIds.includes(zone.zoneId)}
                                        onChange={(e) => {
                                            if (e.target.checked) {
                                                setFormData({...formData, zoneIds: [...formData.zoneIds, zone.zoneId]});
                                            } else {
                                                setFormData({...formData, zoneIds: formData.zoneIds.filter(id => id !== zone.zoneId)});
                                            }
                                        }}
                                    />
                                </label>
                            ))}
                        </div>
                        {zones.length === 0 && <p className="text-right text-xs text-text-secondary mt-2">لا توجد مناطق متاحة في هذه البلدية.</p>}
                    </div>

                    <div className="flex gap-4 pt-8 border-t border-primary/5">
                        <button
                            type="button"
                            onClick={() => setShowUserModal(false)}
                            className="flex-1 h-12 rounded-xl border border-primary/10 text-text-primary hover:bg-primary/5 transition-all font-bold"
                        >
                            إلغاء
                        </button>
                        <button
                            type="submit"
                            disabled={actionLoading === "user-form"}
                            className="flex-1 h-12 rounded-xl bg-primary hover:bg-primary-dark text-white transition-all font-bold shadow-lg shadow-primary/20 disabled:opacity-50"
                        >
                            {actionLoading === "user-form" ? "جاري الحفظ..." : (isEditMode ? "حفظ التعديلات" : "إضافة المستخدم")}
                        </button>
                    </div>
                </form>
            </GlassCard>
            </div>
        )}

        {/* Device Management Modal */}
        {deviceModalUser && (
            <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 z-50 animate-fade-in">
                <GlassCard variant="panel" className="w-full max-w-md !bg-background-paper !border-primary/10 shadow-xl">
                    <div className="flex items-center justify-between mb-6 border-b border-primary/5 pb-4">
                        <div className="flex items-center gap-2">
                             <h3 className="text-xl font-bold text-text-primary text-right">إدارة الجهاز</h3>
                             <Smartphone size={20} className="text-primary" />
                        </div>
                        <button onClick={() => setDeviceModalUser(null)} className="p-2 hover:bg-primary/5 rounded-full transition-colors text-text-primary">
                            <X size={20} />
                        </button>
                    </div>

                    <div className="space-y-6">
                        <div className="text-center">
                            <div className="w-16 h-16 bg-primary/5 rounded-full flex items-center justify-center mx-auto mb-3 border border-primary/10">
                                <Smartphone size={32} className={deviceModalUser.deviceId ? "text-secondary" : "text-text-muted"} />
                            </div>
                            <h4 className="text-lg font-bold text-text-primary">{deviceModalUser.fullName}</h4>
                            <p className="text-sm text-text-secondary dir-ltr font-mono mt-1">
                                {deviceModalUser.deviceId || "لا يوجد جهاز مرتبط"}
                            </p>
                        </div>

                        {deviceAction === 'view' ? (
                            <div className="grid grid-cols-2 gap-4">
                                <button 
                                    onClick={() => handleUnbindDevice(deviceModalUser.userId)}
                                    disabled={!deviceModalUser.deviceId}
                                    className="h-12 rounded-xl border border-red-500/30 text-red-400 hover:bg-red-500/10 transition-all font-bold disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
                                >
                                    <UserX size={18} />
                                    فك الارتباط
                                </button>
                                <button 
                                    onClick={() => setDeviceAction('edit')}
                                    className="h-12 rounded-xl bg-primary hover:bg-primary-dark text-white transition-all font-bold shadow-lg shadow-primary/20 flex items-center justify-center gap-2"
                                >
                                    <Edit2 size={18} />
                                    تغيير المعرف
                                </button>
                            </div>
                        ) : (
                            <form onSubmit={handleUpdateDevice} className="space-y-4 animate-fade-in">
                                <div className="space-y-2 text-right">
                                    <label className="text-sm font-bold text-text-muted">معرف الجهاز الجديد (Device ID)</label>
                                    <input 
                                        type="text" 
                                        required
                                        value={targetDeviceId} 
                                        onChange={e => setTargetDeviceId(e.target.value)} 
                                        className="glass-input w-full h-11 text-center font-mono !bg-primary/5 text-text-primary"
                                        placeholder="Enter new Device ID"
                                    />
                                </div>
                                <div className="flex gap-4">
                                     <button 
                                        type="button"
                                        onClick={() => setDeviceAction('view')}
                                        className="flex-1 h-11 rounded-xl border border-primary/10 text-text-primary hover:bg-primary/5 transition-all font-bold"
                                    >
                                        إلغاء
                                    </button>
                                    <button 
                                        type="submit"
                                        disabled={!!actionLoading}
                                        className="flex-1 h-11 rounded-xl bg-primary hover:bg-primary-dark text-white transition-all font-bold shadow-lg shadow-primary/20 flex items-center justify-center gap-2"
                                    >
                                        {actionLoading ? <Loader2 size={16} className="animate-spin" /> : <Save size={16} />}
                                        حفظ
                                    </button>
                                </div>
                            </form>
                        )}
                    </div>
                </GlassCard>
            </div>
        )}

    </div>
  );
}
