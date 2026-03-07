import { useState, useEffect } from "react";
import { STORAGE_KEYS } from "../../constants/storageKeys";

const BASE_URL = import.meta.env.VITE_API_BASE_URL || "";

interface AuthImageProps {
  src: string;
  alt: string;
  className?: string;
  onClick?: (e: React.MouseEvent) => void;
}

/**
 * Image component that fetches protected API images with JWT auth header.
 * Use for any image served from /api/files/* that requires authentication.
 */
export default function AuthImage({ src, alt, className, onClick }: AuthImageProps) {
  const [objectUrl, setObjectUrl] = useState<string>("");
  const [error, setError] = useState(false);

  useEffect(() => {
    let cancelled = false;

    const fetchImage = async () => {
      try {
        const token = localStorage.getItem(STORAGE_KEYS.TOKEN);
        // Build full URL if src is relative
        const fullUrl = src.startsWith("http") ? src : `${BASE_URL.replace(/\/api\/?$/, "")}${src.startsWith("/") ? "" : "/"}${src}`;

        const res = await fetch(fullUrl, {
          headers: token ? { Authorization: `Bearer ${token}` } : {},
        });

        if (!res.ok) throw new Error(`${res.status}`);

        const blob = await res.blob();
        if (!cancelled) {
          setObjectUrl(URL.createObjectURL(blob));
          setError(false);
        }
      } catch {
        if (!cancelled) setError(true);
      }
    };

    if (src) fetchImage();

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [src]);

  if (error || !src) {
    return (
      <div className={`bg-gray-100 flex items-center justify-center text-gray-400 text-xs ${className || ""}`}>
        فشل تحميل الصورة
      </div>
    );
  }

  if (!objectUrl) {
    return (
      <div className={`bg-gray-50 flex items-center justify-center ${className || ""}`}>
        <div className="w-5 h-5 border-2 border-gray-300 border-t-gray-500 rounded-full animate-spin" />
      </div>
    );
  }

  return <img src={objectUrl} alt={alt} className={className} onClick={onClick} />;
}
