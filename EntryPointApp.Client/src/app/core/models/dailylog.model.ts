export interface DailyLog {
  id: number;
  weeklyLogId: number;
  date: string;
  hours: number;
  mileage: number;
  tollCharge: number;
  parkingFee: number;
  otherCharges: number;
  comment: string;
}
