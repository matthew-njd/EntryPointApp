export interface TeamTimesheetResponse {
  id: number;
  userId: number;
  userFullName: string;
  userEmail: string;
  dateFrom: string;
  dateTo: string;
  totalHours: number;
  totalCharges: number;
  status: string;
  managerComment: string | null;
  submittedAt: string;
  updatedAt: string;
}

export interface TeamDailyLogResponse {
  id: number;
  date: string;
  hours: number;
  mileage: number;
  tollCharge: number;
  parkingFee: number;
  otherCharges: number;
  comment: string;
}

export interface TeamTimesheetDetailResponse extends TeamTimesheetResponse {
  dailyLogs: TeamDailyLogResponse[];
}

export interface ApproveTimesheetRequest {
  comment?: string | null;
}

export interface DenyTimesheetRequest {
  reason: string;
}

export enum TimesheetStatus {
  Draft = 'Draft',
  Pending = 'Pending',
  Approved = 'Approved',
  Denied = 'Denied',
}
