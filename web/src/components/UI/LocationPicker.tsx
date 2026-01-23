import { useState, useEffect, useCallback } from "react";
import { MapContainer, TileLayer, Marker, useMapEvents, useMap } from "react-leaflet";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { Search, MapPin, Crosshair } from "lucide-react";
import { useMunicipality } from "../../contexts/MunicipalityContext";

// Fix Leaflet default icon issue
import markerIcon from "leaflet/dist/images/marker-icon.png";
import markerShadow from "leaflet/dist/images/marker-shadow.png";

const DefaultIcon = L.icon({
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
  iconSize: [25, 41],
  iconAnchor: [12, 41],
});
L.Marker.prototype.options.icon = DefaultIcon;

// Custom red marker for selected location
const SelectedIcon = L.divIcon({
  className: "custom-marker",
  html: `<div class="relative">
    <div class="w-8 h-8 bg-red-500 rounded-full border-4 border-white shadow-lg flex items-center justify-center">
      <div class="w-2 h-2 bg-white rounded-full"></div>
    </div>
    <div class="absolute -bottom-2 left-1/2 transform -translate-x-1/2 w-0 h-0 border-l-4 border-r-4 border-t-8 border-l-transparent border-r-transparent border-t-red-500"></div>
  </div>`,
  iconSize: [32, 40],
  iconAnchor: [16, 40],
});

interface LocationPickerProps {
  latitude: number | null;
  longitude: number | null;
  onLocationChange: (lat: number, lng: number) => void;
  onLocationNameChange?: (name: string) => void; // Called when a search result is selected
  disabled?: boolean;
}

// Component to handle map click events
function MapClickHandler({ onLocationSelect, onMapClick }: { onLocationSelect: (lat: number, lng: number) => void; onMapClick?: () => void }) {
  useMapEvents({
    click: (e) => {
      onMapClick?.(); // Reset zoom behavior when clicking
      onLocationSelect(e.latlng.lat, e.latlng.lng);
    },
  });
  return null;
}

// Component to recenter and zoom map
function RecenterMap({ center, zoomLevel }: { center: [number, number]; zoomLevel?: number }) {
  const map = useMap();
  useEffect(() => {
    // If zoomLevel is provided, use it; otherwise keep current zoom but ensure at least 16
    const targetZoom = zoomLevel ?? Math.max(map.getZoom(), 16);
    map.flyTo(center, targetZoom, { duration: 0.5 });
  }, [center, map, zoomLevel]);
  return null;
}

