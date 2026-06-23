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
  salesRepComment: string | null;
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
  PendingSalesRep = 'PendingSalesRep',
  PendingManager = 'PendingManager',
  Approved = 'Approved',
  Denied = 'Denied',
}

export interface TimesheetSummary {
  totalApproved: number;
  totalPending: number;
  totalPendingSalesRep: number;
  totalPendingManager: number;
  totalDenied: number;
}

export interface TimesheetStatusHistoryEntry {
  id: number;
  actorFullName: string;
  actorRole: string;
  fromStatus: string;
  toStatus: string;
  comment: string | null;
  createdAt: string;
}
