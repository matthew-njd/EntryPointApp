//Depreciated model. Now separated into DailyLog and WeeklyLog
import { DailyLog } from './dailylog.model';

export interface Timesheet {
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
