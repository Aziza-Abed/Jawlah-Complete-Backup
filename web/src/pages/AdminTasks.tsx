import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { usePageTitle } from "../hooks/usePageTitle";
import { getTasks, deleteTask, updateTask, reassignTask } from "../api/tasks";
import { getTaskTemplates, type TaskTemplate } from "../api/templates";
import { getZones } from "../api/zones";
import { getWorkers } from "../api/users";
import type { TaskResponse, TaskStatus, TaskPriority, TaskType, UpdateTaskRequest } from "../types/task";
import type { ZoneResponse } from "../types/zone";
import type { UserResponse } from "../types/user";
import {
  ClipboardList,
  Search,
  Trash2,
  User,
  MapPin,
  Calendar,
  Loader2,
  Repeat,
  X,
  Save,
  Edit3
} from "lucide-react";
import { useConfirm } from "../components/common/ConfirmDialog";

export default function AdminTasks() {
  usePageTitle("إدارة المهام");
  const navigate = useNavigate();
  const [confirm, ConfirmDialog] = useConfirm();
  const [tasks, setTasks] = useState<TaskResponse[]>([]);
  const [zones, setZones] = useState<ZoneResponse[]>([]);
  const [workers, setWorkers] = useState<UserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<TaskStatus | "all">("all");
  const [priorityFilter, setPriorityFilter] = useState<TaskPriority | "all">("all");
  const [zoneFilter, setZoneFilter] = useState<string>("all");
  // Templates (read-only, for display count)
  const [templates, setTemplates] = useState<TaskTemplate[]>([]);

  // Edit Task State
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingTask, setEditingTask] = useState<TaskResponse | null>(null);
  const [editForm, setEditForm] = useState<{
    title: string;
    description: string;
    priority: TaskPriority;
    taskType: TaskType | '';
    requiresPhotoProof: boolean;
    estimatedDurationMinutes: string;
    assignedToUserId: number;
    zoneId: number | null;
    dueDate: string;
    locationDescription: string;
  }>({ title: '', description: '', priority: 'Medium', taskType: '', requiresPhotoProof: true, estimatedDurationMinutes: '', assignedToUserId: 0, zoneId: null, dueDate: '', locationDescription: '' });
  const [editLoading, setEditLoading] = useState(false);
  const [formError, setFormError] = useState("");
  const [pageError, setPageError] = useState("");
  const [touched, setTouched] = useState<Record<string, boolean>>({});

  useEffect(() => {
    fetchInitialData();
  }, []);

  const fetchInitialData = async () => {
    try {
      setLoading(true);
      const [tasksData, zonesData, templatesData, workersData] = await Promise.all([
        getTasks(),
        getZones(),
        getTaskTemplates(),
        getWorkers()
      ]);
      setTasks(tasksData);
      setZones(zonesData);
      setTemplates(templatesData);
      setWorkers(workersData);
    } catch (err) {
      console.error("Failed to fetch data", err);
      setPageError("فشل في تحميل البيانات. يرجى تحديث الصفحة.");
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteTask = async (taskId: number) => {
    if (!await confirm("هل أنت متأكد من حذف (إلغاء) هذه المهمة نهائياً؟")) return;
    try {
      await deleteTask(taskId);
      setTasks(tasks.filter(t => t.taskId !== taskId));
    } catch (err) {
      const msg = (err as any)?.response?.data?.message || 'فشل حذف المهمة';
      setPageError(msg);
    }
  };

  const openEditModal = (task: TaskResponse) => {
    setFormError("");
    setTouched({});
    setEditingTask(task);
    setEditForm({
      title: task.title,
      description: task.description || '',
      priority: task.priority,
      taskType: task.taskType || '',
      requiresPhotoProof: task.requiresPhotoProof,
      estimatedDurationMinutes: task.estimatedDurationMinutes?.toString() || '',
      assignedToUserId: task.assignedToUserId,
      zoneId: task.zoneId || null,
      dueDate: task.dueDate ? task.dueDate.slice(0, 10) : '',
      locationDescription: task.locationDescription || '',
    });
    setShowEditModal(true);
  };

  const handleEditTask = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingTask) return;

    setEditLoading(true);
    try {
      const updateData: UpdateTaskRequest = {
        title: editForm.title,
        description: editForm.description,
        priority: editForm.priority,
        taskType: editForm.taskType || undefined,
        requiresPhotoProof: editForm.requiresPhotoProof,
        estimatedDurationMinutes: editForm.estimatedDurationMinutes ? parseInt(editForm.estimatedDurationMinutes) : undefined,
        zoneId: editForm.zoneId || undefined,
        dueDate: editForm.dueDate || undefined,
        locationDescription: editForm.locationDescription || undefined,
      };

      const updatedTask = await updateTask(editingTask.taskId, updateData);

      if (editForm.assignedToUserId !== editingTask.assignedToUserId && editForm.assignedToUserId !== 0) {
        await reassignTask(editingTask.taskId, {
          newAssignedToUserId: editForm.assignedToUserId,
          reassignmentReason: 'تم إعادة التعيين بواسطة المشرف'
        });
      }

      setTasks(prev => prev.map(t =>
        t.taskId === editingTask.taskId
          ? { ...updatedTask, assignedToUserId: editForm.assignedToUserId, assignedToUserName: workers.find(w => w.userId === editForm.assignedToUserId)?.fullName || updatedTask.assignedToUserName }
          : t
      ));

      setShowEditModal(false);
      setEditingTask(null);
    } catch (err) {
      const msg = (err as any)?.response?.data?.message || (err as any)?.response?.data?.errors?.join(', ') || 'فشل تحديث المهمة';
      setFormError(msg);
    } finally {
      setEditLoading(false);
    }
  };

  const filteredTasks = tasks.filter(task => {
    const search = searchTerm.toLowerCase();
    const matchesSearch = (task.title?.toLowerCase() || "").includes(search) ||
                         (task.description?.toLowerCase() || "").includes(search);
    const matchesStatus = statusFilter === "all" || task.status === statusFilter;
    const matchesPriority = priorityFilter === "all" || task.priority === priorityFilter;
    const matchesZone = zoneFilter === "all" || (task.zoneId && task.zoneId.toString() === zoneFilter);

    return matchesSearch && matchesStatus && matchesPriority && matchesZone;
  });

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#7895B2]/30 border-t-[#7895B2] rounded-full animate-spin"></div>
          <p className="text-[#6B7280] font-medium">جاري تحميل المهام...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1200px] mx-auto space-y-6">
          {/* Header (Corrected Layout) */}
          <div className="flex flex-col md:flex-row items-center justify-between gap-6">
            <div className="flex items-center gap-3">
              <div className="p-3 rounded-[16px] bg-[#7895B2]/10 text-[#7895B2]">
                <ClipboardList size={28} />
              </div>
              <div className="text-right">
                <h1 className="font-black text-[28px] text-[#2F2F2F] tracking-tight">
                  إدارة العمليات
                </h1>
                <p className="text-[14px] font-bold text-[#AFAFAF] mt-1">متابعة وإسناد المهام الميدانية والروتينية</p>
              </div>
            </div>

            {/* Quick Link to Templates */}
            <button
              onClick={() => navigate('/task-templates')}
              className="px-6 py-2.5 rounded-[14px] text-[13px] font-black transition-all flex items-center gap-2 text-[#6B7280] hover:text-[#2F2F2F] hover:bg-white bg-white/50 backdrop-blur-sm border border-black/5 shadow-sm"
            >
              <Repeat size={14} />
              المهام المتكررة ({templates.length})
            </button>
          </div>

          {pageError && (
            <div className="bg-[#C86E5D]/10 border border-[#C86E5D]/30 rounded-[16px] p-4 text-right flex items-center justify-between gap-4">
              <button onClick={() => setPageError("")} className="text-[#6B7280] hover:text-[#2F2F2F] text-sm shrink-0">✕</button>
              <p className="text-[#C86E5D] font-semibold text-[14px]">{pageError}</p>
            </div>
          )}

          {/* Search and Filters */}
              <div className="bg-white rounded-[24px] p-5 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 flex flex-col xl:flex-row gap-5">
                <div className="flex-1 relative">
                  <input
                    type="text"
                    placeholder="بحث في المهام..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="w-full h-[52px] pr-12 pl-6 bg-[#F9F8F6] rounded-[18px] text-right text-[14px] font-black text-[#2F2F2F] placeholder:text-[#AFAFAF] border-0 outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all"
                  />
                  <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
                </div>

                <div className="flex flex-wrap gap-3 justify-end">
                  <select
                    value={zoneFilter}
                    onChange={(e) => setZoneFilter(e.target.value)}
                    className="h-[52px] w-full sm:w-[150px] px-4 bg-[#F9F8F6] rounded-[18px] text-right text-[13px] font-black text-[#2F2F2F] border-0 outline-none focus:ring-4 focus:ring-[#7895B2]/10 cursor-pointer appearance-none transition-all"
                  >
                    <option value="all">كل المناطق</option>
                    {zones.map(z => <option key={z.zoneId} value={z.zoneId}>{z.zoneName}</option>)}
                  </select>
                  <select
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value as TaskStatus | "all")}
                    className="h-[52px] w-full sm:w-[150px] px-4 bg-[#F9F8F6] rounded-[18px] text-right text-[13px] font-black text-[#2F2F2F] border-0 outline-none focus:ring-4 focus:ring-[#7895B2]/10 cursor-pointer appearance-none transition-all"
                  >
                    <option value="all">كل الحالات</option>
                    <option value="Pending">إنتظار</option>
                    <option value="InProgress">تنفيذ</option>
                    <option value="UnderReview">بانتظار اعتماد</option>
                    <option value="Completed">مكتمل</option>
                    <option value="Rejected">مرفوض</option>
                    <option value="Cancelled">ملغاة</option>
                  </select>
                  <select
                    value={priorityFilter}
                    onChange={(e) => setPriorityFilter(e.target.value as TaskPriority | "all")}
                    className="h-[52px] w-full sm:w-[150px] px-4 bg-[#F9F8F6] rounded-[18px] text-right text-[13px] font-black text-[#2F2F2F] border-0 outline-none focus:ring-4 focus:ring-[#7895B2]/10 cursor-pointer appearance-none transition-all"
                  >
                    <option value="all">الأولوية</option>
                    <option value="Urgent">حرجة</option>
                    <option value="High">عالية</option>
                    <option value="Medium">متوسطة</option>
                    <option value="Low">عادية</option>
                  </select>
                </div>
              </div>

              {/* Tasks List */}
              <div className="space-y-4">
                {filteredTasks.map((task) => (
                  <div
                    key={task.taskId}
                    className="bg-white rounded-[24px] p-6 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5 flex flex-col lg:flex-row gap-6 lg:items-center hover:border-[#7895B2]/30 hover:shadow-xl transition-all duration-300 group"
                  >
                    {/* Task Info Body (Now on the Right, Tabular Style) */}
                    <div className="flex-1 text-right order-1 grid grid-cols-1 xl:grid-cols-4 gap-6 items-center">
                      
                      {/* Task Identity (Far Right) */}
                      <div className="order-1 xl:order-1 xl:border-l border-black/5 xl:pl-6">
                        <h3 className="font-black text-[18px] text-[#2F2F2F] group-hover:text-[#7895B2] transition-colors leading-tight mb-1">{task.title}</h3>
                        <p className="text-[#AFAFAF] text-[13px] font-bold line-clamp-1 truncate">{task.description}</p>
                      </div>

                      {/* Worker */}
                      <div className="flex flex-col items-end gap-1 order-2 xl:order-2">
                          <span className="text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest">الموظف المكلف</span>
                          <div className="flex items-center gap-2">
                            {task.assignedToUserId === 0 && !task.isTeamTask ? (
                              <span className="font-black text-[12px] text-[#C86E5D] bg-[#C86E5D]/10 px-2 py-0.5 rounded-full border border-[#C86E5D]/20">غير مسند</span>
                            ) : (
                              <span className="font-black text-[13px] text-[#2F2F2F]">{task.assignedToUserName}</span>
                            )}
                              <User size={14} className="text-[#7895B2]" />
                          </div>
                      </div>

                      {/* Zone */}
                      <div className="flex flex-col items-end gap-1 order-3 xl:order-3">
                          <span className="text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest">نطاق العمل</span>
                          <div className="flex items-center gap-2">
                              <span className="font-black text-[13px] text-[#2F2F2F]">{task.zoneName || 'خارج المنطقة'}</span>
                              <MapPin size={14} className="text-[#8FA36A]" />
                          </div>
                      </div>

                      {/* Date (Far Left of content) */}
                      <div className="flex flex-col items-end gap-1 order-4 xl:order-4">
                          <span className="text-[10px] text-[#AFAFAF] font-black uppercase tracking-widest">تاريخ الإنشاء</span>
                          <div className="flex items-center gap-2">
                              <span className="font-black text-[13px] text-[#2F2F2F]">{task.createdAt ? new Date(task.createdAt).toLocaleDateString('ar-EG', { day: 'numeric', month: 'short', year: 'numeric' }) : '-'}</span>
                              <Calendar size={14} className="text-[#C86E5D]" />
                          </div>
                      </div>

                    </div>

                    {/* Actions Side (Buttons) (Now on the Left) */}
                    <div className="flex lg:flex-col items-center gap-2 lg:border-r border-black/5 lg:pr-6 justify-end order-2">
                      <button
                        onClick={() => navigate(`/tasks/${task.taskId}`)}
                        className="flex-1 lg:w-full px-6 py-2.5 bg-[#F9F8F6] hover:bg-[#7895B2] hover:text-white text-[#2F2F2F] rounded-xl transition-all font-black text-[12px] shadow-sm"
                      >
                        التفاصيل
                      </button>
                      <div className="flex gap-2 w-full">
                        <button
                          onClick={() => openEditModal(task)}
                          className="flex-1 p-2.5 bg-[#F9F8F6] text-[#7895B2] hover:bg-[#7895B2] hover:text-white rounded-xl transition-all shadow-sm flex items-center justify-center border border-black/5"
                        >
                          <Edit3 size={16} />
                        </button>
                        <button
                          onClick={() => handleDeleteTask(task.taskId)}
                          className="flex-1 p-2.5 bg-[#C86E5D]/5 text-[#C86E5D] hover:bg-[#C86E5D] hover:text-white rounded-xl transition-all shadow-sm flex items-center justify-center border border-black/5"
                        >
                          <Trash2 size={16} />
                        </button>
                      </div>
                    </div>
                  </div>
                ))}



                {filteredTasks.length === 0 && (
                  <div className="bg-white rounded-[16px] p-8 shadow-[0_4px_20px_rgba(0,0,0,0.04)] text-center">
                    <ClipboardList size={48} className="text-[#7895B2]/20 mx-auto mb-3" />
                    <p className="text-[#6B7280] text-[15px] font-medium">لا توجد مهام مطابقة</p>
                  </div>
                )}
              </div>
        </div>
      </div>

      {/* Edit Task Modal */}
      {showEditModal && editingTask && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-[16px] w-full max-w-lg p-6 shadow-xl">
            <div className="flex items-center justify-between mb-6">
              <h3 className="text-[18px] font-bold text-[#2F2F2F]">تعديل المهمة</h3>
              <button onClick={() => { setShowEditModal(false); setEditingTask(null); setFormError(""); setTouched({}); }} className="p-2 hover:bg-[#F3F1ED] rounded-full transition-colors text-[#6B7280]">
                <X size={20} />
              </button>
            </div>

            <form onSubmit={handleEditTask} className="space-y-4 max-h-[70vh] overflow-y-auto pr-1">
              {formError && (
                <div className="p-3 rounded-[10px] bg-red-50 border border-red-200 text-red-700 text-[13px] font-semibold text-right">
                  {formError}
                </div>
              )}
              <div className="space-y-1">
                <label className="block text-right text-[12px] font-semibold text-[#6B7280]">عنوان المهمة</label>
                <input
                  required
                  value={editForm.title}
                  onChange={e => setEditForm({...editForm, title: e.target.value})}
                  onBlur={() => setTouched(t => ({...t, editTitle: true}))}
                  className={`w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border outline-none focus:ring-2 focus:ring-[#7895B2]/30 ${touched.editTitle && !editForm.title ? 'border-red-400' : 'border-transparent'}`}
                />
                {touched.editTitle && !editForm.title && (
                  <p className="text-red-500 text-[11px] text-right">العنوان مطلوب</p>
                )}
              </div>

              <div className="space-y-3">
                <label className="block text-right text-[12px] font-semibold text-[#6B7280]">الوصف</label>
                <textarea
                  value={editForm.description}
                  onChange={e => setEditForm({...editForm, description: e.target.value})}
                  className="w-full h-24 px-4 py-3 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30 resize-none"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">الأولوية</label>
                  <select
                    value={editForm.priority}
                    onChange={e => setEditForm({...editForm, priority: e.target.value as TaskPriority})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  >
                    <option value="Low">منخفضة</option>
                    <option value="Medium">متوسطة</option>
                    <option value="High">عالية</option>
                    <option value="Urgent">عاجلة</option>
                  </select>
                </div>

                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">نوع المهمة</label>
                  <select
                    value={editForm.taskType}
                    onChange={e => setEditForm({...editForm, taskType: e.target.value as TaskType | ''})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  >
                    <option value="">غير محدد</option>
                    <option value="GarbageCollection">جمع القمامة</option>
                    <option value="StreetSweeping">كنس الشوارع</option>
                    <option value="ContainerMaintenance">صيانة الحاويات</option>
                    <option value="RepairMaintenance">صيانة وإصلاح</option>
                    <option value="PublicSpaceCleaning">تنظيف الأماكن العامة</option>
                    <option value="Inspection">تفتيش</option>
                    <option value="Other">أخرى</option>
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">العامل المكلف</label>
                  <select
                    value={editForm.assignedToUserId}
                    onChange={e => setEditForm({...editForm, assignedToUserId: parseInt(e.target.value)})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  >
                    <option value={0}>— مهمة جماعية (بدون تعيين) —</option>
                    {workers.map(w => (
                      <option key={w.userId} value={w.userId}>{w.fullName}</option>
                    ))}
                  </select>
                </div>

                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">المنطقة</label>
                  <select
                    value={editForm.zoneId || 0}
                    onChange={e => setEditForm({...editForm, zoneId: parseInt(e.target.value) || null})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  >
                    <option value={0}>بدون منطقة</option>
                    {zones.map(z => <option key={z.zoneId} value={z.zoneId}>{z.zoneName}</option>)}
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">تاريخ الاستحقاق</label>
                  <input
                    type="date"
                    value={editForm.dueDate}
                    onChange={e => setEditForm({...editForm, dueDate: e.target.value})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  />
                </div>

                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">المدة المتوقعة (دقيقة)</label>
                  <input
                    type="number"
                    min={1}
                    max={1440}
                    value={editForm.estimatedDurationMinutes}
                    onChange={e => setEditForm({...editForm, estimatedDurationMinutes: e.target.value})}
                    placeholder="60"
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  />
                </div>
              </div>

              <div className="space-y-3">
                <label className="block text-right text-[12px] font-semibold text-[#6B7280]">وصف الموقع</label>
                <input
                  type="text"
                  value={editForm.locationDescription}
                  onChange={e => setEditForm({...editForm, locationDescription: e.target.value})}
                  placeholder="مثال: بالقرب من ساحة البلدية"
                  className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                />
              </div>

              <div className="flex items-center justify-end gap-3 bg-[#F3F1ED] rounded-[12px] px-4 py-3">
                <span className="text-[12px] font-semibold text-[#6B7280]">يتطلب إثبات صورة</span>
                <input
                  type="checkbox"
                  checked={editForm.requiresPhotoProof}
                  onChange={e => setEditForm({...editForm, requiresPhotoProof: e.target.checked})}
                  className="w-5 h-5 accent-[#7895B2] cursor-pointer"
                />
              </div>

              <div className="flex gap-3 pt-4 border-t border-[#F3F1ED]">
                <button
                  type="button"
                  onClick={() => { setShowEditModal(false); setEditingTask(null); setFormError(""); setTouched({}); }}
                  className="flex-1 h-[46px] rounded-[12px] border border-[#E5E7EB] text-[#2F2F2F] hover:bg-[#F3F1ED] transition-all font-semibold text-[14px]"
                >
                  إلغاء
                </button>
                <button
                  type="submit"
                  disabled={editLoading}
                  className="flex-1 h-[46px] rounded-[12px] bg-[#7895B2] hover:bg-[#6B87A3] text-white transition-all font-semibold text-[14px] flex items-center justify-center gap-2 disabled:opacity-50"
                >
                  {editLoading ? <Loader2 size={16} className="animate-spin" /> : <Save size={16} />}
                  {editLoading ? 'جاري الحفظ...' : 'حفظ التغييرات'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {ConfirmDialog}
    </div>
  );
}
