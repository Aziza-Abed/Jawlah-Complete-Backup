import { apiClient } from "./client";

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

export async function getAuditLogs(page = 1, pageSize = 50, filters?: {
    username?: string;
    action?: string;
    startDate?: string;
    endDate?: string;
}): Promise<AuditLogsResponse> {
  const params = new URLSearchParams();
  // Backend uses 'count' parameter, not pageSize
  params.append("count", (pageSize * page).toString());

  if (filters?.username) params.append("userId", filters.username);
  if (filters?.action) params.append("action", filters.action);
  // Note: backend uses /audit/range endpoint for date filtering

  const response = await apiClient.get<{ data: AuditLog[] }>(`/audit?${params.toString()}`);
  const items = response.data.data || [];

  // Backend returns flat array, we wrap it for pagination UI
  const startIndex = (page - 1) * pageSize;
  const paginatedItems = items.slice(startIndex, startIndex + pageSize);

  return {
    items: paginatedItems,
    totalCount: items.length,
    page,
    pageSize
  };
}
