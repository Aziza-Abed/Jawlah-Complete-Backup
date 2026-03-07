import { apiClient } from "./client";
import { DEFAULT_PAGE_SIZE, AUDIT_LOGS_SERVER_FETCH_COUNT } from "../constants/appConstants";

export interface AuditLog {
  auditLogId: number;
  username: string;
  userFullName?: string;
  action: string;
  entityName: string;
  entityId?: string;
  details?: string;
  ipAddress?: string;
  userAgent?: string;
  createdAt: string;
}

export interface AuditLogsResponse {
  items: AuditLog[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export async function getAuditLogs(page = 1, pageSize = DEFAULT_PAGE_SIZE, filters?: {
    username?: string;
    action?: string;
    startDate?: string;
    endDate?: string;
}): Promise<AuditLogsResponse> {
  let items: AuditLog[];

  if (filters?.startDate || filters?.endDate) {
    // Date range: use dedicated /audit/range endpoint (from=ISO&to=ISO)
    const from = filters.startDate
      ? new Date(filters.startDate).toISOString()
      : new Date(0).toISOString();
    const to = filters.endDate
      ? new Date(filters.endDate + 'T23:59:59').toISOString()
      : new Date().toISOString();

    const response = await apiClient.get<{ data: AuditLog[] }>(
      `/audit/range?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`
    );
    items = response.data.data || [];

    // action filter applied client-side for date-range results (partial match, same as server-side Contains)
    if (filters.action) {
      const term = filters.action.toLowerCase();
      items = items.filter(l => l.action.toLowerCase().includes(term));
    }
  } else {
    // Recent logs: server handles action filter, fetch enough for client pagination
    const params = new URLSearchParams();
    params.append("count", AUDIT_LOGS_SERVER_FETCH_COUNT.toString());
    if (filters?.action) params.append("action", filters.action);

    const response = await apiClient.get<{ data: AuditLog[] }>(`/audit?${params.toString()}`);
    items = response.data.data || [];
  }

  // Username text search is client-side (backend only supports userId int, not username string)
  if (filters?.username) {
    const term = filters.username.toLowerCase();
    items = items.filter(
      l => l.username?.toLowerCase().includes(term) ||
           l.userFullName?.toLowerCase().includes(term)
    );
  }

  const totalCount = items.length;
  const startIndex = (page - 1) * pageSize;
  const paginatedItems = items.slice(startIndex, startIndex + pageSize);

  return {
    items: paginatedItems,
    totalCount,
    page,
    pageSize,
  };
}
