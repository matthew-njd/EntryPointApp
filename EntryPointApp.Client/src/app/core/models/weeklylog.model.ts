import { DailyLog } from './dailylog.model';

export interface WeeklyLog {
  id: number;
  userId: number;
  dateFrom: string;
  dateTo: string;
  totalHours: number;
  totalCharges: number;
  status: string;
  isDeleted: boolean;
  createdAt: string;
  updatedAt: string;
  dailyLogs: DailyLog[];
}
