import { useEffect, useState } from "react";
import { getMunicipalities, updateMunicipality, createMunicipality } from "../api/municipality";
import type { Municipality } from "../api/municipality";
import {
  getGisFilesStatus,
  uploadGisFile,
  getZonesSummary,
  type GisFilesStatus,
  type GisFileType,
  type ZonesSummary
} from "../api/gis";
import {
  Building2,
  Loader2,
  CheckCircle,
  AlertCircle,
  Save,
  Clock,
  Phone,
  Map,
  Calendar,
  Upload,
  FileCheck,
  Layers,
  RefreshCw
} from "lucide-react";
import GlassCard from "../components/UI/GlassCard";

export default function AdminSystemSettings() {
  const [municipality, setMunicipality] = useState<Municipality | null>(null);
  const [loading, setLoading] = useState(true);
  const [actionLoading, setActionLoading] = useState(false);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);
  const [isEditing, setIsEditing] = useState(false);

  // GIS states
  const [gisStatus, setGisStatus] = useState<GisFilesStatus | null>(null);
  const [zonesSummary, setZonesSummary] = useState<ZonesSummary | null>(null);
  const [gisLoading, setGisLoading] = useState(false);
  const [uploadingType, setUploadingType] = useState<GisFileType | null>(null);

  // Form state
  const [formData, setFormData] = useState<Partial<Municipality>>({
    name: "",
    nameEnglish: "",
    code: "",
    country: "فلسطين",
    region: "",
    contactEmail: "",
    contactPhone: "",
    address: "",
    minLatitude: 31.0,
    maxLatitude: 32.5,
    minLongitude: 34.0,
    maxLongitude: 35.5,
    defaultStartTime: "07:00:00",
    defaultEndTime: "15:00:00",
    defaultGraceMinutes: 15,
    maxAcceptableAccuracyMeters: 50,
    licenseExpiresAt: new Date(new Date().setFullYear(new Date().getFullYear() + 1)).toISOString().split('T')[0]
  });

  useEffect(() => {
    fetchMyMunicipality();
    fetchGisData();
  }, []);

  const fetchGisData = async () => {
    try {
      setGisLoading(true);
      const [status, summary] = await Promise.all([
        getGisFilesStatus(),
        getZonesSummary()
      ]);
      setGisStatus(status);
      setZonesSummary(summary);
    } catch (err) {
      console.error("Failed to fetch GIS data", err);
    } finally {
      setGisLoading(false);
    }
  };

  const handleGisUpload = async (file: File, fileType: GisFileType) => {
    setUploadingType(fileType);
    setMessage({ type: "success", text: `جاري رفع ملف ${fileType === "Quarters" ? "الأحياء" : fileType === "Borders" ? "الحدود" : "البلوكات"}...` });

    try {
      const result = await uploadGisFile(file, fileType, { autoImport: true });

      if (result.success) {
        setMessage({
          type: "success",
          text: result.warning ? `${result.message} (تحذير: ${result.warning})` : result.message
        });
        // Refresh GIS data
        await fetchGisData();
      } else {
        setMessage({ type: "error", text: result.message });
      }
    } catch (err: any) {
      console.error("Upload error", err);
      setMessage({ type: "error", text: err.response?.data?.message || "فشل رفع الملف" });
    } finally {
      setUploadingType(null);
      setTimeout(() => setMessage(null), 5000);
    }
  };

  const fetchMyMunicipality = async () => {
    try {
      setLoading(true);
      const data = await getMunicipalities();
      if (data && data.length > 0) {
        // Assume the first one is "My Municipality" for single-tenant context
        setMunicipality(data[0]);
        setFormData(data[0]);
      } else {
        setMunicipality(null);
      }
    } catch (err) {
      console.error("Failed to fetch municipality", err);
      setMessage({ type: "error", text: "فشل تحميل بيانات البلدية" });
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setActionLoading(true);
    try {
      if (municipality) {
        await updateMunicipality(municipality.municipalityId, formData);
        setMessage({ type: "success", text: "تم تحديث إعدادات البلدية بنجاح" });
        setMunicipality({ ...municipality, ...formData } as Municipality);
        setIsEditing(false);
      } else {
        // Initialization mode
        await createMunicipality(formData);
        setMessage({ type: "success", text: "تم تكوين إعدادات البلدية بنجاح" });
        fetchMyMunicipality();
      }
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      setMessage({ type: "error", text: "فشل حفظ الإعدادات" });
    } finally {
      setActionLoading(false);
    }
  };

  const handleCancel = () => {
      setIsEditing(false);
      if (municipality) {
          setFormData(municipality);
      }
  };

  if (loading) {
    return (
      <div className="h-full w-full flex items-center justify-center">
         <div className="flex flex-col items-center gap-4">
             <div className="w-12 h-12 border-4 border-primary/30 border-t-primary rounded-full animate-spin"></div>
             <p className="text-text-secondary font-medium">جاري تحميل الإعدادات...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8 pb-10 max-w-5xl mx-auto">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-6">
          <div className="flex flex-col items-start">
            <h1 className="text-3xl font-extrabold text-text-primary">
                إعدادات النظام
            </h1>
            <p className="text-right text-text-secondary mt-2 font-medium">إعدادات {municipality?.name || "البلدية"} - أوقات العمل، GPS، وبيانات التواصل</p>
          </div>
          
          {municipality && !isEditing && (
             <button 
                onClick={() => setIsEditing(true)}
                className="flex items-center gap-2 bg-primary/10 hover:bg-primary/20 text-primary border border-primary/20 px-6 py-3 rounded-xl transition-all font-bold"
              >
                تعديل الإعدادات
                <Save size={20} />
            </button>
          )}
        </div>

        {message && (
          <div className={`p-4 rounded-xl flex items-center justify-end gap-3 text-right animate-fade-in ${
            message.type === "success" ? "bg-secondary/10 text-secondary border border-secondary/20" : "bg-accent/10 text-accent border border-accent/20"
          }`}>
            <span className="font-bold">{message.text}</span>
            {message.type === "success" ? <CheckCircle size={20} /> : <AlertCircle size={20} />}
          </div>
        )}

        <GlassCard className="p-8 relative overflow-hidden !bg-background-paper shadow-xl !border-primary/5">
             {/* Decorative Background */}
             <div className="absolute top-0 right-0 w-64 h-64 bg-primary/10 rounded-full blur-3xl -z-10 translate-x-1/3 -translate-y-1/3"></div>
             
             {!municipality && !isEditing && (
                 <div className="text-center py-10">
                     <Building2 size={64} className="mx-auto text-primary/10 mb-4" />
                     <h2 className="text-2xl font-bold text-text-primary mb-2">النظام غير مهيأ</h2>
                     <p className="text-text-muted mb-6">لم يتم تكوين إعدادات البلدية بعد. قم ببدء الإعداد الآن.</p>
                     <button 
                        onClick={() => setIsEditing(true)}
                        className="bg-primary hover:bg-primary-dark text-white px-8 py-3 rounded-xl font-bold shadow-lg shadow-primary/20 transition-all font-sans"
                     >
                         بدء الإعداد
                     </button>
                 </div>
             )}

             {(municipality || isEditing) && (
                  <form onSubmit={handleSubmit} className="space-y-8">
                     {/* Header Section */}
                     <div className="flex flex-col md:flex-row-reverse items-center gap-8 border-b border-primary/5 pb-8">
                         <div className="w-24 h-24 rounded-2xl bg-gradient-to-br from-primary/10 to-primary/5 border border-primary/10 flex items-center justify-center shrink-0 shadow-inner">
                             <Building2 size={40} className="text-primary" />
                         </div>
                         <div className="flex-1 w-full text-right">
                            <div className="space-y-2">
                                <label className="text-xs font-bold text-text-muted uppercase">اسم البلدية</label>
                                <input 
                                    disabled={!isEditing} 
                                    required 
                                    type="text" 
                                    value={formData.name} 
                                    onChange={e => setFormData({...formData, name: e.target.value})} 
                                    className="glass-input w-full h-12 text-right disabled:opacity-70 disabled:border-transparent font-bold text-lg text-text-primary !bg-primary/5 focus:bg-primary/10" 
                                    placeholder="اسم البلدية"
                                />
                            </div>
                         </div>
                     </div>

                     {/* Settings Grid */}
                     <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                         {/* Contact Info (Simplified) */}
                         <div className="space-y-6">
                             <h3 className="text-xl font-bold text-text-primary text-right border-r-4 border-primary pr-3">بيانات التواصل</h3>
                             <div className="space-y-4">
                                <div className="space-y-2 text-right">
                                    <label className="text-xs font-bold text-text-muted">رقم الهاتف / الخط الساخن (اختياري)</label>
                                    <div className="relative">
                                        <input 
                                            disabled={!isEditing} 
                                            type="text" 
                                            value={formData.contactPhone} 
                                            onChange={e => setFormData({...formData, contactPhone: e.target.value})} 
                                            className="glass-input w-full h-11 text-right pr-10 disabled:opacity-70 font-mono text-text-primary !bg-primary/5 focus:bg-primary/10" 
                                        />
                                        <Phone size={16} className="absolute right-3 top-1/2 -translate-y-1/2 text-text-muted" />
                                    </div>
                                </div>
                            </div>
                         </div>

                         {/* System Settings */}
                         <div className="space-y-6">
                             <h3 className="text-xl font-bold text-text-primary text-right border-r-4 border-secondary pr-3">إعدادات النظام</h3>
                             <div className="grid grid-cols-2 gap-4">
                                <div className="space-y-2 text-right">
                                    <label className="text-xs font-bold text-text-muted">وقت بدء العمل</label>
                                    <div className="relative">
                                        <input 
                                            disabled={!isEditing} 
                                            type="time" 
                                            value={formData.defaultStartTime?.substring(0, 5)} 
                                            onChange={e => setFormData({...formData, defaultStartTime: e.target.value + ":00"})} 
                                            className="glass-input w-full h-11 text-center font-mono disabled:opacity-70 text-text-primary !bg-primary/5 focus:bg-primary/10" 
                                        />
                                        <Clock size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none" />
                                    </div>
                                </div>
                                <div className="space-y-2 text-right">
                                    <label className="text-xs font-bold text-text-muted">وقت انتهاء العمل</label>
                                    <div className="relative">
                                        <input 
                                            disabled={!isEditing} 
                                            type="time" 
                                            value={formData.defaultEndTime?.substring(0, 5)} 
                                            onChange={e => setFormData({...formData, defaultEndTime: e.target.value + ":00"})} 
                                            className="glass-input w-full h-11 text-center font-mono disabled:opacity-70 text-text-primary !bg-primary/5 focus:bg-primary/10" 
                                        />
                                        <Clock size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-text-muted pointer-events-none" />
                                    </div>
                                </div>
                             </div>
                             
                             <div className="grid grid-cols-2 gap-4">
                                <div className="space-y-2 text-right">
                                    <label className="text-xs font-bold text-text-muted">فترة السماح (دقائق)</label>
                                    <input 
                                        disabled={!isEditing} 
                                        type="number" 
                                        value={formData.defaultGraceMinutes} 
                                        onChange={e => setFormData({...formData, defaultGraceMinutes: parseInt(e.target.value)})} 
                                        className="glass-input w-full h-11 text-center font-mono disabled:opacity-70 text-text-primary !bg-primary/5 focus:bg-primary/10" 
                                    />
                                </div>
                                <div className="space-y-2 text-right">
                                    <label className="text-xs font-bold text-text-muted">نطاق دقة الـ GPS (متر)</label>
                                    <input 
                                        disabled={!isEditing} 
                                        type="number" 
                                        value={formData.maxAcceptableAccuracyMeters} 
                                        onChange={e => setFormData({...formData, maxAcceptableAccuracyMeters: parseInt(e.target.value)})} 
                                        className="glass-input w-full h-11 text-center font-mono disabled:opacity-70 text-text-primary !bg-primary/5 focus:bg-primary/10" 
                                    />
                                </div>
                             </div>

                             <div className="pt-4 border-t border-primary/5 mt-2">
                                 <div className="flex items-center justify-between bg-primary/5 p-3 rounded-xl border border-primary/10">
                                     <span className="text-xs font-bold text-text-primary font-mono">{municipality?.licenseExpiresAt ? new Date(municipality.licenseExpiresAt).toLocaleDateString('ar-EG') : 'غير محدد'}</span>
                                     <div className="flex items-center gap-2 text-text-muted text-xs font-bold">
                                         <span>تاريخ انتهاء الترخيص</span>
                                         <Calendar size={14} />
                                     </div>
                                 </div>
                             </div>
                         </div>
                     </div>

                      {/* GIS Data Section */}
                     <div className="border-t border-primary/5 pt-8">
                         <div className="flex items-center justify-between mb-6">
                             <button
                                 onClick={fetchGisData}
                                 disabled={gisLoading}
                                 className="flex items-center gap-2 text-sm text-text-muted hover:text-text-primary transition-colors"
                             >
                                 <RefreshCw size={16} className={gisLoading ? "animate-spin" : ""} />
                                 تحديث
                             </button>
                             <h3 className="text-xl font-bold text-text-primary text-right border-r-4 border-secondary pr-3">بيانات الخرائط (GIS)</h3>
                         </div>

                         {/* Zones Summary */}
                         {zonesSummary && (
                             <div className="bg-secondary/10 border border-secondary/20 rounded-xl p-4 mb-6 flex items-center justify-between">
                                 <div className="flex items-center gap-4">
                                     {zonesSummary.byType.map((t) => (
                                         <span key={t.type} className="text-xs bg-white/50 border border-primary/10 px-3 py-1 rounded-full text-text-primary">
                                             {t.type}: {t.count}
                                         </span>
                                     ))}
                                 </div>
                                 <div className="flex items-center gap-2 text-secondary">
                                     <span className="font-bold">{zonesSummary.totalZones} منطقة مسجلة</span>
                                     <Layers size={20} />
                                 </div>
                             </div>
                         )}

                         {/* GIS File Upload Cards */}
                         <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                             {/* Quarters */}
                             <GisFileCard
                                 title="الأحياء (Quarters)"
                                 description="حدود الأحياء والمناطق السكنية"
                                 fileType="Quarters"
                                 file={gisStatus?.quarters}
                                 isUploading={uploadingType === "Quarters"}
                                 onUpload={(file) => handleGisUpload(file, "Quarters")}
                             />

                             {/* Borders */}
                             <GisFileCard
                                 title="الحدود (Borders)"
                                 description="حدود البلدية الخارجية"
                                 fileType="Borders"
                                 file={gisStatus?.borders}
                                 isUploading={uploadingType === "Borders"}
                                 onUpload={(file) => handleGisUpload(file, "Borders")}
                             />

                             {/* Blocks */}
                             <GisFileCard
                                 title="البلوكات (Blocks)"
                                 description="تقسيمات البلوكات (اختياري)"
                                 fileType="Blocks"
                                 file={gisStatus?.blocks}
                                 isUploading={uploadingType === "Blocks"}
                                 onUpload={(file) => handleGisUpload(file, "Blocks")}
                                 optional
                             />
                         </div>

                         <p className="text-xs text-text-secondary mt-4 text-right">
                             * يجب رفع ملفات GeoJSON (.json أو .geojson). الحد الأقصى للملف: 10MB
                         </p>
                     </div>

                     {/* Action Buttons */}
                     {isEditing && (
                         <div className="flex gap-4 pt-8 border-t border-primary/5 animate-fade-in">
                             <button 
                                type="button" 
                                onClick={handleCancel}
                                className="flex-1 h-12 rounded-xl border border-primary/10 text-text-primary hover:bg-primary/5 transition-all font-bold"
                            >
                                إلغاء
                            </button>
                             <button 
                                type="submit" 
                                disabled={actionLoading} 
                                className="flex-1 h-12 rounded-xl bg-primary hover:bg-primary-dark text-white transition-all font-bold shadow-lg shadow-primary/20 disabled:opacity-50 flex items-center justify-center gap-2"
                            >
                                {actionLoading ? <Loader2 size={20} className="animate-spin" /> : <Save size={20} />}
                                حفظ التغييرات
                             </button>
                         </div>
                     )}
                 </form>
             )}
        </GlassCard>
    </div>
  );
}

// GIS File Upload Card Component
interface GisFileCardProps {
  title: string;
  description: string;
  fileType: GisFileType;
  file: import("../api/gis").GisFileDto | null | undefined;
  isUploading: boolean;
  onUpload: (file: File) => void;
  optional?: boolean;
}

function GisFileCard({ title, description, file, isUploading, onUpload, optional }: GisFileCardProps) {
  const hasFile = !!file;

  return (
    <div className={`bg-primary/5 rounded-xl p-4 border transition-all ${
      hasFile ? "border-secondary/30" : optional ? "border-primary/10" : "border-warning/30"
    }`}>
      <div className="flex items-start justify-between mb-3">
        <div className={`w-10 h-10 rounded-lg flex items-center justify-center ${
          hasFile ? "bg-secondary/20" : "bg-primary/10"
        }`}>
          {hasFile ? (
            <FileCheck size={20} className="text-secondary" />
          ) : (
            <Map size={20} className="text-text-muted" />
          )}
        </div>
        <div className="text-right">
          <h4 className="font-bold text-text-primary">{title}</h4>
          <p className="text-xs text-text-muted">{description}</p>
        </div>
      </div>

      {hasFile && file && (
        <div className="bg-background-paper/50 rounded-lg p-3 mb-3 text-right space-y-1 border border-primary/5">
          <p className="text-xs text-text-secondary truncate" dir="ltr">{file.originalFileName}</p>
          <div className="flex items-center justify-between text-xs">
            <span className="text-secondary font-bold">{file.featuresCount} منطقة</span>
            <span className="text-text-muted">{file.fileSizeFormatted}</span>
          </div>
          <p className="text-xs text-text-muted">
            آخر تحديث: {new Date(file.uploadedAt).toLocaleDateString("ar-EG")}
          </p>
        </div>
      )}

      <label className="block">
        <input
          type="file"
          accept=".json,.geojson"
          disabled={isUploading}
          className="hidden"
          onChange={(e) => {
            if (e.target.files && e.target.files[0]) {
              onUpload(e.target.files[0]);
              e.target.value = "";
            }
          }}
        />
        <div className={`w-full h-10 rounded-lg border-2 border-dashed flex items-center justify-center gap-2 cursor-pointer transition-all ${
          isUploading
            ? "border-primary/50 bg-primary/10"
            : "border-primary/20 hover:border-primary/50 hover:bg-primary/5"
        }`}>
          {isUploading ? (
            <>
              <Loader2 size={16} className="animate-spin text-primary" />
              <span className="text-sm text-primary">جاري الرفع...</span>
            </>
          ) : (
            <>
              <Upload size={16} className="text-text-muted" />
              <span className="text-sm text-text-muted">{hasFile ? "تحديث الملف" : "رفع ملف"}</span>
            </>
          )}
        </div>
      </label>
    </div>
  );
}
