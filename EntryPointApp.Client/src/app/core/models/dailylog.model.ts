export interface DailyLog {
  id: number;
  weeklyLogId: number;
  date: string;
  hoursWorked: number;
  mileage: number;
  tollCharges: number;
  parkingFee: number;
  otherCharges: number;
  comment: string;
}
