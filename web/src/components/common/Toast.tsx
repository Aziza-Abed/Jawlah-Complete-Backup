import { useEffect, useState } from "react";

export type ToastType = "success" | "error" | "info";

export interface ToastProps {
  message: string;
  type?: ToastType;
  duration?: number;
  onClose?: () => void;
}

export function Toast({ message, type = "success", duration = 3000, onClose }: ToastProps) {
  const [isVisible, setIsVisible] = useState(true);
  const [isExiting, setIsExiting] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => {
      setIsExiting(true);
      setTimeout(() => {
        setIsVisible(false);
        onClose?.();
      }, 300); // Match animation duration
    }, duration);

    return () => clearTimeout(timer);
  }, [duration, onClose]);

  if (!isVisible) return null;

  const bgColor = type === "success"
    ? "bg-green-50 border-green-200"
    : type === "error"
    ? "bg-red-50 border-red-200"
    : "bg-blue-50 border-blue-200";

  const textColor = type === "success"
    ? "text-green-800"
    : type === "error"
    ? "text-red-800"
    : "text-blue-800";

  const icon = type === "success"
    ? "✓"
    : type === "error"
    ? "✕"
    : "ℹ";

  return (
    <div
      dir="rtl"
      className={`fixed top-4 left-4 z-50 flex items-center gap-3 px-4 py-3 rounded-[12px] border shadow-lg transition-all duration-300 ${bgColor} ${
        isExiting ? "opacity-0 translate-x-4" : "opacity-100 translate-x-0"
      }`}
      role="alert"
      aria-live="polite"
    >
      <span className={`text-lg font-semibold ${textColor}`}>{icon}</span>
      <span className={`font-sans font-medium text-sm ${textColor}`}>{message}</span>
    </div>
  );
}

// Toast container component for managing multiple toasts
interface ToastMessage {
  id: string;
  message: string;
  type: ToastType;
}

export function ToastContainer() {
  const [toasts, setToasts] = useState<ToastMessage[]>([]);

  useEffect(() => {
    // Listen for custom toast events
    const handleToast = (event: CustomEvent<{ message: string; type: ToastType }>) => {
      const id = Date.now().toString();
      setToasts((prev) => [...prev, { id, ...event.detail }]);
    };

    window.addEventListener("show-toast" as any, handleToast);
    return () => window.removeEventListener("show-toast" as any, handleToast);
  }, []);

  const removeToast = (id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  };

  return (
    <div className="fixed top-4 left-4 z-50 flex flex-col gap-2">
      {toasts.map((toast, index) => (
        <div
          key={toast.id}
          style={{ top: `${index * 60}px` }}
          className="relative"
        >
          <Toast
            message={toast.message}
            type={toast.type}
            onClose={() => removeToast(toast.id)}
          />
        </div>
      ))}
    </div>
  );
}

// Helper function to show toasts from anywhere
export function showToast(message: string, type: ToastType = "success") {
  const event = new CustomEvent("show-toast", { detail: { message, type } });
  window.dispatchEvent(event);
}
