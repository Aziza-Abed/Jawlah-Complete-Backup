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
  const params = new URLSearchParams({
    page: page.toString(),
    pageSize: pageSize.toString(),
  });
  
  if (filters?.username) params.append("username", filters.username);
  if (filters?.action) params.append("action", filters.action);
  if (filters?.startDate) params.append("startDate", filters.startDate);
  if (filters?.endDate) params.append("endDate", filters.endDate);

  const response = await apiClient.get<AuditLogsResponse>(`/audit?${params.toString()}`);
  // If the backend response is wrapped in {data: ...}, adjust here:
  return (response as any).data?.items ? (response as any).data : response.data;
}
