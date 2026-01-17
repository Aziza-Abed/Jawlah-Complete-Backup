import React, { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";

type NoticeType = "task_update" | "task_done" | "issue_report" | "system";
type NoticeStatus = "unread" | "read";

type Notice = {
  id: string;
  type: NoticeType;
  status: NoticeStatus;
  title: string;
  body: string;
  time: string; // "10:40"
  taskId?: string;
  issueId?: string;
};

type FilterKey = "all" | "unread" | "tasks" | "issues";

export default function Notifications() {
  const navigate = useNavigate();

  // TODO: Replace with backend notifications:
  // - GET /notifications?role=supervisor
  // - Add realtime via WebSocket or polling
  const data: Notice[] = useMemo(
    () => [
      {
        id: "n1",
        type: "task_done",
        status: "unread",
        title: "تم إنهاء مهمة",
        body: "العامل محمود أنهى مهمة: إصلاح حفرة (المنطقة 4) وأرفق صورة إثبات.",
        time: "10:42",
        taskId: "t-204",
      },
      {
        id: "n2",
        type: "task_update",
        status: "unread",
        title: "تحديث جديد على مهمة",
        body: "العامل محمد رفع صورة تقدم لمهمة: تنظيف شارع (المنطقة 2).",
        time: "10:35",
        taskId: "t-102",
      },
      {
        id: "n3",
        type: "issue_report",
        status: "read",
        title: "بلاغ جديد",
        body: "العامل أبو عمار أبلغ عن مشكلة خارج المسؤولية: تسريب مياه قرب دوار البلدية.",
        time: "09:58",
        issueId: "i-77",
      },
      {
        id: "n4",
        type: "system",
        status: "read",
        title: "تنبيه النظام",
        body: "تم تحديث إعدادات التقارير لهذا الأسبوع.",
        time: "09:20",
      },
    ],
    []
  );

  const [items, setItems] = useState<Notice[]>(data);
  const [filter, setFilter] = useState<FilterKey>("all");

  const unreadCount = useMemo(
    () => items.filter((x) => x.status === "unread").length,
    [items]
  );

  const filtered = useMemo(() => {
    if (filter === "all") return items;
    if (filter === "unread") return items.filter((x) => x.status === "unread");
    if (filter === "tasks") return items.filter((x) => x.type === "task_update" || x.type === "task_done");
    return items.filter((x) => x.type === "issue_report");
  }, [items, filter]);

  const markAllRead = () => {
    setItems((prev) => prev.map((x) => ({ ...x, status: "read" })));
    // TODO: PATCH /notifications/mark-all-read
  };

  const markOneRead = (id: string) => {
    setItems((prev) => prev.map((x) => (x.id === id ? { ...x, status: "read" } : x)));
    // TODO: PATCH /notifications/{id}/read
  };

  const clearRead = () => {
    setItems((prev) => prev.filter((x) => x.status !== "read"));
    // TODO: DELETE /notifications?status=read (or endpoint provided by backend)
  };

  const openNotice = (n: Notice) => {
    // Mark as read first
    if (n.status === "unread") markOneRead(n.id);

    // Navigate based on type
    if (n.type === "issue_report" && n.issueId) {
      // TODO: Create Issue Details page later: /issues/:id
      navigate(`/issues/${n.issueId}`);
      return;
    }

    if ((n.type === "task_update" || n.type === "task_done") && n.taskId) {
      // TODO: Create Task Details page later: /tasks/:id
      navigate(`/tasks/${n.taskId}`);
      return;
    }
  };

  return (
    <div className="h-full w-full bg-[#D9D9D9] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1100px] mx-auto">
          {/* Header */}
          <div className="flex items-center justify-between gap-3">
            <h1 className="text-right font-sans font-semibold text-[20px] sm:text-[22px] text-[#2F2F2F]">
              الإشعارات
            </h1>

            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={clearRead}
                className="h-[38px] px-3 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[13px] hover:opacity-95"
              >
                مسح المقروء
              </button>

              <button
                type="button"
                onClick={markAllRead}
                className="h-[38px] px-3 rounded-[10px] bg-[#60778E] text-white font-sans font-semibold text-[13px] shadow-[0_2px_0_rgba(0,0,0,0.15)] hover:opacity-95"
              >
                تعليم الكل كمقروء
              </button>
            </div>
          </div>

          {/* Filters */}
          <div className="mt-4 bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.08)] p-3 sm:p-4">
            <div className="flex items-center justify-between gap-3 flex-wrap">
              <div className="flex items-center gap-2 flex-wrap justify-end">
                <FilterChip active={filter === "all"} onClick={() => setFilter("all")}>
                  الكل
                </FilterChip>
                <FilterChip active={filter === "unread"} onClick={() => setFilter("unread")}>
                  غير مقروء
                </FilterChip>
                <FilterChip active={filter === "tasks"} onClick={() => setFilter("tasks")}>
                  تحديثات المهام
                </FilterChip>
                <FilterChip active={filter === "issues"} onClick={() => setFilter("issues")}>
                  البلاغات
                </FilterChip>
              </div>

              <div className="text-right text-[12px] text-[#6B7280]">
                غير مقروء: <span className="font-semibold text-[#2F2F2F]">{unreadCount}</span>
              </div>
            </div>
          </div>

          {/* List */}
          <div className="mt-4 space-y-3">
            {filtered.length === 0 ? (
              <div className="bg-white rounded-[14px] border border-black/10 p-5 text-right text-[#6B7280]">
                لا يوجد إشعارات حسب الفلتر الحالي.
              </div>
            ) : (
              filtered.map((n) => (
                <button
                  key={n.id}
                  type="button"
                  onClick={() => openNotice(n)}
                  className={[
                    "w-full text-right",
                    "bg-[#F3F1ED] rounded-[14px] border border-black/10 shadow-[0_2px_0_rgba(0,0,0,0.06)]",
                    "p-4 sm:p-5",
                    "hover:opacity-95 transition",
                  ].join(" ")}
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-center gap-2">
                      {n.status === "unread" && (
                        <span className="inline-block w-2.5 h-2.5 rounded-full bg-[#C86E5D]" aria-hidden="true" />
                      )}
                      <span className="text-[12px] text-[#6B7280]">{n.time}</span>
                    </div>

                    <div className="flex-1">
                      <div className="flex items-center justify-between gap-3">
                        <div className="text-[14px] sm:text-[15px] font-sans font-semibold text-[#2F2F2F]">
                          {n.title}
                        </div>
                        <span
                          className="text-[11px] px-2 py-1 rounded-full border border-black/10 bg-white text-[#2F2F2F]"
                        >
                          {labelForType(n.type)}
                        </span>
                      </div>

                      <div className="mt-2 text-[13px] text-[#2F2F2F] leading-relaxed">
                        {n.body}
                      </div>

                      {/* Action hint */}
                      {(n.taskId || n.issueId) && (
                        <div className="mt-3 text-[12px] text-[#60778E] font-sans font-semibold">
                          اضغط لعرض التفاصيل
                        </div>
                      )}
                    </div>
                  </div>
                </button>
              ))
            )}
          </div>

          <div className="mt-6 text-right text-[12px] text-[#6B7280]">
            {/* TODO: Backend should provide:
                - notifications list
                - unread count
                - read/unread actions
                - deep links to task/issue details */}
          </div>
        </div>
      </div>
    </div>
  );
}

/* ---------- UI Helpers ---------- */

function FilterChip({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={[
        "h-[34px] px-3 rounded-[10px] font-sans font-semibold text-[13px] border",
        active
          ? "bg-[#60778E] text-white border-black/10"
          : "bg-white text-[#2F2F2F] border-black/10 hover:opacity-95",
      ].join(" ")}
    >
      {children}
    </button>
  );
}

function labelForType(t: NoticeType) {
  if (t === "task_update") return "تحديث";
  if (t === "task_done") return "إكمال";
  if (t === "issue_report") return "بلاغ";
  return "نظام";
}
