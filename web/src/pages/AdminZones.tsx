import { useEffect, useState } from "react";
import { getZones, createZone, updateZone, deleteZone } from "../api/municipality";
import type { Zone } from "../api/municipality";
import { MapContainer, TileLayer, Polygon, useMap } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import {
  Map as MapIcon,
  Plus,
  Search,
  CheckCircle,
  AlertCircle,
  X,
  Edit2,
  Trash2,
  Maximize2,
  Layers,
  Globe2
} from "lucide-react";
import GlassCard from "../components/UI/GlassCard";
import { useMunicipality } from "../contexts/MunicipalityContext";

// Helper to center map
function ChangeView({ center, zoom }: { center: [number, number], zoom: number }) {
  const map = useMap();
  map.setView(center, zoom);
  return null;
}

export default function AdminZones() {
  const [zones, setZones] = useState<Zone[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [actionLoading, setActionLoading] = useState<number | string | null>(null);
  const [message, setMessage] = useState<{ type: "success" | "error"; text: string } | null>(null);

  // Get map center from municipality settings
  const { mapCenter } = useMunicipality();

  // Modal states
  const [showModal, setShowModal] = useState(false);
  const [editingZone, setEditingZone] = useState<Zone | null>(null);

  // Form state
  const [formData, setFormData] = useState<Partial<Zone>>({
    name: "",
    code: "",
    municipalityId: 1, // Default to 1 for generic single-tenant
    areaSizeKm2: 0,
    geometryJson: ""
  });

  useEffect(() => {
    fetchInitialData();
  }, []);

  const fetchInitialData = async () => {
    try {
      setLoading(true);
      const zonesData = await getZones();
      setZones(zonesData);
    } catch (err) {
      console.error("Failed to fetch data", err);
      setMessage({ type: "error", text: "فشل تحميل البيانات" });
    } finally {
      setLoading(false);
    }
  };

  const handleOpenAdd = () => {
    setEditingZone(null);
    setFormData({
      name: "",
      code: "",
      municipalityId: 1, // Default ID
      areaSizeKm2: 0,
      geometryJson: ""
    });
    setShowModal(true);
  };

  const handleOpenEdit = (zone: Zone) => {
    setEditingZone(zone);
    setFormData(zone);
    setShowModal(true);
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm("هل أنت متأكد من حذف هذه المنطقة؟")) return;
    setActionLoading(id);
    try {
      await deleteZone(id);
      setMessage({ type: "success", text: "تم حذف المنطقة بنجاح" });
      fetchInitialData();
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      setMessage({ type: "error", text: "فشل حذف المنطقة" });
    } finally {
      setActionLoading(null);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setActionLoading("submit");
    try {
      if (editingZone) {
        await updateZone(editingZone.zoneId, formData);
        setMessage({ type: "success", text: "تم تحديث المنطقة بنجاح" });
      } else {
        await createZone(formData);
        setMessage({ type: "success", text: "تم إضافة المنطقة بنجاح" });
      }
      setShowModal(false);
      fetchInitialData();
      setTimeout(() => setMessage(null), 3000);
    } catch (err) {
      setMessage({ type: "error", text: "فشل حفظ بيانات المنطقة" });
    } finally {
      setActionLoading(null);
    }
  };

  const filteredZones = zones.filter(z => {
    return z.name.toLowerCase().includes(searchTerm.toLowerCase()) || z.code.toLowerCase().includes(searchTerm.toLowerCase());
  });

  // Mock geometry for preview if empty
  const getPreviewGeometry = (geometryJson?: string) => {
    if (!geometryJson) return null;
    try {
      const geo = JSON.parse(geometryJson);
      if (geo.type === "Polygon") {
        return geo.coordinates[0].map((coord: any) => [coord[1], coord[0]]); // Leaflet uses [lat, lng]
      }
    } catch (e) {}
    return null;
  };

  if (loading) {
    return (
      <div className="h-full w-full flex items-center justify-center">
         <div className="flex flex-col items-center gap-4">
             <div className="w-12 h-12 border-4 border-primary/30 border-t-primary rounded-full animate-spin"></div>
             <p className="text-text-secondary font-medium">جاري تحديث المناطق...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-8 pb-10">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 animate-fade-in">
          <div className="flex flex-col items-end">
            <h1 className="text-3xl font-extrabold text-text-primary">
                إدارة المناطق
            </h1>
            <p className="text-right text-text-secondary mt-2 font-medium">تقسيم البلدية لمناطق جغرافية لتعيين العمال وتتبعهم</p>
          </div>
          
          <button 
            onClick={handleOpenAdd}
            className="flex items-center gap-2 bg-primary hover:bg-primary-dark text-white px-6 py-3 rounded-xl transition-all shadow-lg shadow-primary/20 hover:shadow-primary/40 active:transform active:scale-95 font-bold"
          >
            <Plus size={20} />
            إضافة منطقة جديدة
          </button>
        </div>

        <GlassCard className="flex flex-col sm:flex-row-reverse gap-4">
            <div className="flex-1 relative group">
                <input
                    type="text"
                    placeholder="بحث عن منطقة بالاسم أو الكود..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="glass-input w-full h-12 pr-11 text-right focus:bg-primary/5 text-text-primary"
                />
                <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-text-muted group-focus-within:text-primary transition-colors" size={20} />
            </div>
        </GlassCard>

        {message && (
          <div className={`p-4 rounded-xl flex items-center justify-end gap-3 text-right animate-fade-in ${
            message.type === "success" ? "bg-secondary/10 text-secondary border border-secondary/20" : "bg-accent/10 text-accent border border-accent/20"
          }`}>
            <span className="font-bold">{message.text}</span>
            {message.type === "success" ? <CheckCircle size={20} /> : <AlertCircle size={20} />}
          </div>
        )}

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {filteredZones.map((zone, idx) => (
                <GlassCard key={zone.zoneId} variant="hover" noPadding className="relative overflow-hidden group animate-slide-up" style={{ animationDelay: `${idx * 100}ms` }}>
                    <div className="h-40 bg-primary/5 relative border-b border-primary/5">
                        {(() => {
                            const geometry = getPreviewGeometry(zone.geometryJson);
                            if (geometry && geometry.length > 0) {
                                return (
                                   <MapContainer center={mapCenter} zoom={11} zoomControl={false} dragging={false} scrollWheelZoom={false} className="h-full w-full z-0 opacity-60 grayscale group-hover:grayscale-0 group-hover:opacity-100 transition-all duration-500">
                                       <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
                                       <Polygon 
                                            positions={geometry} 
                                            pathOptions={{ color: '#0F5A3E', fillColor: '#0F5A3E', fillOpacity: 0.3, weight: 2 }} 
                                        />
                                   </MapContainer>
                                );
                            }
                            return (
                                <div className="h-full w-full flex items-center justify-center opacity-10">
                                    <MapIcon size={48} className="text-primary" />
                                </div>
                            );
                        })()}
                        <div className="absolute top-3 right-3 px-3 py-1 bg-white/80 backdrop-blur-md rounded-lg text-text-primary text-xs font-mono font-bold tracking-wider border border-primary/10 shadow-lg">
                            {zone.code}
                        </div>
                    </div>
                    <div className="p-5">
                        <div className="flex items-start justify-between mb-4">
                            <div className="flex gap-1.5">
                                <button onClick={() => handleOpenEdit(zone)} className="p-2 text-text-muted hover:text-primary hover:bg-primary/10 rounded-lg transition-colors"><Edit2 size={16} /></button>
                                <button onClick={() => handleDelete(zone.zoneId)} className="p-2 text-text-muted hover:text-accent hover:bg-accent/10 rounded-lg transition-colors"><Trash2 size={16} /></button>
                            </div>
                            <div className="text-right">
                                <h3 className="font-extrabold text-text-primary text-lg">{zone.name}</h3>
                            </div>
                        </div>

                        <div className="flex items-center justify-end gap-4 text-xs font-bold text-text-secondary border-t border-primary/5 pt-4">
                             <div className="flex items-center gap-1">
                                <span>{zone.areaSizeKm2} كم²</span>
                                <Layers size={14} className="opacity-60 text-primary" />
                            </div>
                             <div className="flex items-center gap-1">
                                <span>{zone.geometryJson ? 'معرف جغرافياً' : 'غير معرف'}</span>
                                <Globe2 size={14} className="opacity-60 text-secondary" />
                            </div>
                        </div>
                    </div>
                </GlassCard>
            ))}
        </div>

        {filteredZones.length === 0 && (
            <GlassCard className="py-16 text-center flex flex-col items-center">
              <MapIcon size={64} className="text-primary/10 mb-4" />
              <p className="text-text-muted text-lg font-medium">لا توجد مناطق مطابقة للبحث</p>
            </GlassCard>
        )}

      {/* Add/Edit Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 z-50 animate-fade-in overflow-y-auto">
          <GlassCard variant="panel" className="w-full max-w-4xl !bg-background-paper my-8 !p-0 overflow-hidden shadow-2xl !border-primary/10">
             <div className="p-6 border-b border-primary/5 flex items-center justify-between">
                <button onClick={() => setShowModal(false)} className="p-2 hover:bg-primary/5 rounded-full transition-colors text-text-primary">
                    <X size={20} />
                </button>
                <div className="flex items-center gap-3">
                    <div className="text-right">
                        <h3 className="text-2xl font-bold text-text-primary max-w-lg truncate">{editingZone ? 'تعديل بيانات المنطقة' : 'إضافة منطقة جديدة'}</h3>
                        <p className="text-xs text-text-secondary mt-1">قم بتعيين البيانات الجغرافية والإدارية للمنطقة</p>
                    </div>
                    <div className="p-3 bg-primary/10 rounded-2xl text-primary">
                        <Layers size={24} />
                    </div>
                </div>
            </div>

            <div className="p-6 max-h-[80vh] overflow-y-auto custom-scrollbar">
                <form onSubmit={handleSubmit} className="space-y-8">
                    <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                        <div className="space-y-6">
                            <div className="space-y-2 text-right">
                                <label className="text-sm font-bold text-text-muted">اسم المنطقة</label>
                                <input required type="text" value={formData.name} onChange={e => setFormData({...formData, name: e.target.value})} className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary focus:bg-primary/10 transition-colors" placeholder="مثال: منطقة البلدة القديمة" />
                            </div>
                            <div className="space-y-2 text-right">
                                <label className="text-sm font-bold text-text-muted">كود المنطقة</label>
                                <input required type="text" value={formData.code} onChange={e => setFormData({...formData, code: e.target.value.toUpperCase()})} className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary font-mono focus:bg-primary/10 transition-colors" placeholder="Z01" />
                            </div>
                            {/* Municipality Select Removed - implicitly linked */}
                            <div className="space-y-2 text-right">
                                <label className="text-sm font-bold text-text-muted">المساحة التقديرية (كم²)</label>
                                <input type="number" step="0.01" value={formData.areaSizeKm2} onChange={e => setFormData({...formData, areaSizeKm2: parseFloat(e.target.value)})} className="glass-input w-full h-11 text-right !bg-primary/5 text-text-primary focus:bg-primary/10 transition-colors" />
                            </div>
                            <div className="space-y-2 text-right">
                                <label className="text-sm font-bold text-text-muted">بيانات الـ GeoJSON (إحداثيات المنطقة)</label>
                                <textarea value={formData.geometryJson} onChange={e => setFormData({...formData, geometryJson: e.target.value})} className="glass-input w-full h-32 p-4 text-left !bg-primary/5 text-text-primary font-mono text-xs leading-relaxed focus:bg-primary/10 transition-colors" placeholder='{"type": "Polygon", "coordinates": [[...]]}' />
                            </div>
                        </div>

                        <div className="space-y-2">
                            <label className="text-sm font-bold text-text-muted text-right block mb-2">معاينة المنطقة على الخريطة</label>
                            <div className="rounded-[24px] overflow-hidden border border-primary/10 h-[400px] relative shadow-inner bg-primary/5">
                                <MapContainer center={mapCenter} zoom={11} className="h-full w-full">
                                    <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" className="opacity-80" />
                                    <Polygon 
                                        positions={getPreviewGeometry(formData.geometryJson) || []} 
                                        pathOptions={{ color: '#0F5A3E', fillColor: '#0F5A3E', fillOpacity: 0.3, weight: 2 }} 
                                    />
                                    <ChangeView center={getPreviewGeometry(formData.geometryJson)?.[0] || mapCenter} zoom={13} />
                                </MapContainer>
                                <div className="absolute top-4 left-4 z-[1000]">
                                    <button type="button" className="p-2 bg-white/50 backdrop-blur-md border border-primary/10 rounded-lg shadow-md hover:bg-white/70 text-text-primary transition-colors"><Maximize2 size={18} /></button>
                                </div>
                            </div>
                            <p className="text-[10px] text-text-muted text-center mt-2 italic opacity-60">* المعاينة تعتمد على صحة كود GeoJSON المدخل يدوياً في هذا الإصدار</p>
                        </div>
                    </div>

                    <div className="flex gap-4 pt-8 border-t border-primary/5">
                        <button type="button" onClick={() => setShowModal(false)} className="flex-1 h-12 rounded-xl border border-primary/10 text-text-primary hover:bg-primary/5 transition-all font-bold">إلغاء</button>
                        <button type="submit" disabled={actionLoading === "submit"} className="flex-1 h-12 rounded-xl bg-primary hover:bg-primary-dark text-white transition-all font-bold shadow-lg shadow-primary/20 disabled:opacity-50">
                            {actionLoading === "submit" ? "جاري الحفظ..." : 'حفظ بيانات المنطقة'}
                        </button>
                    </div>
                </form>
            </div>
          </GlassCard>
        </div>
      )}
    </div>
  );
}