export default function LocationPicker({
  latitude,
  longitude,
  onLocationChange,
  onLocationNameChange,
  disabled = false,
}: LocationPickerProps) {
  const [searchQuery, setSearchQuery] = useState("");
  const [searchResults, setSearchResults] = useState<any[]>([]);
  const [searching, setSearching] = useState(false);
  const [showResults, setShowResults] = useState(false);
  const [targetZoom, setTargetZoom] = useState<number | undefined>(undefined);

  // Get municipality settings for default center
  const { mapCenter: defaultCenter, municipalityName } = useMunicipality();
  const center: [number, number] = latitude && longitude ? [latitude, longitude] : defaultCenter;

  // Search for location using Nominatim (OpenStreetMap)
  const searchLocation = useCallback(async () => {
    if (!searchQuery.trim()) return;

    setSearching(true);
    try {
      // Add municipality name to improve search results for local streets
      const searchWithContext = searchQuery.includes(municipalityName)
        ? searchQuery
        : `${searchQuery}, ${municipalityName}، فلسطين`;

      const response = await fetch(
        `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(searchWithContext)}&limit=8&accept-language=ar`
      );
      const data = await response.json();

      // If no results, try without the context
      if (data.length === 0) {
        const fallbackResponse = await fetch(
          `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(searchQuery)}&limit=8&accept-language=ar`
        );
        const fallbackData = await fallbackResponse.json();
        setSearchResults(fallbackData);
      } else {
        setSearchResults(data);
      }
      setShowResults(true);
    } catch (error) {
      console.error("Search failed:", error);
    } finally {
      setSearching(false);
    }
  }, [searchQuery]);

  // Handle search on Enter key
  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === "Enter") {
      e.preventDefault();
      searchLocation();
    }
  };

  // Select a search result
  const selectResult = (result: any) => {
    const lat = parseFloat(result.lat);
    const lng = parseFloat(result.lon);
    setTargetZoom(18); // Zoom in close when selecting from search
    onLocationChange(lat, lng);

    // Get a clean location name for the description
    const locationName = result.display_name.split(",").slice(0, 3).join("،"); // Take first 3 parts
    setSearchQuery(result.display_name.split(",")[0]);
    setShowResults(false);

    // Auto-fill the location description field
    onLocationNameChange?.(locationName);
  };

  // Get current location from browser
  const getCurrentLocation = () => {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          setTargetZoom(18); // Zoom in close when using current location
          onLocationChange(position.coords.latitude, position.coords.longitude);
        },
        (error) => {
          console.error("Geolocation error:", error);
          alert("لم نتمكن من تحديد موقعك الحالي");
        }
      );
    } else {
      alert("المتصفح لا يدعم تحديد الموقع");
    }
  };

  return (
    <div className="space-y-3">
      {/* Search Bar */}
      <div className="relative z-[1000]">
        <div className="flex gap-2">
          <div className="flex-1 relative">
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              onKeyDown={handleKeyDown}
              placeholder={`ابحث عن موقع... (مثال: شارع الرئيسي، ${municipalityName})`}
              disabled={disabled}
              className="w-full h-[44px] rounded-[10px] bg-[#F3F1ED] border border-black/10 px-4 pr-10 text-right outline-none focus:ring-2 focus:ring-black/10 disabled:opacity-50"
            />
            <Search className="absolute right-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-400" />
          </div>
          <button
            type="button"
            onClick={searchLocation}
            disabled={disabled || searching}
            className="px-4 h-[44px] rounded-[10px] bg-[#60778E] text-white hover:bg-[#4a5d70] disabled:opacity-50"
          >
            {searching ? "..." : "بحث"}
          </button>
          <button
            type="button"
            onClick={getCurrentLocation}
            disabled={disabled}
            title="استخدم موقعي الحالي"
            className="px-3 h-[44px] rounded-[10px] bg-[#F3F1ED] border border-black/10 hover:bg-[#e5e3de] disabled:opacity-50"
          >
            <Crosshair className="w-5 h-5 text-[#60778E]" />
          </button>
        </div>

        {/* Search Results Dropdown */}
        {showResults && searchResults.length > 0 && (
          <div className="absolute z-[1001] w-full mt-1 bg-white border border-gray-200 rounded-[10px] shadow-lg max-h-60 overflow-auto">
            {searchResults.map((result, index) => (
              <button
                key={index}
                type="button"
                onClick={() => selectResult(result)}
                className="w-full px-4 py-3 text-right hover:bg-gray-100 border-b border-gray-100 last:border-0"
              >
                <div className="flex items-start gap-2">
                  <MapPin className="w-4 h-4 text-red-500 mt-1 flex-shrink-0" />
                  <span className="text-sm text-gray-700">{result.display_name}</span>
                </div>
              </button>
            ))}
          </div>
        )}
      </div>

      {/* Map */}
      <div className="relative rounded-[10px] overflow-hidden border border-black/10" style={{ height: "300px" }}>
        <MapContainer
          center={center}
          zoom={15}
          style={{ height: "100%", width: "100%" }}
          scrollWheelZoom={true}
        >
          <TileLayer
            attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
            url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
          />

          {!disabled && <MapClickHandler onLocationSelect={onLocationChange} onMapClick={() => setTargetZoom(undefined)} />}

          {latitude && longitude && (
            <>
              <Marker position={[latitude, longitude]} icon={SelectedIcon} />
              <RecenterMap center={[latitude, longitude]} zoomLevel={targetZoom} />
            </>
          )}
        </MapContainer>

        {/* Instructions overlay */}
        {!latitude && !longitude && (
          <div className="absolute inset-0 flex items-center justify-center bg-black/10 pointer-events-none">
            <div className="bg-white/90 px-4 py-2 rounded-lg text-sm text-gray-600">
              ابحث عن موقع أو انقر على الخريطة لتحديد الموقع
            </div>
          </div>
        )}

        {/* Hint when marker is placed */}
        {latitude && longitude && (
          <div className="absolute bottom-2 left-2 right-2 pointer-events-none">
            <div className="bg-white/90 px-3 py-1.5 rounded-lg text-xs text-gray-600 text-center">
              💡 انقر على الخريطة لتحديد موقع أدق
            </div>
          </div>
        )}
      </div>

      {/* Selected Coordinates */}
      {latitude && longitude && (
        <div className="flex justify-between items-center text-sm bg-[#F3F1ED] px-4 py-2 rounded-[10px]">
          <div className="flex gap-4 text-gray-600">
            <span>خط العرض: <strong>{latitude.toFixed(6)}</strong></span>
            <span>خط الطول: <strong>{longitude.toFixed(6)}</strong></span>
          </div>
          <button
            type="button"
            onClick={() => onLocationChange(0, 0)}
            disabled={disabled}
            className="text-red-500 hover:text-red-600 text-sm"
          >
            مسح الموقع
          </button>
        </div>
      )}
    </div>
  );
}
