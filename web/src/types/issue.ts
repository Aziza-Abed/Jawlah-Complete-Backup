// Issue types matching backend DTOs

export type IssueType = "Infrastructure" | "Safety" | "Sanitation" | "Equipment" | "Other";
export type IssueSeverity = "Minor" | "Medium" | "Major" | "Critical";
export type IssueStatus = "Reported" | "UnderReview" | "Resolved" | "Dismissed";

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
  syncTime?: string;
  syncVersion: number;
  priority: string; // Alias for severity (for web dashboard compatibility)
};

export type CreateIssueRequest = {
  title: string;
  description: string;
  type: IssueType;
  severity: IssueSeverity;
  latitude: number;
  longitude: number;
  locationDescription?: string;
  zoneId?: number;
};

export type UpdateIssueStatusRequest = {
  status: IssueStatus;
  resolutionNotes?: string;
};
