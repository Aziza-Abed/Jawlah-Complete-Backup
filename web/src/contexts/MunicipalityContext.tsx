import { createContext, useContext, useState, useEffect, ReactNode } from "react";
import {
  MunicipalitySettings,
  MunicipalityBasic,
  getCurrentMunicipalitySettings,
  getDefaultMunicipalitySettings,
  DEFAULT_MUNICIPALITY_SETTINGS
} from "../api/municipality";

interface MunicipalityContextType {
  settings: MunicipalitySettings | MunicipalityBasic | null;
  loading: boolean;
  error: string | null;
  mapCenter: [number, number];
  mapZoom: number;
  municipalityName: string;
  municipalityNameEn: string;
  logoUrl: string | null;
  refresh: () => Promise<void>;
}

const MunicipalityContext = createContext<MunicipalityContextType | null>(null);

export function MunicipalityProvider({ children }: { children: ReactNode }) {
  const [settings, setSettings] = useState<MunicipalitySettings | MunicipalityBasic | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchSettings = async () => {
    setLoading(true);
    setError(null);

    try {
      // Check if user is authenticated
      const token = localStorage.getItem("followup_token");

      if (token) {
        // Try to get user's municipality settings
        const userSettings = await getCurrentMunicipalitySettings();
        if (userSettings) {
          setSettings(userSettings);
          setLoading(false);
          return;
        }
      }

      // Fallback to default municipality
      const defaultSettings = await getDefaultMunicipalitySettings();
      setSettings(defaultSettings);
    } catch (err) {
      console.error("Failed to load municipality settings:", err);
      setError("فشل في تحميل إعدادات البلدية");
      setSettings(DEFAULT_MUNICIPALITY_SETTINGS);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSettings();
  }, []);

  // Compute derived values
  const mapCenter: [number, number] = settings
    ? [settings.centerLatitude, settings.centerLongitude]
    : [DEFAULT_MUNICIPALITY_SETTINGS.centerLatitude, DEFAULT_MUNICIPALITY_SETTINGS.centerLongitude];

  const mapZoom = settings?.defaultZoom ?? DEFAULT_MUNICIPALITY_SETTINGS.defaultZoom;
  const municipalityName = settings?.name ?? DEFAULT_MUNICIPALITY_SETTINGS.name;
  const municipalityNameEn = settings?.nameEnglish ?? DEFAULT_MUNICIPALITY_SETTINGS.nameEnglish ?? "";
  const logoUrl = settings?.logoUrl ?? null;

  return (
    <MunicipalityContext.Provider
      value={{
        settings,
        loading,
        error,
        mapCenter,
        mapZoom,
        municipalityName,
        municipalityNameEn,
        logoUrl,
        refresh: fetchSettings,
      }}
    >
      {children}
    </MunicipalityContext.Provider>
  );
}

/**
 * Hook to access municipality settings throughout the app
 */
export function useMunicipality() {
  const context = useContext(MunicipalityContext);
  if (!context) {
    throw new Error("useMunicipality must be used within a MunicipalityProvider");
  }
  return context;
}

/**
 * Simple hook for components that just need map center coordinates
 * Can be used without the context provider (uses defaults)
 */
export function useMapCenter(): { center: [number, number]; zoom: number; loading: boolean } {
  const [center, setCenter] = useState<[number, number]>([
    DEFAULT_MUNICIPALITY_SETTINGS.centerLatitude,
    DEFAULT_MUNICIPALITY_SETTINGS.centerLongitude,
  ]);
  const [zoom, setZoom] = useState(DEFAULT_MUNICIPALITY_SETTINGS.defaultZoom);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const loadSettings = async () => {
      try {
        const token = localStorage.getItem("followup_token");
        if (token) {
          const settings = await getCurrentMunicipalitySettings();
          if (settings) {
            setCenter([settings.centerLatitude, settings.centerLongitude]);
            setZoom(settings.defaultZoom);
          }
        } else {
          const settings = await getDefaultMunicipalitySettings();
          setCenter([settings.centerLatitude, settings.centerLongitude]);
          setZoom(settings.defaultZoom);
        }
      } catch (error) {
        console.error("Failed to load map settings:", error);
      } finally {
        setLoading(false);
      }
    };

    loadSettings();
  }, []);

  return { center, zoom, loading };
}
