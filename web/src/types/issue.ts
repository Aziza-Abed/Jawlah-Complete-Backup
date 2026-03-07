// issue types

export type IssueType = "Infrastructure" | "Safety" | "Cleanliness" | "Equipment" | "Other";
export type IssueSeverity = "Low" | "Medium" | "High" | "Critical";
export type IssueStatus = "New" | "Forwarded" | "Resolved" | "InProgress" | "Closed" | "ConvertedToTask";

export type IssueResponse = {
  issueId: number;
  title: string;
  description: string;
  type: IssueType;
  severity: IssueSeverity;
  status: IssueStatus;
  reportedByUserId: number;
  reportedByName?: string;
  zoneId?: number;
  zoneName?: string;
  latitude: number;
  longitude: number;
  locationDescription?: string;
  photos: string[];
  reportedAt: string;
  resolvedAt?: string;
  resolutionNotes?: string;
  // Forwarding info
  forwardedToDepartmentId?: number;
  forwardedToDepartmentName?: string;
  forwardedAt?: string;
  forwardingNotes?: string;

  syncTime?: string;
  syncVersion: number;
  priority: string; // Alias for severity (for web dashboard compatibility)
};

export type ForwardIssueRequest = {
  departmentId: number;
  notes?: string;
};

export type UpdateIssueStatusRequest = {
  status: IssueStatus;
  resolutionNotes?: string;
};

export type CreateTaskFromIssueRequest = {
  assignedToUserId: number;
  priority: "Low" | "Medium" | "High" | "Urgent";
  dueDate?: string;
  requiresPhotoProof: boolean;
  taskType?: string;
};
