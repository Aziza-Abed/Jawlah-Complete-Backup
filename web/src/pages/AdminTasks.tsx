import { useEffect, useState } from "react";
import { getTasks, deleteTask, updateTask, reassignTask } from "../api/tasks";
import { getTaskTemplates, createTaskTemplate, deleteTaskTemplate, type TaskTemplate, type CreateTaskTemplateRequest } from "../api/templates";
import { getZones } from "../api/zones";
import { getWorkers } from "../api/users";
import type { TaskResponse, TaskStatus, TaskPriority, UpdateTaskRequest } from "../types/task";
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
  Plus,
  X,
  Save,
  Edit3
} from "lucide-react";
import { useConfirm } from "../components/common/ConfirmDialog";

export default function AdminTasks() {
  const [confirm, ConfirmDialog] = useConfirm();
  const [tasks, setTasks] = useState<TaskResponse[]>([]);
  const [zones, setZones] = useState<ZoneResponse[]>([]);
  const [workers, setWorkers] = useState<UserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<TaskStatus | "all">("all");
  const [priorityFilter, setPriorityFilter] = useState<TaskPriority | "all">("all");
  const [zoneFilter, setZoneFilter] = useState<string>("all");
  const [_unusedActionLoading, setActionLoading] = useState<number | null>(null);

  // Tabs & Templates State
  const [activeTab, setActiveTab] = useState<'tasks' | 'templates'>('tasks');
  const [showTemplateModal, setShowTemplateModal] = useState(false);
  const [templates, setTemplates] = useState<TaskTemplate[]>([]);
  const [templateForm, setTemplateForm] = useState<CreateTaskTemplateRequest>({
      title: '', description: '', zoneId: 0, frequency: 'Daily', time: '08:00'
  });
  const [templateLoading, setTemplateLoading] = useState(false);

  // Edit Task State
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingTask, setEditingTask] = useState<TaskResponse | null>(null);
  const [editForm, setEditForm] = useState<{
    title: string;
    description: string;
    priority: TaskPriority;
    assignedToUserId: number;
    zoneId: number | null;
  }>({ title: '', description: '', priority: 'Medium', assignedToUserId: 0, zoneId: null });
  const [editLoading, setEditLoading] = useState(false);

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
      if (zonesData.length > 0) setTemplateForm(prev => ({...prev, zoneId: zonesData[0].zoneId}));
    } catch (err) {
      console.error("Failed to fetch data", err);
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteTask = async (taskId: number) => {
    if (!await confirm("هل أنت متأكد من حذف (إلغاء) هذه المهمة نهائياً؟")) return;
    setActionLoading(taskId);
    try {
      await deleteTask(taskId);
      setTasks(tasks.filter(t => t.taskId !== taskId));
    } catch (err) {
      console.error("Failed to delete task", err);
    } finally {
      setActionLoading(null);
    }
  };

  const handleCreateTemplate = async (e: React.FormEvent) => {
      e.preventDefault();
      setTemplateLoading(true);
      try {
        const newTemplate = await createTaskTemplate({
            ...templateForm,
            zoneId: templateForm.zoneId === 0 ? undefined : templateForm.zoneId
        });
        setTemplates([...templates, newTemplate]);
        setShowTemplateModal(false);
        setTemplateForm({
            title: '', description: '', zoneId: zones.length > 0 ? zones[0].zoneId : 0, frequency: 'Daily', time: '08:00'
        });
      } catch (err) {
          console.error("Failed to create template", err);
          alert("فشل إنشاء القالب");
      } finally {
          setTemplateLoading(false);
      }
  };

  const handleDeleteTemplate = async (id: number) => {
      if(!await confirm("هل أنت متأكد من حذف هذا القالب؟")) return;
      try {
          await deleteTaskTemplate(id);
          setTemplates(templates.filter(t => t.id !== id));
      } catch (err) {
          console.error("Failed to delete template", err);
      }
  };

  const openEditModal = (task: TaskResponse) => {
    setEditingTask(task);
    setEditForm({
      title: task.title,
      description: task.description || '',
      priority: task.priority,
      assignedToUserId: task.assignedToUserId,
      zoneId: task.zoneId || null
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
        zoneId: editForm.zoneId || undefined
      };

      if (editForm.assignedToUserId !== editingTask.assignedToUserId) {
        await reassignTask(editingTask.taskId, {
          newAssignedToUserId: editForm.assignedToUserId,
          reassignmentReason: 'تم إعادة التعيين بواسطة المشرف'
        });
      }

      const updatedTask = await updateTask(editingTask.taskId, updateData);

      setTasks(prev => prev.map(t =>
        t.taskId === editingTask.taskId
          ? { ...updatedTask, assignedToUserId: editForm.assignedToUserId, assignedToUserName: workers.find(w => w.userId === editForm.assignedToUserId)?.fullName || updatedTask.assignedToUserName }
          : t
      ));

      setShowEditModal(false);
      setEditingTask(null);
    } catch (err) {
      console.error("Failed to update task", err);
      alert("فشل تحديث المهمة");
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
                <h1 className="font-sans font-black text-[28px] text-[#2F2F2F] tracking-tight">
                  إدارة العمليات
                </h1>
                <p className="text-[14px] font-bold text-[#AFAFAF] mt-1">متابعة وإسناد المهام الميدانية والروتينية</p>
              </div>
            </div>

            {/* Tabs */}
            <div className="flex bg-white/50 backdrop-blur-sm rounded-[18px] p-1.5 border border-black/5 shadow-sm">
              <button
                onClick={() => setActiveTab('tasks')}
                className={`px-6 py-2.5 rounded-[14px] text-[13px] font-black transition-all ${
                  activeTab === 'tasks'
                    ? 'bg-[#7895B2] text-white shadow-lg shadow-[#7895B2]/20'
                    : 'text-[#6B7280] hover:text-[#2F2F2F] hover:bg-white'
                }`}
              >
                المهام الحالية
              </button>
              <button
                onClick={() => setActiveTab('templates')}
                className={`px-6 py-2.5 rounded-[14px] text-[13px] font-black transition-all flex items-center gap-2 ${
                  activeTab === 'templates'
                    ? 'bg-[#7895B2] text-white shadow-lg shadow-[#7895B2]/20'
                    : 'text-[#6B7280] hover:text-[#2F2F2F] hover:bg-white'
                }`}
              >
                <Repeat size={14} />
                المهام المتكررة
              </button>
            </div>
          </div>

          {activeTab === 'tasks' ? (
            <>
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
                              <span className="font-black text-[13px] text-[#2F2F2F]">{task.assignedToUserName}</span>
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
                        onClick={() => (window.location.href = `/tasks/${task.taskId}`)}
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
            </>
          ) : (
            /* Templates Tab */
            <div className="space-y-6">
              <div className="flex justify-end">
                <button
                  onClick={() => setShowTemplateModal(true)}
                  className="flex items-center gap-2 bg-[#7895B2] hover:bg-[#6B87A3] text-white px-5 py-3 rounded-[12px] transition-all font-semibold text-[14px]"
                >
                  <Plus size={18} />
                  قالب جديد
                </button>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {templates.map(template => (
                  <div key={template.id} className="bg-white rounded-[16px] p-5 shadow-[0_4px_20px_rgba(0,0,0,0.04)] relative group">
                    <div className="absolute top-4 left-4">
                      <div className={`w-2.5 h-2.5 rounded-full ${template.isActive ? 'bg-[#8FA36A]' : 'bg-[#6B7280]/30'}`} />
                    </div>

                    <div className="flex items-center gap-4 mb-3 text-right flex-row-reverse">
                      <div className="p-2.5 bg-[#7895B2]/10 text-[#7895B2] rounded-[12px]">
                        <Repeat size={22} />
                      </div>
                      <div className="flex-1">
                        <h3 className="text-[15px] font-bold text-[#2F2F2F]">{template.title}</h3>
                        <div className="flex items-center justify-end gap-2 text-[11px] text-[#6B7280] mt-1">
                          <span>{template.time}</span>
                          <span className="bg-[#7895B2]/5 px-2 py-0.5 rounded border border-[#7895B2]/10">{template.frequency}</span>
                        </div>
                      </div>
                    </div>

                    <p className="text-[#6B7280] text-[13px] text-right mb-3">{template.description}</p>

                    <div className="flex items-center justify-end gap-2 text-[11px] text-[#6B7280] border-t border-[#F3F1ED] pt-3">
                      <span className="font-semibold">{template.zoneName || 'كل المناطق'}</span>
                      <MapPin size={12} className="text-[#7895B2]" />
                    </div>

                    <button
                      onClick={(e) => {
                        e.stopPropagation();
                        handleDeleteTemplate(template.id);
                      }}
                      className="absolute top-4 right-4 p-2 text-[#6B7280] hover:text-[#C86E5D] hover:bg-[#C86E5D]/10 rounded-full opacity-0 group-hover:opacity-100 transition-all"
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>
                ))}

                {templates.length === 0 && (
                  <div className="col-span-2 bg-white rounded-[16px] p-8 shadow-[0_4px_20px_rgba(0,0,0,0.04)] text-center">
                    <Repeat size={48} className="text-[#7895B2]/20 mx-auto mb-3" />
                    <p className="text-[#6B7280] text-[15px] font-medium">لا يوجد قوالب مهام متكررة</p>
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Add Template Modal */}
      {showTemplateModal && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-[16px] w-full max-w-lg p-6 shadow-xl">
            <div className="flex items-center justify-between mb-6">
              <button onClick={() => setShowTemplateModal(false)} className="p-2 hover:bg-[#F3F1ED] rounded-full transition-colors text-[#6B7280]">
                <X size={20} />
              </button>
              <h3 className="text-[18px] font-bold text-[#2F2F2F]">إضافة قالب مهمة متكررة</h3>
            </div>

            <form onSubmit={handleCreateTemplate} className="space-y-4">
              <div className="space-y-3">
                <label className="block text-right text-[12px] font-semibold text-[#6B7280]">عنوان المهمة</label>
                <input
                  required
                  value={templateForm.title}
                  onChange={e => setTemplateForm({...templateForm, title: e.target.value})}
                  className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  placeholder="مثال: تفتيش صباحي"
                />
              </div>
              <div className="space-y-3">
                <label className="block text-right text-[12px] font-semibold text-[#6B7280]">الوصف</label>
                <textarea
                  required
                  value={templateForm.description}
                  onChange={e => setTemplateForm({...templateForm, description: e.target.value})}
                  className="w-full h-24 px-4 py-3 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30 resize-none"
                  placeholder="وصف تفصيلي للمهمة..."
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">التكرار</label>
                  <select
                    value={templateForm.frequency}
                    onChange={e => setTemplateForm({...templateForm, frequency: e.target.value})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  >
                    <option value="Daily">يومي</option>
                    <option value="Weekly">أسبوعي</option>
                    <option value="Monthly">شهري</option>
                  </select>
                </div>
                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">وقت التعيين</label>
                  <input
                    type="time"
                    required
                    value={templateForm.time}
                    onChange={e => setTemplateForm({...templateForm, time: e.target.value})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-center text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  />
                </div>
              </div>
              <div className="space-y-3">
                <label className="block text-right text-[12px] font-semibold text-[#6B7280]">المنطقة</label>
                <select
                  value={templateForm.zoneId || 0}
                  onChange={e => setTemplateForm({...templateForm, zoneId: parseInt(e.target.value)})}
                  className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                >
                  <option value={0}>كل المناطق</option>
                  {zones.map(z => <option key={z.zoneId} value={z.zoneId}>{z.zoneName}</option>)}
                </select>
              </div>

              <div className="flex gap-3 pt-4 border-t border-[#F3F1ED]">
                <button
                  type="button"
                  onClick={() => setShowTemplateModal(false)}
                  className="flex-1 h-[46px] rounded-[12px] border border-[#E5E7EB] text-[#2F2F2F] hover:bg-[#F3F1ED] transition-all font-semibold text-[14px]"
                >
                  إلغاء
                </button>
                <button
                  type="submit"
                  disabled={templateLoading}
                  className="flex-1 h-[46px] rounded-[12px] bg-[#7895B2] hover:bg-[#6B87A3] text-white transition-all font-semibold text-[14px] flex items-center justify-center gap-2 disabled:opacity-50"
                >
                  {templateLoading ? <Loader2 size={16} className="animate-spin" /> : <Save size={16} />}
                  {templateLoading ? 'جاري الحفظ...' : 'حفظ القالب'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Edit Task Modal */}
      {showEditModal && editingTask && (
        <div className="fixed inset-0 bg-black/40 flex items-center justify-center p-4 z-50">
          <div className="bg-white rounded-[16px] w-full max-w-lg p-6 shadow-xl">
            <div className="flex items-center justify-between mb-6">
              <button onClick={() => { setShowEditModal(false); setEditingTask(null); }} className="p-2 hover:bg-[#F3F1ED] rounded-full transition-colors text-[#6B7280]">
                <X size={20} />
              </button>
              <h3 className="text-[18px] font-bold text-[#2F2F2F]">تعديل المهمة</h3>
            </div>

            <form onSubmit={handleEditTask} className="space-y-4">
              <div className="space-y-3">
                <label className="block text-right text-[12px] font-semibold text-[#6B7280]">عنوان المهمة</label>
                <input
                  required
                  value={editForm.title}
                  onChange={e => setEditForm({...editForm, title: e.target.value})}
                  className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                />
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
                    <option value="Low">عادية</option>
                    <option value="Medium">متوسطة</option>
                    <option value="High">عالية</option>
                    <option value="Urgent">حرجة</option>
                  </select>
                </div>

                <div className="space-y-3">
                  <label className="block text-right text-[12px] font-semibold text-[#6B7280]">العامل المكلف</label>
                  <select
                    value={editForm.assignedToUserId}
                    onChange={e => setEditForm({...editForm, assignedToUserId: parseInt(e.target.value)})}
                    className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                  >
                    {workers.map(w => (
                      <option key={w.userId} value={w.userId}>{w.fullName}</option>
                    ))}
                  </select>
                </div>
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

              <div className="flex gap-3 pt-4 border-t border-[#F3F1ED]">
                <button
                  type="button"
                  onClick={() => { setShowEditModal(false); setEditingTask(null); }}
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
