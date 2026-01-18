import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { getNotifications, markNotificationAsRead, markAllNotificationsAsRead } from "../api/notifications";
import type { NotificationResponse } from "../types/notification";

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

// Map backend notification type to frontend type
const mapNotificationType = (type: string): NoticeType => {
  if (type.includes("Task")) return "task_update";
  if (type.includes("Issue")) return "issue_report";
  return "system";
};

// Convert backend NotificationResponse to frontend Notice
const mapNotificationToNotice = (notification: NotificationResponse): Notice => {
  const date = new Date(notification.createdAt);
  const hours = date.getHours().toString().padStart(2, "0");
  const minutes = date.getMinutes().toString().padStart(2, "0");

  return {
    id: notification.notificationId.toString(),
    type: mapNotificationType(notification.type),
    status: notification.isRead ? "read" : "unread",
    title: notification.title,
    body: notification.message,
    time: `${hours}:${minutes}`,
  };
};

export default function Notifications() {
  const navigate = useNavigate();

  const [items, setItems] = useState<Notice[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [filter, setFilter] = useState<FilterKey>("all");

  // Fetch notifications from backend
  useEffect(() => {
    const fetchNotifications = async () => {
      try {
        setLoading(true);
        setError("");
        const notifications = await getNotifications();
        const notices = notifications.map(mapNotificationToNotice);
        setItems(notices);
      } catch (err) {
        console.error("Failed to fetch notifications:", err);
        setError("فشل تحميل الإشعارات");
      } finally {
        setLoading(false);
      }
    };
    fetchNotifications();
  }, []);

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

  const markAllRead = async () => {
    try {
      await markAllNotificationsAsRead();
      setItems((prev) => prev.map((x) => ({ ...x, status: "read" })));
    } catch (err) {
      console.error("Failed to mark all as read:", err);
    }
  };

  const markOneRead = async (id: string) => {
    try {
      await markNotificationAsRead(Number(id));
      setItems((prev) => prev.map((x) => (x.id === id ? { ...x, status: "read" } : x)));
    } catch (err) {
      console.error("Failed to mark notification as read:", err);
    }
  };

  const clearRead = () => {
    setItems((prev) => prev.filter((x) => x.status !== "read"));
    // Note: Backend doesn't have bulk delete endpoint yet
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
                disabled={loading}
                className="h-[38px] px-3 rounded-[10px] bg-white border border-black/10 text-[#2F2F2F] font-sans font-semibold text-[13px] hover:opacity-95 disabled:opacity-50"
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

          {loading && (
            <div className="mt-8 text-center text-[#2F2F2F] font-sans">جاري التحميل...</div>
          )}

          {error && (
            <div className="mt-4 p-4 rounded-[12px] bg-red-100 text-red-700 text-right font-sans">
              {error}
            </div>
          )}

          {!loading && !error && (
            <>
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
            {/* Notifications loaded from backend API */}
          </div>
            </>
          )}
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
