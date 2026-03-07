import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  getNotifications,
  markNotificationAsRead,
  markAllNotificationsAsRead,
} from "../api/notifications";
import type { NotificationResponse } from "../types/notification";
import { useNotifications } from "../contexts/NotificationContext";

type NoticeType = "task_update" | "issue_report" | "system";
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
  if (type.includes("Appeal")) return "task_update";
  if (type.includes("Issue")) return "issue_report";
  return "system";
};

// Format notification time with date context
const formatNotificationTime = (date: Date): string => {
  const now = new Date();
  const hours = date.getHours().toString().padStart(2, "0");
  const minutes = date.getMinutes().toString().padStart(2, "0");
  const time = `${hours}:${minutes}`;

  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  const target = new Date(date.getFullYear(), date.getMonth(), date.getDate());
  const diffDays = Math.floor((today.getTime() - target.getTime()) / 86400000);

  if (diffDays === 0) return `اليوم ${time}`;
  if (diffDays === 1) return `أمس ${time}`;
  return `${date.getDate()}/${date.getMonth() + 1} ${time}`;
};

// Convert backend NotificationResponse to frontend Notice
const mapNotificationToNotice = (
  notification: NotificationResponse,
): Notice => {
  const date = new Date(notification.createdAt);

  return {
    id: notification.notificationId.toString(),
    type: mapNotificationType(notification.type),
    status: notification.isRead ? "read" : "unread",
    title: notification.title,
    body: notification.message,
    time: formatNotificationTime(date),
    taskId: notification.taskId?.toString(),
    issueId: notification.issueId?.toString(),
  };
};

export default function Notifications() {
  const navigate = useNavigate();
  const { decrementCount, resetCount } = useNotifications();

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
    [items],
  );

  const filtered = useMemo(() => {
    if (filter === "all") return items;
    if (filter === "unread") return items.filter((x) => x.status === "unread");
    if (filter === "tasks")
      return items.filter((x) => x.type === "task_update");
    return items.filter((x) => x.type === "issue_report");
  }, [items, filter]);

  const markAllRead = async () => {
    try {
      await markAllNotificationsAsRead();
      setItems((prev) => prev.map((x) => ({ ...x, status: "read" })));
      resetCount();
    } catch (err) {
      console.error("Failed to mark all as read:", err);
    }
  };

  const markOneRead = async (id: string) => {
    try {
      const item = items.find((x) => x.id === id);
      await markNotificationAsRead(Number(id));
      setItems((prev) =>
        prev.map((x) => (x.id === id ? { ...x, status: "read" } : x)),
      );
      if (item?.status === "unread") {
        decrementCount();
      }
    } catch (err) {
      console.error("Failed to mark notification as read:", err);
    }
  };

  const openNotice = (n: Notice) => {
    // Mark as read first
    if (n.status === "unread") markOneRead(n.id);

    // Navigate to task or issue if ID is available
    if (n.taskId) {
      navigate(`/tasks/${n.taskId}`);
      return;
    }

    if (n.issueId) {
      navigate(`/issues/${n.issueId}`);
      return;
    }
  };

  return (
    <div className="h-full w-full bg-[#F3F1ED] overflow-auto">
      <div className="p-4 sm:p-6 md:p-8">
        <div className="max-w-[1100px] mx-auto">
          {/* Header */}
          <div className="flex items-center justify-between gap-3">
            <h1 className="text-right font-black text-[28px] text-[#2F2F2F] tracking-tight">
              الإشعارات
            </h1>

            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={markAllRead}
                disabled={unreadCount === 0}
                className={[
                  "h-[38px] px-3 rounded-[10px] font-sans font-semibold text-[13px] shadow-[0_2px_0_rgba(0,0,0,0.15)]",
                  unreadCount > 0
                    ? "bg-[#7895B2] text-white hover:opacity-95"
                    : "bg-[#C5C5C5] text-white cursor-not-allowed",
                ].join(" ")}
              >
                تعليم الكل كمقروء
              </button>
            </div>
          </div>

          {loading && (
            <div className="mt-8 text-center text-[#2F2F2F] font-sans">
              جاري التحميل...
            </div>
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
                    <FilterChip
                      active={filter === "all"}
                      onClick={() => setFilter("all")}
                    >
                      الكل
                    </FilterChip>
                    <FilterChip
                      active={filter === "unread"}
                      onClick={() => setFilter("unread")}
                    >
                      غير مقروء
                    </FilterChip>
                    <FilterChip
                      active={filter === "tasks"}
                      onClick={() => setFilter("tasks")}
                    >
                      تحديثات المهام
                    </FilterChip>
                    <FilterChip
                      active={filter === "issues"}
                      onClick={() => setFilter("issues")}
                    >
                      البلاغات
                    </FilterChip>
                  </div>

                  <div className="text-right text-[12px] text-[#6B7280]">
                    غير مقروء:{" "}
                    <span className="font-semibold text-[#2F2F2F]">
                      {unreadCount}
                    </span>
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
                            <span
                              className="inline-block w-2.5 h-2.5 rounded-full bg-[#C86E5D]"
                              aria-hidden="true"
                            />
                          )}
                          <span className="text-[12px] text-[#6B7280]">
                            {n.time}
                          </span>
                        </div>

                        <div className="flex-1">
                          <div className="flex items-center justify-between gap-3">
                            <div className="text-[14px] sm:text-[15px] font-sans font-semibold text-[#2F2F2F]">
                              {n.title}
                            </div>
                            <span className="text-[11px] px-2 py-1 rounded-full border border-black/10 bg-white text-[#2F2F2F]">
                              {labelForType(n.type)}
                            </span>
                          </div>

                          <div className="mt-2 text-[13px] text-[#2F2F2F] leading-relaxed">
                            {n.body}
                          </div>

                          {/* Action hint */}
                          {(n.taskId || n.issueId) && (
                            <div className="mt-3 text-[12px] text-[#7895B2] font-sans font-semibold">
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
          ? "bg-[#7895B2] text-white border-black/10"
          : "bg-white text-[#2F2F2F] border-black/10 hover:opacity-95",
      ].join(" ")}
    >
      {children}
    </button>
  );
}

function labelForType(t: NoticeType) {
  if (t === "task_update") return "تحديث";
  if (t === "issue_report") return "بلاغ";
  return "نظام";
}
