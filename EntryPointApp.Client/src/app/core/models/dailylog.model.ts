export interface ReceiptResponse {
  id: number;
  dailyLogId: number;
  originalFileName: string;
  contentType: string;
  fileSizeBytes: number;
  uploadedAt: string;
}

export interface DailyLog {
  id: number;
  weeklyLogId: number;
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

export interface DailyLogRequest {
  date: string;
  timeIn: string;
  timeOut: string;
  mileage: number;
  tollCharge: number;
  parkingFee: number;
  otherCharges: number;
  comment: string;
}

export interface DailyLogUpdateItem {
  id?: number | null;
  date: string;
  timeIn: string;
  timeOut: string;
  mileage: number;
  tollCharge: number;
  parkingFee: number;
  otherCharges: number;
  comment: string;
}

export interface UpdateDailyLogsRequest {
  dailyLogs: DailyLogUpdateItem[];
}
