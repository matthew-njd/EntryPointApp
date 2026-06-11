export interface WeeklyLog {
  id: number;
  userId: number;
  dateFrom: string;
  dateTo: string;
  totalHours: number;
  totalCharges: number;
  status: string;
  managerComment: string | null;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface WeeklyLogRequest {
  dateFrom: string;
  dateTo: string;
}

export interface WeeklyLogSummary {
  totalApproved: number;
  totalPending: number;
  totalDenied: number;
  totalDraft: number;
}
