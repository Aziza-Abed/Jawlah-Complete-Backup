import { useState, useCallback, useRef, useEffect } from "react";
import { AlertTriangle } from "lucide-react";

interface ConfirmState {
  message: string;
  danger?: boolean;
}

// Hook that replaces window.confirm with a styled dialog
// Usage: const [confirm, ConfirmDialog] = useConfirm(); if (!await confirm("...")) return;
export function useConfirm() {
  const [state, setState] = useState<ConfirmState | null>(null);
  const resolveRef = useRef<((value: boolean) => void) | null>(null);

  const confirm = useCallback((message: string, danger = true): Promise<boolean> => {
    return new Promise((resolve) => {
      resolveRef.current = resolve;
      setState({ message, danger });
    });
  }, []);

  const handleClose = useCallback((result: boolean) => {
    resolveRef.current?.(result);
    resolveRef.current = null;
    setState(null);
  }, []);

  useEffect(() => {
    if (!state) return;
    const onKey = (e: KeyboardEvent) => { if (e.key === "Escape") handleClose(false); };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [state, handleClose]);

  const Dialog = state ? (
    <div className="fixed inset-0 z-[900] flex items-center justify-center" dir="rtl">
      <div className="absolute inset-0 bg-black/40" onClick={() => handleClose(false)} />
      <div className="relative bg-white rounded-[18px] shadow-2xl max-w-[400px] w-[90%] p-6">
        <div className="flex items-start gap-4">
          <div className={`p-3 rounded-[14px] shrink-0 ${state.danger ? "bg-[#C86E5D]/10 text-[#C86E5D]" : "bg-[#7895B2]/10 text-[#7895B2]"}`}>
            <AlertTriangle size={22} />
          </div>
          <div className="text-right flex-1">
            <h3 className="font-black text-[16px] text-[#2F2F2F]">تأكيد العملية</h3>
            <p className="text-[14px] text-[#6B7280] mt-1 leading-relaxed">{state.message}</p>
          </div>
        </div>
        <div className="flex items-center gap-3 mt-6 justify-end">
          <button
            type="button"
            onClick={() => handleClose(true)}
            className={`h-[40px] px-5 rounded-[12px] font-semibold text-[14px] text-white shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95 ${state.danger ? "bg-[#C86E5D]" : "bg-[#7895B2]"}`}
          >
            تأكيد
          </button>
          <button
            type="button"
            onClick={() => handleClose(false)}
            className="h-[40px] px-5 rounded-[12px] bg-white border border-black/10 text-[#2F2F2F] font-semibold text-[14px] hover:bg-[#F9F8F6]"
          >
            إلغاء
          </button>
        </div>
      </div>
    </div>
  ) : null;

  return [confirm, Dialog] as const;
}
