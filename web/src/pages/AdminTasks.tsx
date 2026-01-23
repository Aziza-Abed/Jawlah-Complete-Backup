import { useEffect, useState } from "react";
import { getTasks, deleteTask, getTaskTemplates, createTaskTemplate, deleteTaskTemplate, updateTask, reassignTask } from "../api/tasks";
import { getZones } from "../api/zones";
import { getWorkers } from "../api/users";
import type { TaskResponse, TaskStatus, TaskPriority, UpdateTaskRequest } from "../types/task";
import type { TaskTemplate, CreateTaskTemplateRequest } from "../api/tasks";
import type { ZoneResponse } from "../types/zone";
import type { UserResponse } from "../types/user";
import {
  ClipboardList,
  Search,
  CheckCircle2,
  Clock,
  XCircle,
  Trash2,
  User,
  MapPin,
  Calendar,
  Loader2,
  PauseCircle,
  PlayCircle,
  ShieldAlert,
  Repeat,
  Plus,
  X,
  Save,
  Edit3
} from "lucide-react";
import GlassCard from "../components/UI/GlassCard";

export default function AdminTasks() {
  const [tasks, setTasks] = useState<TaskResponse[]>([]);
  const [zones, setZones] = useState<ZoneResponse[]>([]);
  const [workers, setWorkers] = useState<UserResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [statusFilter, setStatusFilter] = useState<TaskStatus | "all">("all");
  const [priorityFilter, setPriorityFilter] = useState<TaskPriority | "all">("all");
  const [zoneFilter, setZoneFilter] = useState<string>("all");
  const [actionLoading, setActionLoading] = useState<number | null>(null);

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
    if (!window.confirm("هل أنت متأكد من حذف (إلغاء) هذه المهمة نهائياً؟")) return;
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
            zoneId: templateForm.zoneId === 0 ? null : templateForm.zoneId
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
      if(!window.confirm("هل أنت متأكد من حذف هذا القالب؟")) return;
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

      // Check if worker changed - need to use reassign endpoint
      if (editForm.assignedToUserId !== editingTask.assignedToUserId) {
        await reassignTask(editingTask.taskId, {
          newAssignedToUserId: editForm.assignedToUserId,
          reassignmentReason: 'تم إعادة التعيين بواسطة المشرف'
        });
      }

      const updatedTask = await updateTask(editingTask.taskId, updateData);

      // Update the task in the list
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

  const getStatusIcon = (status: TaskStatus) => {
    switch (status) {
      case "Pending": return <Clock size={16} />;
      case "InProgress": return <PlayCircle size={16} />;
      case "Completed": return <CheckCircle2 size={16} />;
      case "Approved": return <ShieldAlert size={16} />;
      case "Rejected": return <XCircle size={16} />;
      case "Cancelled": return <PauseCircle size={16} />;
      default: return null;
    }
  };

  const getStatusText = (status: TaskStatus) => {
    switch (status) {
      case "Pending": return "قيد الانتظار";
      case "InProgress": return "قيد التنفيذ";
      case "Completed": return "مكتملة";
      case "Approved": return "معتمدة";
      case "Rejected": return "مرفوضة";
      case "Cancelled": return "ملغاة";
      default: return status;
    }
  };

  const getStatusColor = (status: TaskStatus) => {
      switch (status) {
      case "Pending": return "bg-warning/10 text-warning border-warning/30";
      case "InProgress": return "bg-primary/10 text-primary border-primary/30";
      case "Completed": return "bg-secondary/10 text-secondary border-secondary/30";
      case "Approved": return "bg-secondary text-white border-secondary";
      case "Rejected": return "bg-accent/10 text-accent border-accent/30";
      case "Cancelled": return "bg-text-muted/10 text-text-muted border-text-muted/30";
      default: return "bg-primary/5 text-text-primary border-primary/10";
      }
  };

  const filteredTasks = tasks.filter(task => {
    const matchesSearch = task.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         task.description?.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesStatus = statusFilter === "all" || task.status === statusFilter;
    const matchesPriority = priorityFilter === "all" || task.priority === priorityFilter;
    const matchesZone = zoneFilter === "all" || (task.zoneId && task.zoneId.toString() === zoneFilter);

    return matchesSearch && matchesStatus && matchesPriority && matchesZone;
  });

  if (loading) {
    return (
      <div className="h-full w-full flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
             <div className="w-12 h-12 border-4 border-primary/30 border-t-primary rounded-full animate-spin"></div>
             <p className="text-text-secondary font-medium">جاري تحميل المهام...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8 pb-10">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
          <div className="flex flex-col items-start text-right">
            <h1 className="text-3xl font-extrabold text-text-primary">
                إدارة المهام
            </h1>
            <p className="text-right text-text-secondary mt-2 font-medium">متابعة وإدارة المهام الميدانية والروتينية</p>
          </div>
          
          <div className="flex bg-primary/5 rounded-xl p-1 border border-primary/10">
              <button
                  onClick={() => setActiveTab('tasks')}
                  className={`px-4 py-2 rounded-lg text-sm font-bold transition-all ${activeTab === 'tasks' ? 'bg-primary text-white shadow-lg' : 'text-text-secondary hover:text-text-primary'}`}
              >
                  المهام الحالية
              </button>
              <button
                  onClick={() => setActiveTab('templates')}
                  className={`px-4 py-2 rounded-lg text-sm font-bold transition-all flex items-center gap-2 ${activeTab === 'templates' ? 'bg-primary text-white shadow-lg' : 'text-text-secondary hover:text-text-primary'}`}
              >
                  المهام المتكررة
                  <Repeat size={14} />
              </button>
          </div>
        </div>

        {activeTab === 'tasks' ? (
            <>
                <GlassCard className="flex flex-col md:flex-row gap-4">
                    <div className="flex-1 relative group w-full">
                        <input
                            type="text"
                            placeholder="بحث في المهام..."
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                            className="glass-input w-full h-12 pr-11 text-right focus:bg-primary/5 text-text-primary"
                        />
                        <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-text-muted group-focus-within:text-primary transition-colors" size={20} />
                    </div>

                    <div className="flex flex-wrap gap-2 justify-end w-full md:w-auto">
                        <select
                            value={zoneFilter}
                            onChange={(e) => setZoneFilter(e.target.value)}
                            className="glass-input h-12 w-full sm:w-[150px] text-right appearance-none cursor-pointer focus:bg-primary/5 text-text-primary [&>option]:text-black"
                        >
                            <option value="all">كل المناطق</option>
                            {zones.map(z => <option key={z.zoneId} value={z.zoneId}>{z.zoneName}</option>)}
                        </select>
                         <select
                            value={statusFilter}
                            onChange={(e) => setStatusFilter(e.target.value as any)}
                            className="glass-input h-12 w-full sm:w-[150px] text-right appearance-none cursor-pointer focus:bg-primary/5 text-text-primary [&>option]:text-black"
                        >
                            <option value="all">كل الحالات</option>
                            <option value="Pending">قيد الانتظار</option>
                            <option value="InProgress">قيد التنفيذ</option>
                            <option value="Completed">بانتظار الاعتماد</option>
                            <option value="Approved">تم الاعتماد</option>
                            <option value="Rejected">مرفوضة</option>
                            <option value="Cancelled">ملغاة</option>
                        </select>
                        <select
                            value={priorityFilter}
                            onChange={(e) => setPriorityFilter(e.target.value as any)}
                            className="glass-input h-12 w-full sm:w-[150px] text-right appearance-none cursor-pointer focus:bg-primary/5 text-text-primary [&>option]:text-black"
                        >
                            <option value="all">كل الأولويات</option>
                            <option value="Urgent">حرجة</option>
                            <option value="High">عالية</option>
                            <option value="Medium">متوسطة</option>
                            <option value="Low">منخفضة</option>
                        </select>
                    </div>
                </GlassCard>

                <div className="grid grid-cols-1 gap-4">
                    {filteredTasks.map((task, idx) => (
                        <GlassCard 
                            key={task.taskId} 
                            variant="hover" 
                            noPadding 
                            className="p-5 flex flex-col md:flex-row gap-6 items-start md:items-center group animate-slide-up"
                            style={{ animationDelay: `${idx * 50}ms` }}
                        >
                            <div className="flex-1 w-full md:w-auto text-right">
                                <div className="flex items-center justify-start gap-3 mb-2 flex-wrap">
                                     <h3 className="font-extrabold text-text-primary text-lg flex-1 text-right line-clamp-1">{task.title}</h3>
                                     
                                     <span className={`px-3 py-1 rounded-full text-[10px] font-bold border ${
                                            task.priority === 'Urgent' ? 'bg-accent/10 text-accent border-accent/30' :
                                            task.priority === 'High' ? 'bg-warning/10 text-warning border-warning/30 shadow-warning/20 shadow-sm' :
                                            task.priority === 'Medium' ? 'bg-warning/5 text-warning border-warning/20' :
                                            'bg-primary/10 text-primary border-primary/30'
                                        }`}>
                                            {task.priority === 'Urgent' ? 'أولوية حرجة' : 
                                             task.priority === 'High' ? 'أولوية عالية' : 
                                             task.priority === 'Medium' ? 'أولوية متوسطة' : 'أولوية عادية'}
                                    </span>

                                     <div className={`flex items-center gap-1.5 px-3 py-1 rounded-full text-[10px] font-bold border ${getStatusColor(task.status)}`}>
                                        {getStatusText(task.status)}
                                        {getStatusIcon(task.status)}
                                     </div>
                                </div>
                                <p className="text-text-secondary text-sm mb-4 line-clamp-1 pl-4">{task.description}</p>
                                
                                <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 border-t border-primary/5 pt-4 mt-2">
                                    <div className="flex items-center justify-start gap-2 text-xs text-text-muted">
                                        <User size={14} className="opacity-60 text-primary" />
                                        <span className="font-bold text-text-secondary">{task.assignedToUserName}</span>
                                    </div>
                                    <div className="flex items-center justify-start gap-2 text-xs text-text-muted">
                                        <MapPin size={14} className="opacity-60 text-primary" />
                                        <span className="font-bold text-text-secondary">{task.zoneName || 'خارج المنطقة'}</span>
                                    </div>
                                    <div className="flex items-center justify-start gap-2 text-xs text-text-muted">
                                        <Calendar size={14} className="opacity-60 text-primary" />
                                        <span className="font-bold text-text-secondary">{task.createdAt ? new Date(task.createdAt).toLocaleDateString('ar-EG') : '-'}</span>
                                    </div>
                                </div>
                            </div>
    
                            <div className="flex items-center gap-3 w-full md:w-auto md:border-r border-primary/10 md:pr-6 md:my-2 justify-end">
                                <button
                                    onClick={() => (window.location.href = `/tasks/${task.taskId}`)}
                                    className="px-4 py-2 bg-primary/5 hover:bg-primary/10 text-text-primary rounded-xl transition-all border border-primary/10 font-bold text-xs"
                                >
                                    التفاصيل
                                </button>

                                <button
                                    onClick={() => openEditModal(task)}
                                    className="p-2 text-text-muted hover:text-primary hover:bg-primary/5 rounded-xl transition-colors border border-transparent hover:border-primary/10 flex items-center justify-center"
                                    title="تعديل المهمة"
                                >
                                    <Edit3 size={18} />
                                </button>

                                <button
                                    onClick={() => handleDeleteTask(task.taskId)}
                                    disabled={actionLoading === task.taskId}
                                    className="p-2 text-text-muted hover:text-accent hover:bg-accent/5 rounded-xl transition-colors border border-transparent hover:border-accent/10 flex items-center justify-center"
                                    title="حذف المهمة"
                                >
                                    {actionLoading === task.taskId ? <Loader2 size={18} className="animate-spin" /> : <Trash2 size={18} />}
                                </button>
                            </div>
                        </GlassCard>
                    ))}
                    {filteredTasks.length === 0 && (
                        <div className="text-center py-20 opacity-50">
                            <ClipboardList size={48} className="mx-auto mb-4" />
                            <p className="text-lg font-medium">لا توجد مهام مطابقة</p>
                        </div>
                    )}
                </div>
            </>
        ) : (
            <div className="space-y-6 animate-fade-in">
                <div className="flex justify-end">
                    <button 
                        onClick={() => setShowTemplateModal(true)}
                        className="flex items-center gap-2 bg-primary hover:bg-primary-dark text-white px-6 py-3 rounded-xl transition-all shadow-lg shadow-primary/20 hover:shadow-primary/40 active:transform active:scale-95 font-bold"
                    >
                        <Plus size={20} />
                        قالب جديد
                    </button>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {templates.map(template => (
                         <GlassCard key={template.id} variant="hover" className="relative group !bg-background-paper !border-primary/5">
                             <div className="absolute top-4 left-4">
                                <div className={`w-3 h-3 rounded-full ${template.isActive ? 'bg-secondary shadow-[0_0_8px_rgba(42,157,143,0.4)]' : 'bg-text-muted/30'}`} />
                             </div>
                             <div className="flex items-center gap-4 mb-4 text-right">
                                 <div className="p-3 bg-primary/10 text-primary rounded-xl">
                                     <Repeat size={24} />
                                 </div>
                                 <div className="flex-1">
                                     <h3 className="text-lg font-bold text-text-primary">{template.title}</h3>
                                     <div className="flex items-center justify-end gap-2 text-xs text-text-muted mt-1">
                                         <span>{template.time}</span>
                                         <span className="bg-primary/5 px-2 py-0.5 rounded border border-primary/10">{template.frequency}</span>
                                     </div>
                                 </div>
                             </div>
                             <p className="text-text-secondary text-sm text-right mb-4">{template.description}</p>
                             <div className="flex items-center justify-end gap-2 text-xs text-text-muted border-t border-primary/5 pt-3 mt-auto w-full">
                                 <span className="font-bold">{template.zoneName || 'كل المناطق'}</span>
                                 <MapPin size={12} className="text-primary" />
                             </div>
                             
                             <button 
                                onClick={(e) => {
                                    e.stopPropagation();
                                    handleDeleteTemplate(template.id);
                                }}
                                className="absolute top-4 right-4 p-2 text-text-muted hover:text-accent hover:bg-accent/5 rounded-full opacity-0 group-hover:opacity-100 transition-all"
                             >
                                 <Trash2 size={16} />
                             </button>
                         </GlassCard>
                    ))}
                </div>
            </div>
        )}

        {/* Add Template Modal */}
        {showTemplateModal && (
             <div className="fixed inset-0 bg-black/40 backdrop-blur-sm flex items-center justify-center p-4 z-50 animate-fade-in">
                <GlassCard variant="panel" className="w-full max-w-lg !bg-background-paper !border-primary/10 shadow-2xl">
                    <div className="flex items-center justify-between mb-6 border-b border-primary/5 pb-4">
                        <button onClick={() => setShowTemplateModal(false)} className="p-2 hover:bg-primary/5 rounded-full transition-colors text-text-primary">
                            <X size={20} />
                        </button>
                        <h3 className="text-xl font-bold text-text-primary text-right">إضافة قالب مهمة متكررة</h3>
                    </div>

                    <form onSubmit={handleCreateTemplate} className="space-y-4">
                        <div className="space-y-2">
                            <label className="block text-right text-sm font-bold text-text-muted">عنوان المهمة</label>
                            <input 
                                required
                                value={templateForm.title}
                                onChange={e => setTemplateForm({...templateForm, title: e.target.value})}
                                className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary focus:bg-primary/10 transition-colors"
                                placeholder="مثال: تفتيش صباحي"
                            />
                        </div>
                         <div className="space-y-2">
                            <label className="block text-right text-sm font-bold text-text-muted">الوصف</label>
                            <textarea 
                                required
                                value={templateForm.description}
                                onChange={e => setTemplateForm({...templateForm, description: e.target.value})}
                                className="glass-input w-full h-24 text-right !bg-primary/5 text-text-primary pt-2 focus:bg-primary/10 transition-colors"
                                placeholder="وصف تفصيلي للمهمة..."
                            />
                        </div>
                        <div className="grid grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <label className="block text-right text-sm font-bold text-text-muted">التكرار</label>
                                <select 
                                    value={templateForm.frequency}
                                    onChange={e => setTemplateForm({...templateForm, frequency: e.target.value as any})}
                                    className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary [&>option]:text-black focus:bg-primary/10 transition-colors"
                                >
                                    <option value="Daily">يومي</option>
                                    <option value="Weekly">أسبوعي</option>
                                    <option value="Monthly">شهري</option>
                                </select>
                            </div>
                            <div className="space-y-2">
                                <label className="block text-right text-sm font-bold text-text-muted">وقت التعيين</label>
                                <input 
                                    type="time"
                                    required
                                    value={templateForm.time}
                                    onChange={e => setTemplateForm({...templateForm, time: e.target.value})}
                                    className="glass-input w-full h-11 text-center !bg-primary/5 text-text-primary focus:bg-primary/10 transition-colors"
                                />
                            </div>
                        </div>
                            <div className="space-y-2">
                                <label className="block text-right text-sm font-bold text-text-muted">المنطقة</label>
                                <select 
                                    value={templateForm.zoneId || 0}
                                    onChange={e => setTemplateForm({...templateForm, zoneId: parseInt(e.target.value)})}
                                    className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary [&>option]:text-black focus:bg-primary/10 transition-colors"
                                >
                                    <option value={0}>كل المناطق</option>
                                    {zones.map(z => <option key={z.zoneId} value={z.zoneId}>{z.zoneName}</option>)}
                                </select>
                            </div>

                        <div className="flex gap-4 pt-4 border-t border-primary/5">
                            <button
                                type="button"
                                onClick={() => setShowTemplateModal(false)}
                                className="flex-1 h-11 rounded-xl border border-primary/10 text-text-primary hover:bg-primary/5 transition-all font-bold"
                            >
                                إلغاء
                            </button>
                            <button
                                type="submit"
                                className="flex-1 h-11 rounded-xl bg-primary hover:bg-primary-dark text-white transition-all font-bold shadow-lg shadow-primary/20 flex items-center justify-center gap-2"
                            >
                                {templateLoading ? <Loader2 size={18} className="animate-spin" /> : <Save size={18} />}
                                {templateLoading ? 'جاري الحفظ...' : 'حفظ القالب'}
                            </button>
                        </div>
                    </form>
                </GlassCard>
             </div>
        )}

        {/* Edit Task Modal */}
        {showEditModal && editingTask && (
            <div className="fixed inset-0 bg-black/40 backdrop-blur-sm flex items-center justify-center p-4 z-50 animate-fade-in">
                <GlassCard variant="panel" className="w-full max-w-lg !bg-background-paper !border-primary/10 shadow-2xl">
                    <div className="flex items-center justify-between mb-6 border-b border-primary/5 pb-4">
                        <button onClick={() => { setShowEditModal(false); setEditingTask(null); }} className="p-2 hover:bg-primary/5 rounded-full transition-colors text-text-primary">
                            <X size={20} />
                        </button>
                        <h3 className="text-xl font-bold text-text-primary text-right">تعديل المهمة</h3>
                    </div>

                    <form onSubmit={handleEditTask} className="space-y-4">
                        <div className="space-y-2">
                            <label className="block text-right text-sm font-bold text-text-muted">عنوان المهمة</label>
                            <input
                                required
                                value={editForm.title}
                                onChange={e => setEditForm({...editForm, title: e.target.value})}
                                className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary focus:bg-primary/10 transition-colors"
                            />
                        </div>

                        <div className="space-y-2">
                            <label className="block text-right text-sm font-bold text-text-muted">الوصف</label>
                            <textarea
                                value={editForm.description}
                                onChange={e => setEditForm({...editForm, description: e.target.value})}
                                className="glass-input w-full h-24 text-right !bg-primary/5 text-text-primary pt-2 focus:bg-primary/10 transition-colors"
                            />
                        </div>

                        <div className="grid grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <label className="block text-right text-sm font-bold text-text-muted">الأولوية</label>
                                <select
                                    value={editForm.priority}
                                    onChange={e => setEditForm({...editForm, priority: e.target.value as TaskPriority})}
                                    className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary [&>option]:text-black focus:bg-primary/10 transition-colors"
                                >
                                    <option value="Low">عادية</option>
                                    <option value="Medium">متوسطة</option>
                                    <option value="High">عالية</option>
                                    <option value="Urgent">حرجة</option>
                                </select>
                            </div>

                            <div className="space-y-2">
                                <label className="block text-right text-sm font-bold text-text-muted">العامل المكلف</label>
                                <select
                                    value={editForm.assignedToUserId}
                                    onChange={e => setEditForm({...editForm, assignedToUserId: parseInt(e.target.value)})}
                                    className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary [&>option]:text-black focus:bg-primary/10 transition-colors"
                                >
                                    {workers.map(w => (
                                        <option key={w.userId} value={w.userId}>{w.fullName}</option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        <div className="space-y-2">
                            <label className="block text-right text-sm font-bold text-text-muted">المنطقة</label>
                            <select
                                value={editForm.zoneId || 0}
                                onChange={e => setEditForm({...editForm, zoneId: parseInt(e.target.value) || null})}
                                className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary [&>option]:text-black focus:bg-primary/10 transition-colors"
                            >
                                <option value={0}>بدون منطقة</option>
                                {zones.map(z => <option key={z.zoneId} value={z.zoneId}>{z.zoneName}</option>)}
                            </select>
                        </div>

                        <div className="flex gap-4 pt-4 border-t border-primary/5">
                            <button
                                type="button"
                                onClick={() => { setShowEditModal(false); setEditingTask(null); }}
                                className="flex-1 h-11 rounded-xl border border-primary/10 text-text-primary hover:bg-primary/5 transition-all font-bold"
                            >
                                إلغاء
                            </button>
                            <button
                                type="submit"
                                disabled={editLoading}
                                className="flex-1 h-11 rounded-xl bg-primary hover:bg-primary-dark text-white transition-all font-bold shadow-lg shadow-primary/20 flex items-center justify-center gap-2 disabled:opacity-50"
                            >
                                {editLoading ? <Loader2 size={18} className="animate-spin" /> : <Save size={18} />}
                                {editLoading ? 'جاري الحفظ...' : 'حفظ التغييرات'}
                            </button>
                        </div>
                    </form>
                </GlassCard>
            </div>
        )}
    </div>
  );
}
