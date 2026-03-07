import { useEffect, useCallback } from "react";
import { X, ChevronRight, ChevronLeft } from "lucide-react";
import AuthImage from "./AuthImage";

interface ImageLightboxProps {
  images: string[];
  currentIndex: number;
  onClose: () => void;
  onNavigate: (index: number) => void;
}

export default function ImageLightbox({
  images,
  currentIndex,
  onClose,
  onNavigate,
}: ImageLightboxProps) {
  const hasPrev = currentIndex > 0;
  const hasNext = currentIndex < images.length - 1;

  const handleKeyDown = useCallback(
    (e: KeyboardEvent) => {
      if (e.key === "Escape") onClose();
      if (e.key === "ArrowLeft" && hasNext) onNavigate(currentIndex + 1);
      if (e.key === "ArrowRight" && hasPrev) onNavigate(currentIndex - 1);
    },
    [onClose, onNavigate, currentIndex, hasNext, hasPrev]
  );

  useEffect(() => {
    document.addEventListener("keydown", handleKeyDown);
    document.body.style.overflow = "hidden";
    return () => {
      document.removeEventListener("keydown", handleKeyDown);
      document.body.style.overflow = "";
    };
  }, [handleKeyDown]);

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/80"
      onClick={onClose}
    >
      {/* Close button */}
      <button
        className="absolute top-4 left-4 text-white/80 hover:text-white bg-black/40 rounded-full p-2 transition"
        onClick={onClose}
        aria-label="إغلاق"
      >
        <X size={24} />
      </button>

      {/* Counter */}
      {images.length > 1 && (
        <div className="absolute top-4 right-4 text-white/80 text-sm bg-black/40 rounded-full px-3 py-1">
          {currentIndex + 1} / {images.length}
        </div>
      )}

      {/* Previous arrow (RTL: right side) */}
      {hasPrev && (
        <button
          className="absolute right-4 top-1/2 -translate-y-1/2 text-white/80 hover:text-white bg-black/40 rounded-full p-2 transition"
          onClick={(e) => {
            e.stopPropagation();
            onNavigate(currentIndex - 1);
          }}
          aria-label="السابقة"
        >
          <ChevronRight size={28} />
        </button>
      )}

      {/* Next arrow (RTL: left side) */}
      {hasNext && (
        <button
          className="absolute left-4 top-1/2 -translate-y-1/2 text-white/80 hover:text-white bg-black/40 rounded-full p-2 transition"
          onClick={(e) => {
            e.stopPropagation();
            onNavigate(currentIndex + 1);
          }}
          aria-label="التالية"
        >
          <ChevronLeft size={28} />
        </button>
      )}

      {/* Image */}
      <AuthImage
        src={images[currentIndex]}
        alt={`صورة ${currentIndex + 1}`}
        className="max-h-[85vh] max-w-[90vw] object-contain rounded-lg shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      />
    </div>
  );
}
