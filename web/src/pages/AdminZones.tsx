import { useEffect, useState } from "react";
import { getZones, createZone, updateZone, deleteZone } from "../api/zones";
import type { ZoneResponse as Zone } from "../types/zone";
import {
  getGisFilesStatus,
  uploadGisFile,
  getZonesSummary,
  type GisFilesStatus,
  type GisFileType,
  type ZonesSummary
} from "../api/gis";
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
  Globe2,
  Upload,
  FileCheck,
  RefreshCw,
  ChevronDown,
  ChevronUp
} from "lucide-react";
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

  // GIS Import states
  const [showGisImport, setShowGisImport] = useState(false);
  const [gisStatus, setGisStatus] = useState<GisFilesStatus | null>(null);
  const [zonesSummary, setZonesSummary] = useState<ZonesSummary | null>(null);
  const [uploadingType, setUploadingType] = useState<GisFileType | null>(null);

  // Form state
  const [formData, setFormData] = useState<Partial<Zone>>({
    zoneName: "",
    zoneCode: "",
    areaSquareMeters: 0,
    boundaryGeoJson: ""
  });

  useEffect(() => {
    fetchInitialData();
    fetchGisData();
  }, []);

  const fetchGisData = async () => {
    try {
      const [status, summary] = await Promise.all([
        getGisFilesStatus(),
        getZonesSummary()
      ]);
      setGisStatus(status);
      setZonesSummary(summary);
    } catch (err) {
      console.error("Failed to fetch GIS data", err);
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
        // Refresh data
        await Promise.all([fetchInitialData(), fetchGisData()]);
      } else {
        setMessage({ type: "error", text: result.message });
      }
    } catch (err: any) {
      setMessage({ type: "error", text: err.response?.data?.message || "فشل رفع الملف" });
    } finally {
      setUploadingType(null);
      setTimeout(() => setMessage(null), 5000);
    }
  };

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
      zoneName: "",
      zoneCode: "",
      areaSquareMeters: 0,
      boundaryGeoJson: ""
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
    const name = z.zoneName?.toLowerCase() || "";
    const code = z.zoneCode?.toLowerCase() || "";
    const search = searchTerm.toLowerCase();
    return name.includes(search) || code.includes(search);
  });

  // Parse GeoJSON to Leaflet polygon coordinates
  const getPreviewGeometry = (geometryJson?: string): [number, number][] | null => {
    if (!geometryJson) return null;
    try {
      const geo = JSON.parse(geometryJson);

      // Handle different GeoJSON formats
      let coordinates: number[][][] | null = null;

      if (geo.type === "Polygon") {
        coordinates = geo.coordinates;
      } else if (geo.type === "MultiPolygon") {
        // Use first polygon of MultiPolygon
        coordinates = geo.coordinates[0];
      } else if (geo.type === "Feature" && geo.geometry) {
        // Handle Feature wrapper
        if (geo.geometry.type === "Polygon") {
          coordinates = geo.geometry.coordinates;
        } else if (geo.geometry.type === "MultiPolygon") {
          coordinates = geo.geometry.coordinates[0];
        }
      } else if (geo.type === "FeatureCollection" && geo.features?.length > 0) {
        // Handle FeatureCollection - use first feature
        const firstFeature = geo.features[0];
        if (firstFeature.geometry?.type === "Polygon") {
          coordinates = firstFeature.geometry.coordinates;
        } else if (firstFeature.geometry?.type === "MultiPolygon") {
          coordinates = firstFeature.geometry.coordinates[0];
        }
      }

      if (coordinates && coordinates[0]) {
        // Convert [lng, lat] to [lat, lng] for Leaflet
        return coordinates[0].map((coord: number[]) => [coord[1], coord[0]] as [number, number]);
      }
    } catch (e) {
      console.error("Failed to parse GeoJSON:", e);
    }
    return null;
  };

  // Get center point of polygon for map centering
  const getPolygonCenter = (positions: [number, number][] | null): [number, number] | null => {
    if (!positions || positions.length === 0) return null;
    const sumLat = positions.reduce((sum, pos) => sum + pos[0], 0);
    const sumLng = positions.reduce((sum, pos) => sum + pos[1], 0);
    return [sumLat / positions.length, sumLng / positions.length];
  };

  if (loading) {
    return (
      <div className="h-full w-full bg-[#F3F1ED] flex items-center justify-center">
        <div className="flex flex-col items-center gap-4">
          <div className="w-12 h-12 border-4 border-[#7895B2]/30 border-t-[#7895B2] rounded-full animate-spin"></div>
          <p className="text-[#6B7280] font-medium">جاري تحميل المناطق...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1200px] mx-auto space-y-6">
          {/* Header (Corrected RTL Layout) */}
          <div className="flex flex-col md:flex-row items-center justify-between gap-6">
            <div className="text-right">
              <h1 className="font-sans font-black text-[28px] text-[#2F2F2F] tracking-tight">إدارة المناطق</h1>
              <p className="text-[14px] font-bold text-[#AFAFAF] mt-1">تقسيم البلدية لمناطق جغرافية لتعيين العمال وتتبعهم</p>
            </div>
            <button
              onClick={handleOpenAdd}
              className="flex items-center justify-center gap-3 bg-[#7895B2] hover:bg-[#647e99] text-white px-6 py-3.5 rounded-[16px] transition-all font-black shadow-lg shadow-[#7895B2]/20 group"
            >
              <div className="p-1.5 bg-white/20 rounded-lg group-hover:rotate-90 transition-transform">
                <Plus size={20} />
              </div>
              <span>إضافة منطقة جديدة</span>
            </button>
          </div>

          {/* GIS Import Section */}
          <div className="bg-white rounded-[16px] p-5 shadow-[0_4px_20px_rgba(0,0,0,0.04)] border border-[#8FA36A]/20">
            <button
              onClick={() => setShowGisImport(!showGisImport)}
              className="w-full flex items-center justify-between"
            >
              <div className="flex items-center gap-3">
                <div className="text-right">
                  <h3 className="text-lg font-black text-[#2F2F2F]">استيراد ملفات GIS</h3>
                  <p className="text-xs font-bold text-[#AFAFAF]">رفع ملفات GeoJSON لإنشاء المناطق تلقائياً</p>
                </div>
                <div className="p-3 bg-[#8FA36A]/10 rounded-[16px] text-[#8FA36A] shadow-sm">
                  <Upload size={22} />
                </div>
              </div>
              <div className="flex items-center gap-2 text-[#AFAFAF] bg-[#F9F8F6] px-4 py-1.5 rounded-full border border-black/5">
                <span className="text-[11px] font-black uppercase tracking-wider">{showGisImport ? "إخفاء" : "عرض"}</span>
                {showGisImport ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
              </div>
            </button>

            {showGisImport && (
              <div className="mt-6 pt-6 border-t border-[#E5E7EB]">
                {/* Zones Summary */}
                {zonesSummary && (
                  <div className="bg-[#F3F1ED] border border-[#E5E7EB] rounded-[12px] p-4 mb-6 flex items-center justify-between">
                    <div className="flex items-center gap-3 flex-wrap">
                      {zonesSummary.byType.map((t) => (
                        <span key={t.type} className="text-xs bg-[#8FA36A]/10 border border-[#8FA36A]/20 px-3 py-1 rounded-full text-[#2F2F2F] font-bold">
                          {t.type}: {t.count}
                        </span>
                      ))}
                    </div>
                    <div className="flex items-center gap-2 text-[#8FA36A]">
                      <span className="font-bold">{zonesSummary.totalZones} منطقة مسجلة</span>
                      <Layers size={20} />
                    </div>
                  </div>
                )}

                {/* GIS Upload Cards */}
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  {/* Quarters */}
                  <div className="bg-[#F3F1ED] border border-[#E5E7EB] rounded-[12px] p-4">
                    <div className="flex items-center justify-between mb-3">
                      <div className="text-right">
                        <h4 className="font-bold text-[#2F2F2F]">الأحياء (Quarters)</h4>
                        <p className="text-xs text-[#6B7280]">حدود الأحياء والمناطق السكنية</p>
                      </div>
                      <MapIcon size={20} className="text-[#7895B2]" />
                    </div>
                    {gisStatus?.quarters?.uploadedAt ? (
                      <div className="flex items-center gap-2 text-[#8FA36A] text-xs mb-3">
                        <FileCheck size={14} />
                        <span>تم الرفع: {new Date(gisStatus.quarters.uploadedAt).toLocaleDateString('ar-EG')}</span>
                      </div>
                    ) : null}
                    <label className={`flex items-center justify-center gap-2 px-4 py-2 rounded-[10px] cursor-pointer transition-all text-sm font-bold ${uploadingType === "Quarters" ? "bg-[#7895B2]/20 text-[#7895B2]" : "bg-[#7895B2]/10 text-[#7895B2] hover:bg-[#7895B2]/20"}`}>
                      {uploadingType === "Quarters" ? (
                        <>
                          <RefreshCw size={16} className="animate-spin" />
                          جاري الرفع...
                        </>
                      ) : (
                        <>
                          <Upload size={16} />
                          رفع ملف
                        </>
                      )}
                      <input
                        type="file"
                        accept=".json,.geojson"
                        className="hidden"
                        disabled={uploadingType !== null}
                        onChange={(e) => e.target.files?.[0] && handleGisUpload(e.target.files[0], "Quarters")}
                      />
                    </label>
                  </div>

                  {/* Borders */}
                  <div className="bg-[#F3F1ED] border border-[#E5E7EB] rounded-[12px] p-4">
                    <div className="flex items-center justify-between mb-3">
                      <div className="text-right">
                        <h4 className="font-bold text-[#2F2F2F]">الحدود (Borders)</h4>
                        <p className="text-xs text-[#6B7280]">حدود البلدية الخارجية</p>
                      </div>
                      <MapIcon size={20} className="text-[#F5B300]" />
                    </div>
                    {gisStatus?.borders?.uploadedAt ? (
                      <div className="flex items-center gap-2 text-[#8FA36A] text-xs mb-3">
                        <FileCheck size={14} />
                        <span>تم الرفع: {new Date(gisStatus.borders.uploadedAt).toLocaleDateString('ar-EG')}</span>
                      </div>
                    ) : null}
                    <label className={`flex items-center justify-center gap-2 px-4 py-2 rounded-[10px] cursor-pointer transition-all text-sm font-bold ${uploadingType === "Borders" ? "bg-[#F5B300]/20 text-[#D4A100]" : "bg-[#F5B300]/10 text-[#D4A100] hover:bg-[#F5B300]/20"}`}>
                      {uploadingType === "Borders" ? (
                        <>
                          <RefreshCw size={16} className="animate-spin" />
                          جاري الرفع...
                        </>
                      ) : (
                        <>
                          <Upload size={16} />
                          رفع ملف
                        </>
                      )}
                      <input
                        type="file"
                        accept=".json,.geojson"
                        className="hidden"
                        disabled={uploadingType !== null}
                        onChange={(e) => e.target.files?.[0] && handleGisUpload(e.target.files[0], "Borders")}
                      />
                    </label>
                  </div>

                  {/* Blocks */}
                  <div className="bg-[#F3F1ED] border border-[#E5E7EB] rounded-[12px] p-4">
                    <div className="flex items-center justify-between mb-3">
                      <div className="text-right">
                        <h4 className="font-bold text-[#2F2F2F]">البلوكات (Blocks)</h4>
                        <p className="text-xs text-[#6B7280]">تقسيمات البلوكات (اختياري)</p>
                      </div>
                      <MapIcon size={20} className="text-[#C86E5D]" />
                    </div>
                    {gisStatus?.blocks?.uploadedAt ? (
                      <div className="flex items-center gap-2 text-[#8FA36A] text-xs mb-3">
                        <FileCheck size={14} />
                        <span>تم الرفع: {new Date(gisStatus.blocks.uploadedAt).toLocaleDateString('ar-EG')}</span>
                      </div>
                    ) : null}
                    <label className={`flex items-center justify-center gap-2 px-4 py-2 rounded-[10px] cursor-pointer transition-all text-sm font-bold ${uploadingType === "Blocks" ? "bg-[#C86E5D]/20 text-[#C86E5D]" : "bg-[#C86E5D]/10 text-[#C86E5D] hover:bg-[#C86E5D]/20"}`}>
                      {uploadingType === "Blocks" ? (
                        <>
                          <RefreshCw size={16} className="animate-spin" />
                          جاري الرفع...
                        </>
                      ) : (
                        <>
                          <Upload size={16} />
                          رفع ملف
                        </>
                      )}
                      <input
                        type="file"
                        accept=".json,.geojson"
                        className="hidden"
                        disabled={uploadingType !== null}
                        onChange={(e) => e.target.files?.[0] && handleGisUpload(e.target.files[0], "Blocks")}
                      />
                    </label>
                  </div>
                </div>

                <p className="text-xs text-[#6B7280] mt-4 text-right">
                  * يجب رفع ملفات GeoJSON (.json أو .geojson). الحد الأقصى للملف: 10MB. المناطق المستوردة ستظهر في القائمة أدناه.
                </p>
              </div>
            )}
          </div>

          {/* Search */}
          <div className="bg-white rounded-[24px] p-5 shadow-[0_4px_25px_rgba(0,0,0,0.03)] border border-black/5">
            <div className="relative">
              <input
                type="text"
                placeholder="بحث عن منطقة بالاسم أو الكود..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full h-[52px] pr-12 pl-6 bg-[#F9F8F6] rounded-[18px] text-right text-[15px] font-black text-[#2F2F2F] placeholder:text-[#AFAFAF] border-0 outline-none focus:ring-4 focus:ring-[#7895B2]/10 transition-all"
              />
              <Search className="absolute right-4 top-1/2 -translate-y-1/2 text-[#AFAFAF]" size={20} />
            </div>
          </div>

          {/* Message */}
          {message && (
            <div className={`p-4 rounded-[12px] flex items-center justify-end gap-3 text-right ${
              message.type === "success" ? "bg-[#8FA36A]/10 text-[#8FA36A] border border-[#8FA36A]/20" : "bg-[#C86E5D]/10 text-[#C86E5D] border border-[#C86E5D]/20"
            }`}>
              <span className="font-semibold">{message.text}</span>
              {message.type === "success" ? <CheckCircle size={20} /> : <AlertCircle size={20} />}
            </div>
          )}

          {/* Zones Grid */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
            {filteredZones.map((zone) => (
              <div key={zone.zoneId} className="bg-white rounded-[16px] shadow-[0_4px_20px_rgba(0,0,0,0.04)] overflow-hidden group hover:shadow-[0_8px_30px_rgba(0,0,0,0.08)] transition-all">
                <div className="h-40 bg-[#F3F1ED] relative border-b border-[#E5E7EB]">
                  {(() => {
                    const geometry = getPreviewGeometry(zone.boundaryGeoJson);
                    if (geometry && geometry.length > 0) {
                      const center = getPolygonCenter(geometry) || mapCenter;
                      return (
                        <MapContainer center={center} zoom={14} zoomControl={false} dragging={false} scrollWheelZoom={false} className="h-full w-full z-0 opacity-60 grayscale group-hover:grayscale-0 group-hover:opacity-100 transition-all duration-500">
                          <ChangeView center={center} zoom={14} />
                          <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" />
                          <Polygon
                            positions={geometry}
                            pathOptions={{ color: '#7895B2', fillColor: '#7895B2', fillOpacity: 0.3, weight: 2 }}
                          />
                        </MapContainer>
                      );
                    }
                    return (
                      <div className="h-full w-full flex items-center justify-center opacity-10">
                        <MapIcon size={48} className="text-[#7895B2]" />
                      </div>
                    );
                  })()}
                  <div className="absolute top-3 right-3 px-3 py-1 bg-white/90 backdrop-blur-sm rounded-[8px] text-[#2F2F2F] text-xs font-sans font-bold tracking-wider border border-[#E5E7EB] shadow-sm">
                    {zone.zoneCode}
                  </div>
                </div>
                <div className="p-5">
                  <div className="flex items-start justify-between mb-4">
                    <div className="text-right">
                      <h3 className="font-black text-[#2F2F2F] text-[16px] tracking-tight">{zone.zoneName}</h3>
                    </div>
                    <div className="flex gap-2">
                       <button
                        onClick={() => handleOpenEdit(zone)}
                        className="w-9 h-9 bg-[#F3F1ED] text-[#7895B2] rounded-xl flex items-center justify-center hover:bg-[#7895B2] hover:text-white transition-all border border-black/5"
                      >
                        <Edit2 size={14} />
                      </button>
                      <button
                        onClick={() => handleDelete(zone.zoneId)}
                        className="w-9 h-9 bg-[#C86E5D]/10 text-[#C86E5D] rounded-xl flex items-center justify-center hover:bg-[#C86E5D] hover:text-white transition-all border border-black/5"
                      >
                        <Trash2 size={14} />
                      </button>
                    </div>
                  </div>

                  <div className="flex items-center justify-end gap-4 text-xs font-semibold text-[#6B7280] border-t border-[#E5E7EB] pt-4">
                    <div className="flex items-center gap-1">
                      <span>{(zone.areaSquareMeters / 1000000).toFixed(2)} كم²</span>
                      <Layers size={14} className="opacity-60 text-[#7895B2]" />
                    </div>
                    <div className="flex items-center gap-1">
                      <span>{zone.boundaryGeoJson ? 'معرف جغرافياً' : 'غير معرف'}</span>
                      <Globe2 size={14} className="opacity-60 text-[#8FA36A]" />
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {filteredZones.length === 0 && (
            <div className="bg-white rounded-[16px] p-8 shadow-[0_4px_20px_rgba(0,0,0,0.04)] text-center flex flex-col items-center">
              <MapIcon size={48} className="text-[#7895B2]/20 mb-4" />
              <p className="text-[#6B7280] font-medium">لا توجد مناطق مطابقة للبحث</p>
            </div>
          )}
        </div>
      </div>

      {/* Add/Edit Modal */}
      {showModal && (
        <div className="fixed inset-0 bg-black/50 backdrop-blur-sm flex items-center justify-center p-4 z-50 overflow-y-auto">
          <div className="bg-white rounded-[16px] w-full max-w-4xl my-8 shadow-2xl overflow-hidden">
            <div className="p-6 border-b border-[#E5E7EB] flex items-center justify-between">
              <button onClick={() => setShowModal(false)} className="p-2 hover:bg-[#F3F1ED] rounded-full transition-colors text-[#2F2F2F]">
                <X size={20} />
              </button>
              <div className="flex items-center gap-3">
                <div className="text-right">
                  <h3 className="text-xl font-bold text-[#2F2F2F]">{editingZone ? 'تعديل بيانات المنطقة' : 'إضافة منطقة جديدة'}</h3>
                  <p className="text-xs text-[#6B7280] mt-1">قم بتعيين البيانات الجغرافية والإدارية للمنطقة</p>
                </div>
                <div className="p-3 bg-[#7895B2]/10 rounded-[12px] text-[#7895B2]">
                  <Layers size={24} />
                </div>
              </div>
            </div>

            <div className="p-6 max-h-[80vh] overflow-y-auto">
              <form onSubmit={handleSubmit} className="space-y-6">
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                  <div className="space-y-4">
                    <div className="space-y-3 text-right">
                      <label className="text-sm font-semibold text-[#6B7280]">اسم المنطقة</label>
                      <input
                        required
                        type="text"
                        value={formData.zoneName || ""}
                        onChange={e => setFormData({...formData, zoneName: e.target.value})}
                        className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                        placeholder="مثال: منطقة البلدة القديمة"
                      />
                    </div>
                    <div className="space-y-3 text-right">
                      <label className="text-sm font-semibold text-[#6B7280]">كود المنطقة</label>
                      <input
                        required
                        type="text"
                        value={formData.zoneCode || ""}
                        onChange={e => setFormData({...formData, zoneCode: e.target.value.toUpperCase()})}
                        className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] font-sans border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                        placeholder="Z01"
                      />
                    </div>
                    <div className="space-y-3 text-right">
                      <label className="text-sm font-semibold text-[#6B7280]">المساحة التقديرية (كم²)</label>
                      <input
                        type="number"
                        step="0.01"
                        value={(formData.areaSquareMeters || 0) / 1000000}
                        onChange={e => setFormData({...formData, areaSquareMeters: parseFloat(e.target.value) * 1000000})}
                        className="w-full h-[46px] px-4 bg-[#F3F1ED] rounded-[12px] text-right text-[14px] text-[#2F2F2F] border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                      />
                    </div>
                    <div className="space-y-3 text-right">
                      <label className="text-sm font-semibold text-[#6B7280]">بيانات الـ GeoJSON (إحداثيات المنطقة)</label>
                      <textarea
                        value={formData.boundaryGeoJson || ""}
                        onChange={e => setFormData({...formData, boundaryGeoJson: e.target.value})}
                        className="w-full h-32 p-4 bg-[#F3F1ED] rounded-[12px] text-left text-[12px] text-[#2F2F2F] font-sans leading-relaxed border-0 outline-none focus:ring-2 focus:ring-[#7895B2]/30"
                        placeholder='{"type": "Polygon", "coordinates": [[...]]}'
                      />
                    </div>
                  </div>

                  <div className="space-y-3">
                    <label className="text-sm font-semibold text-[#6B7280] text-right block mb-2">معاينة المنطقة على الخريطة</label>
                    <div className="rounded-[16px] overflow-hidden border border-[#E5E7EB] h-[400px] relative shadow-inner bg-[#F3F1ED]">
                      <MapContainer center={mapCenter} zoom={11} className="h-full w-full">
                        <TileLayer url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png" className="opacity-80" />
                        <Polygon
                          positions={getPreviewGeometry(formData.boundaryGeoJson) || []}
                          pathOptions={{ color: '#7895B2', fillColor: '#7895B2', fillOpacity: 0.3, weight: 2 }}
                        />
                        <ChangeView center={getPreviewGeometry(formData.boundaryGeoJson)?.[0] || mapCenter} zoom={13} />
                      </MapContainer>
                      <div className="absolute top-4 left-4 z-[1000]">
                        <button type="button" className="p-2 bg-white/80 backdrop-blur-sm border border-[#E5E7EB] rounded-[8px] shadow-sm hover:bg-white text-[#2F2F2F] transition-colors">
                          <Maximize2 size={18} />
                        </button>
                      </div>
                    </div>
                    <p className="text-[10px] text-[#6B7280] text-center mt-2 italic opacity-60">* المعاينة تعتمد على صحة كود GeoJSON المدخل يدوياً في هذا الإصدار</p>
                  </div>
                </div>

                <div className="flex gap-4 pt-6 border-t border-[#E5E7EB]">
                  <button
                    type="button"
                    onClick={() => setShowModal(false)}
                    className="flex-1 h-[46px] rounded-[12px] border border-[#E5E7EB] text-[#2F2F2F] hover:bg-[#F3F1ED] transition-all font-semibold"
                  >
                    إلغاء
                  </button>
                  <button
                    type="submit"
                    disabled={actionLoading === "submit"}
                    className="flex-1 h-[46px] rounded-[12px] bg-[#7895B2] hover:bg-[#6785A2] text-white transition-all font-semibold shadow-lg disabled:opacity-50"
                  >
                    {actionLoading === "submit" ? "جاري الحفظ..." : 'حفظ بيانات المنطقة'}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
