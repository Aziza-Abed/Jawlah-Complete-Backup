import { useEffect, useState } from "react";
import { Users, Plus, Edit2, Trash2, Search, UserPlus, UserMinus, Shield } from "lucide-react";
import { STORAGE_KEYS } from "../constants/storageKeys";
import { getTeams, createTeam, updateTeam, deleteTeam, getTeamMembers, addWorkerToTeam, removeWorkerFromTeam, type Team, type TeamMember, type CreateTeamRequest, type UpdateTeamRequest } from "../api/teams";
import { getDepartments, type Department } from "../api/departments";
import { getUsers, type User } from "../api/users";
import { useConfirm } from "../components/common/ConfirmDialog";

export default function AdminTeams() {
  const [confirm, ConfirmDialog] = useConfirm();
  const [teams, setTeams] = useState<Team[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [workers, setWorkers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [showModal, setShowModal] = useState(false);
  const [showMembersModal, setShowMembersModal] = useState(false);
  const [editingTeam, setEditingTeam] = useState<Team | null>(null);
  const [selectedTeam, setSelectedTeam] = useState<Team | null>(null);
  const [teamMembers, setTeamMembers] = useState<TeamMember[]>([]);
  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  // Get user role for permission checking
  const getUserRole = (): string => {
    try {
      const userStr = localStorage.getItem(STORAGE_KEYS.USER);
      if (userStr) {
        const user = JSON.parse(userStr);
        return user.role?.toLowerCase() || '';
      }
    } catch (err) {}
    return '';
  };
  const isAdmin = getUserRole() === 'admin';

  const [formData, setFormData] = useState({
    name: "",
    code: "",
    description: "",
    departmentId: 0,
    teamLeaderId: undefined as number | undefined,
    maxMembers: 20,
    isActive: true,
  });

  useEffect(() => {
    fetchData();
  }, []);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [teamsData, departmentsData, workersData] = await Promise.all([
        getTeams(false),
        getDepartments(false),
        getUsers(1, 500, "Worker"),
      ]);
      setTeams(teamsData);
      setDepartments(departmentsData);
      setWorkers(workersData.items || []);
    } catch (error) {
      console.error("Failed to fetch data", error);
      setErrorMessage("فشل في تحميل البيانات");
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    try {
      if (!formData.name || !formData.code || !formData.departmentId) {
        setErrorMessage("يرجى ملء جميع الحقول المطلوبة");
        return;
      }

      const request: CreateTeamRequest = {
        name: formData.name,
        code: formData.code.toUpperCase(),
        description: formData.description || undefined,
        departmentId: formData.departmentId,
        teamLeaderId: formData.teamLeaderId,
        maxMembers: formData.maxMembers,
        isActive: formData.isActive,
      };

      await createTeam(request);
      setSuccessMessage("تم إنشاء الفريق بنجاح");
      setShowModal(false);
      resetForm();
      fetchData();
      setTimeout(() => setSuccessMessage(""), 3000);
    } catch (error) {
      const axiosErr = error as { response?: { data?: { message?: string } } };
      setErrorMessage(axiosErr.response?.data?.message || "فشل في إنشاء الفريق");
      setTimeout(() => setErrorMessage(""), 5000);
    }
  };

  const handleUpdate = async () => {
    try {
      if (!editingTeam || !formData.name || !formData.code) {
        setErrorMessage("يرجى ملء جميع الحقول المطلوبة");
        return;
      }

      const request: UpdateTeamRequest = {
        name: formData.name,
        code: formData.code.toUpperCase(),
        description: formData.description || undefined,
        teamLeaderId: formData.teamLeaderId,
        maxMembers: formData.maxMembers,
        isActive: formData.isActive,
      };

      await updateTeam(editingTeam.teamId, request);
      setSuccessMessage("تم تحديث الفريق بنجاح");
      setShowModal(false);
      resetForm();
      fetchData();
      setTimeout(() => setSuccessMessage(""), 3000);
    } catch (error) {
      const axiosErr = error as { response?: { data?: { message?: string } } };
      setErrorMessage(axiosErr.response?.data?.message || "فشل في تحديث الفريق");
      setTimeout(() => setErrorMessage(""), 5000);
    }
  };

  const handleDelete = async (team: Team) => {
    if (!await confirm(`هل أنت متأكد من حذف الفريق "${team.name}"؟`)) {
      return;
    }

    try {
      await deleteTeam(team.teamId);
      setSuccessMessage("تم حذف الفريق بنجاح");
      fetchData();
      setTimeout(() => setSuccessMessage(""), 3000);
    } catch (error) {
      const axiosErr = error as { response?: { data?: { message?: string } } };
      setErrorMessage(axiosErr.response?.data?.message || "فشل في حذف الفريق");
      setTimeout(() => setErrorMessage(""), 5000);
    }
  };

  const openEditModal = (team: Team) => {
    setEditingTeam(team);
    setFormData({
      name: team.name,
      code: team.code,
      description: team.description || "",
      departmentId: team.departmentId,
      teamLeaderId: team.teamLeaderId,
      maxMembers: team.maxMembers,
      isActive: team.isActive,
    });
    setShowModal(true);
  };

  const openMembersModal = async (team: Team) => {
    try {
      setSelectedTeam(team);
      const members = await getTeamMembers(team.teamId);
      setTeamMembers(members);
      setShowMembersModal(true);
    } catch (error) {
      setErrorMessage("فشل في تحميل أعضاء الفريق");
    }
  };

  const handleAddWorker = async (workerId: number) => {
    if (!selectedTeam) return;

    if (!await confirm("هل أنت متأكد من إضافة هذا العامل إلى الفريق؟")) {
      return;
    }

    try {
      await addWorkerToTeam(selectedTeam.teamId, workerId);
      setSuccessMessage("تم إضافة العامل بنجاح");
      const members = await getTeamMembers(selectedTeam.teamId);
      setTeamMembers(members);
      fetchData();
      setTimeout(() => setSuccessMessage(""), 3000);
    } catch (error) {
      const axiosErr = error as { response?: { data?: { message?: string } } };
      setErrorMessage(axiosErr.response?.data?.message || "فشل في إضافة العامل");
      setTimeout(() => setErrorMessage(""), 5000);
    }
  };

  const handleRemoveWorker = async (workerId: number) => {
    if (!selectedTeam) return;

    if (!await confirm("هل أنت متأكد من إزالة هذا العامل من الفريق؟")) {
      return;
    }

    try {
      await removeWorkerFromTeam(selectedTeam.teamId, workerId);
      setSuccessMessage("تم إزالة العامل بنجاح");
      const members = await getTeamMembers(selectedTeam.teamId);
      setTeamMembers(members);
      fetchData();
      setTimeout(() => setSuccessMessage(""), 3000);
    } catch (error) {
      const axiosErr = error as { response?: { data?: { message?: string } } };
      setErrorMessage(axiosErr.response?.data?.message || "فشل في إزالة العامل");
      setTimeout(() => setErrorMessage(""), 5000);
    }
  };

  const resetForm = () => {
    setFormData({
      name: "",
      code: "",
      description: "",
      departmentId: 0,
      teamLeaderId: undefined,
      maxMembers: 20,
      isActive: true,
    });
    setEditingTeam(null);
  };

  const filteredTeams = teams.filter((team) =>
    team.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    team.code.toLowerCase().includes(searchTerm.toLowerCase()) ||
    team.departmentName.toLowerCase().includes(searchTerm.toLowerCase())
  );

  // Get workers from selected department for team leader dropdown
  const departmentWorkers = workers.filter(
    (w) => w.departmentId === formData.departmentId && w.role === "Worker"
  );

  // Get available workers for adding to team (same department, not in any team)
  const availableWorkers = workers.filter(
    (w) =>
      w.departmentId === selectedTeam?.departmentId &&
      w.role === "Worker" &&
      !w.teamId
  );

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#7895B2]/30 border-t-[#7895B2] rounded-full animate-spin"></div>
          <p className="text-[#6B7280] font-medium">جاري تحميل الفرق...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1200px] mx-auto space-y-6">
          {/* Header */}
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="p-2.5 rounded-[12px] bg-[#7895B2]/20">
                <Users size={22} className="text-[#7895B2]" />
              </div>
              <div>
                <h1 className="font-black text-[28px] text-[#2F2F2F] tracking-tight">
                  إدارة الفرق
                </h1>
                <p className="text-[13px] text-[#6B7280]">إدارة فرق العمل وأعضائها</p>
              </div>
            </div>

            {isAdmin && (
              <button
                onClick={() => {
                  resetForm();
                  setShowModal(true);
                }}
                className="flex items-center gap-2 bg-[#7895B2] hover:bg-[#647e99] text-white px-4 py-3 rounded-[12px] transition-all font-semibold text-[14px]"
              >
                <Plus size={18} />
                <span>إضافة فريق جديد</span>
              </button>
            )}
          </div>

          {/* Success Message */}
          {successMessage && (
            <div className="bg-[#8FA36A]/20 border border-[#8FA36A]/30 text-[#8FA36A] px-4 py-3 rounded-[12px] font-semibold text-[14px]">
              {successMessage}
            </div>
          )}

          {/* Error Message */}
          {errorMessage && (
            <div className="bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-[12px] font-semibold text-[14px]">
              {errorMessage}
            </div>
          )}

          {/* Search */}
          <div className="bg-white rounded-[16px] p-4 shadow-[0_4px_20px_rgba(0,0,0,0.04)]">
            <div className="relative">
              <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-[#9ca3af]" size={20} />
              <input
                type="text"
                placeholder="بحث عن فريق (الاسم، الرمز، القسم)..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full pr-12 pl-4 py-3 border border-[#E5E7EB] rounded-[10px] text-right focus:outline-none focus:ring-2 focus:ring-[#7895B2]/20 focus:border-[#7895B2]"
              />
            </div>
          </div>

          {/* Teams List */}
          <div className="bg-white rounded-[16px] shadow-[0_4px_20px_rgba(0,0,0,0.04)] overflow-hidden">
            {filteredTeams.length === 0 ? (
              <div className="text-center py-16">
                <Users size={48} className="mx-auto text-[#9ca3af] mb-4" />
                <p className="text-[#6B7280] text-[16px] font-semibold">لا توجد فرق</p>
                <p className="text-[#9ca3af] text-[14px] mt-1">
                  {searchTerm ? "لم يتم العثور على نتائج مطابقة" : "ابدأ بإضافة فريق جديد"}
                </p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead className="bg-[#F9FAFB] border-b border-[#E5E7EB]">
                    <tr>
                      <th className="px-6 py-4 text-right text-[13px] font-bold text-[#6B7280] uppercase">
                        الرمز
                      </th>
                      <th className="px-6 py-4 text-right text-[13px] font-bold text-[#6B7280] uppercase">
                        اسم الفريق
                      </th>
                      <th className="px-6 py-4 text-right text-[13px] font-bold text-[#6B7280] uppercase">
                        القسم
                      </th>
                      <th className="px-6 py-4 text-right text-[13px] font-bold text-[#6B7280] uppercase">
                        الأعضاء
                      </th>
                      <th className="px-6 py-4 text-right text-[13px] font-bold text-[#6B7280] uppercase">
                        قائد الفريق
                      </th>
                      <th className="px-6 py-4 text-right text-[13px] font-bold text-[#6B7280] uppercase">
                        الحالة
                      </th>
                      <th className="px-6 py-4 text-center text-[13px] font-bold text-[#6B7280] uppercase">
                        الإجراءات
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-[#E5E7EB]">
                    {filteredTeams.map((team) => (
                      <tr key={team.teamId} className="hover:bg-[#F9FAFB] transition-colors">
                        <td className="px-6 py-4">
                          <span className="inline-block px-2 py-1 bg-[#7895B2]/10 text-[#7895B2] rounded text-[12px] font-bold">
                            {team.code}
                          </span>
                        </td>
                        <td className="px-6 py-4">
                          <div className="font-semibold text-[#2F2F2F]">{team.name}</div>
                          {team.description && (
                            <div className="text-[13px] text-[#6B7280] mt-1">{team.description}</div>
                          )}
                        </td>
                        <td className="px-6 py-4 text-[#6B7280]">{team.departmentName}</td>
                        <td className="px-6 py-4">
                          <button
                            onClick={() => openMembersModal(team)}
                            className="text-[#7895B2] hover:text-[#647e99] font-semibold text-[14px]"
                          >
                            {team.membersCount} / {team.maxMembers}
                          </button>
                        </td>
                        <td className="px-6 py-4">
                          {team.teamLeaderName ? (
                            <div className="flex items-center gap-1">
                              <Shield size={14} className="text-[#8FA36A]" />
                              <span className="text-[14px] text-[#2F2F2F]">{team.teamLeaderName}</span>
                            </div>
                          ) : (
                            <span className="text-[#9ca3af] text-[13px]">غير محدد</span>
                          )}
                        </td>
                        <td className="px-6 py-4">
                          <span
                            className={`inline-block px-2 py-1 rounded text-[12px] font-bold ${
                              team.isActive
                                ? "bg-[#8FA36A]/20 text-[#8FA36A]"
                                : "bg-[#EF4444]/20 text-[#EF4444]"
                            }`}
                          >
                            {team.isActive ? "نشط" : "غير نشط"}
                          </span>
                        </td>
                        <td className="px-6 py-4">
                          <div className="flex items-center justify-center gap-2">
                            {isAdmin ? (
                              <>
                                <button
                                  onClick={() => openEditModal(team)}
                                  className="p-2 hover:bg-[#7895B2]/10 text-[#7895B2] rounded-[8px] transition-colors"
                                  title="تعديل"
                                >
                                  <Edit2 size={16} />
                                </button>
                                <button
                                  onClick={() => handleDelete(team)}
                                  className="p-2 hover:bg-red-50 text-red-600 rounded-[8px] transition-colors"
                                  title="حذف"
                                >
                                  <Trash2 size={16} />
                                </button>
                              </>
                            ) : (
                              <span className="text-[13px] text-[#9ca3af]">للعرض فقط</span>
                            )}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Create/Edit Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-[16px] w-full max-w-[600px] max-h-[90vh] overflow-y-auto">
            <div className="p-6 border-b border-[#E5E7EB]">
              <h2 className="text-[20px] font-bold text-[#2F2F2F]">
                {editingTeam ? "تعديل فريق" : "إضافة فريق جديد"}
              </h2>
            </div>

            <div className="p-6 space-y-4">
              <div>
                <label className="block text-[14px] font-semibold text-[#2F2F2F] mb-2">
                  اسم الفريق *
                </label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="w-full px-4 py-2 border border-[#E5E7EB] rounded-[8px] text-right focus:outline-none focus:ring-2 focus:ring-[#7895B2]/20"
                  placeholder="فريق النظافة 1"
                />
              </div>

              <div>
                <label className="block text-[14px] font-semibold text-[#2F2F2F] mb-2">
                  رمز الفريق *
                </label>
                <input
                  type="text"
                  value={formData.code}
                  onChange={(e) => setFormData({ ...formData, code: e.target.value.toUpperCase() })}
                  className="w-full px-4 py-2 border border-[#E5E7EB] rounded-[8px] text-right focus:outline-none focus:ring-2 focus:ring-[#7895B2]/20 uppercase"
                  placeholder="CLEAN-T1"
                />
              </div>

              {!editingTeam && (
                <div>
                  <label className="block text-[14px] font-semibold text-[#2F2F2F] mb-2">
                    القسم *
                  </label>
                  <select
                    value={formData.departmentId}
                    onChange={(e) =>
                      setFormData({ ...formData, departmentId: parseInt(e.target.value), teamLeaderId: undefined })
                    }
                    className="w-full px-4 py-2 border border-[#E5E7EB] rounded-[8px] text-right focus:outline-none focus:ring-2 focus:ring-[#7895B2]/20"
                  >
                    <option value={0}>اختر القسم</option>
                    {departments.map((dept) => (
                      <option key={dept.departmentId} value={dept.departmentId}>
                        {dept.name}
                      </option>
                    ))}
                  </select>
                </div>
              )}

              <div>
                <label className="block text-[14px] font-semibold text-[#2F2F2F] mb-2">
                  الوصف
                </label>
                <textarea
                  value={formData.description}
                  onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                  className="w-full px-4 py-2 border border-[#E5E7EB] rounded-[8px] text-right focus:outline-none focus:ring-2 focus:ring-[#7895B2]/20 resize-none"
                  rows={3}
                  placeholder="وصف مهام ومسؤوليات الفريق..."
                />
              </div>

              <div>
                <label className="block text-[14px] font-semibold text-[#2F2F2F] mb-2">
                  قائد الفريق
                </label>
                <select
                  value={formData.teamLeaderId || ""}
                  onChange={(e) =>
                    setFormData({ ...formData, teamLeaderId: e.target.value ? parseInt(e.target.value) : undefined })
                  }
                  className="w-full px-4 py-2 border border-[#E5E7EB] rounded-[8px] text-right focus:outline-none focus:ring-2 focus:ring-[#7895B2]/20"
                  disabled={!formData.departmentId}
                >
                  <option value="">لا يوجد</option>
                  {departmentWorkers.map((worker) => (
                    <option key={worker.userId} value={worker.userId}>
                      {worker.fullName} ({worker.username})
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="block text-[14px] font-semibold text-[#2F2F2F] mb-2">
                  الحد الأقصى للأعضاء *
                </label>
                <input
                  type="number"
                  value={formData.maxMembers}
                  onChange={(e) => setFormData({ ...formData, maxMembers: parseInt(e.target.value) || 1 })}
                  className="w-full px-4 py-2 border border-[#E5E7EB] rounded-[8px] text-right focus:outline-none focus:ring-2 focus:ring-[#7895B2]/20"
                  min="1"
                  max="100"
                />
              </div>

              <div className="flex items-center gap-3">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={formData.isActive}
                  onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                  className="w-4 h-4 text-[#7895B2] border-gray-300 rounded focus:ring-[#7895B2]"
                />
                <label htmlFor="isActive" className="text-[14px] font-semibold text-[#2F2F2F]">
                  فريق نشط
                </label>
              </div>
            </div>

            <div className="p-6 border-t border-[#E5E7EB] flex gap-3">
              <button
                onClick={() => {
                  setShowModal(false);
                  resetForm();
                }}
                className="flex-1 px-4 py-2 border border-[#E5E7EB] text-[#6B7280] rounded-[8px] font-semibold hover:bg-[#F9FAFB]"
              >
                إلغاء
              </button>
              <button
                onClick={editingTeam ? handleUpdate : handleCreate}
                className="flex-1 px-4 py-2 bg-[#7895B2] hover:bg-[#647e99] text-white rounded-[8px] font-semibold"
              >
                {editingTeam ? "حفظ التغييرات" : "إضافة الفريق"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Members Modal */}
      {showMembersModal && selectedTeam && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-[16px] w-full max-w-[800px] max-h-[90vh] overflow-y-auto">
            <div className="p-6 border-b border-[#E5E7EB]">
              <h2 className="text-[20px] font-bold text-[#2F2F2F]">
                أعضاء فريق {selectedTeam.name}
              </h2>
              <p className="text-[13px] text-[#6B7280] mt-1">
                {teamMembers.length} / {selectedTeam.maxMembers} أعضاء
              </p>
            </div>

            <div className="p-6 space-y-6">
              {/* Current Members */}
              <div>
                <h3 className="text-[16px] font-bold text-[#2F2F2F] mb-3">الأعضاء الحاليون</h3>
                {teamMembers.length === 0 ? (
                  <p className="text-[#9ca3af] text-[14px]">لا يوجد أعضاء في الفريق</p>
                ) : (
                  <div className="space-y-3">
                    {teamMembers.map((member) => (
                      <div
                        key={member.userId}
                        className="flex items-center justify-between p-3 bg-[#F9FAFB] rounded-[8px]"
                      >
                        <div className="flex items-center gap-2">
                          <div className="font-semibold text-[#2F2F2F]">{member.fullName}</div>
                          <div className="text-[13px] text-[#6B7280]">({member.username})</div>
                          {member.isTeamLeader && (
                            <span className="px-2 py-0.5 bg-[#8FA36A]/20 text-[#8FA36A] rounded text-[11px] font-bold flex items-center gap-1">
                              <Shield size={12} />
                              قائد
                            </span>
                          )}
                        </div>
                        {isAdmin && (
                          <button
                            onClick={() => handleRemoveWorker(member.userId)}
                            className="p-2 hover:bg-red-50 text-red-600 rounded-[8px] transition-colors"
                            title="إزالة من الفريق"
                          >
                            <UserMinus size={16} />
                          </button>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </div>

              {/* Available Workers */}
              {teamMembers.length < selectedTeam.maxMembers && (
                <div>
                  <h3 className="text-[16px] font-bold text-[#2F2F2F] mb-3">العمال المتاحون للإضافة</h3>
                  {availableWorkers.length === 0 ? (
                    <p className="text-[#9ca3af] text-[14px]">لا يوجد عمال متاحون من نفس القسم</p>
                  ) : (
                    <div className="space-y-3 max-h-[300px] overflow-y-auto">
                      {availableWorkers.map((worker) => (
                        <div
                          key={worker.userId}
                          className="flex items-center justify-between p-3 bg-white border border-[#E5E7EB] rounded-[8px]"
                        >
                          <div className="flex items-center gap-2">
                            <div className="font-semibold text-[#2F2F2F]">{worker.fullName}</div>
                            <div className="text-[13px] text-[#6B7280]">({worker.username})</div>
                          </div>
                          {isAdmin && (
                            <button
                              onClick={() => handleAddWorker(worker.userId)}
                              className="p-2 hover:bg-[#7895B2]/10 text-[#7895B2] rounded-[8px] transition-colors"
                              title="إضافة إلى الفريق"
                            >
                              <UserPlus size={16} />
                            </button>
                          )}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>

            <div className="p-6 border-t border-[#E5E7EB]">
              <button
                onClick={() => {
                  setShowMembersModal(false);
                  setSelectedTeam(null);
                  setTeamMembers([]);
                }}
                className="w-full px-4 py-2 bg-[#7895B2] hover:bg-[#647e99] text-white rounded-[8px] font-semibold"
              >
                إغلاق
              </button>
            </div>
          </div>
        </div>
      )}

      {ConfirmDialog}
    </div>
  );
}
