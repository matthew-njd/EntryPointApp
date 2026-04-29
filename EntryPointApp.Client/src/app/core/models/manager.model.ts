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

import { ReceiptResponse } from './dailylog.model';

export interface TeamDailyLogResponse {
  id: number;
  date: string;
  timeIn: string;
  timeOut: string;
  hours: number;
  mileage: number;
  tollCharge: number;
  parkingFee: number;
  otherCharges: number;
  comment: string;
  receipts: ReceiptResponse[];
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
