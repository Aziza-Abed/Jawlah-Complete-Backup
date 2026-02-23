// Shared display types and mappers for issue pages
// Maps backend enum values to frontend display values

export type Severity = "low" | "medium" | "high" | "critical";
export type DisplayIssueStatus = "new" | "reviewing" | "converted" | "forwarded" | "rejected" | "closed";

export const mapSeverity = (severity: string | undefined | null): Severity => {
  switch ((severity || "").toLowerCase()) {
    case "low": return "low";
    case "medium": return "medium";
    case "high": return "high";
    case "critical": return "critical";
    default: return "medium";
  }
};

export const mapStatus = (status: string | null | undefined): DisplayIssueStatus => {
  if (!status) return "new";
  switch (status) {
    case "Reported": return "new";
    case "UnderReview": return "reviewing";
    case "Resolved": return "closed";
    case "Dismissed": return "rejected";
    default: return "new";
  }
};

export const mapTypeToArabic = (type: string): string => {
  switch (type) {
    case "Infrastructure": return "بنية تحتية";
    case "Safety": return "سلامة";
    case "Cleanliness": return "نظافة";
    case "Equipment": return "معدات";
    case "Other": return "أخرى";
    default: return type;
  }
};
