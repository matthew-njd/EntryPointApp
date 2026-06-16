import { ReceiptResponse } from './dailylog.model';

export interface AdminTimesheetResponse {
  id: number;
  dateFrom: string;
  dateTo: string;
  totalHours: number;
  totalCharges: number;
  status: string;
  managerComment: string | null;
  submittedAt: string;
  updatedAt: string;
}

export interface AdminDailyLogResponse {
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

export interface AdminTimesheetDetailResponse extends AdminTimesheetResponse {
  userId: number;
  userFullName: string;
  userEmail: string;
  dailyLogs: AdminDailyLogResponse[];
}

export interface UserSummary {
  totalUsers: number;
  totalManagers: number;
  totalAdmins: number;
  activeUsers: number;
}

export interface UserListResponse {
  users: UserDto[];
  summary: UserSummary;
}

export interface UserPagedResponse {
  data: UserDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  summary: UserSummary;
}

export interface UserDto {
  id: number;
  email: string;
  firstName: string | null;
  lastName: string | null;
  role: string;
  employeeType: string | null;
  managerId: number | null;
  managerName: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface UserRateDto {
  id: number;
  userId: number;
  hourlyRate: number;
  mileageRate: number;
  effectiveDate: string;
  createdAt: string;
  createdByAdminId: number;
}

export interface SetUserRateRequest {
  hourlyRate: number;
  mileageRate: number;
  effectiveDate: string;
}

export interface UpdateUserRoleRequest {
  role: UserRole;
}

export interface AssignManagerRequest {
  managerId: number;
}

export enum UserRole {
  User = 0,
  Manager = 1,
  Admin = 2,
}

export enum EmployeeType {
  Employee = 0,
  Contractor = 1,
}

export interface UpdateEmployeeTypeRequest {
  employeeType: EmployeeType | null;
}

export function getRoleDisplayName(role: string | UserRole): string {
  if (typeof role === 'string') {
    return role;
  }
  switch (role) {
    case UserRole.User:
      return 'User';
    case UserRole.Manager:
      return 'Manager';
    case UserRole.Admin:
      return 'Admin';
    default:
      return 'Unknown';
  }
}

export interface PayrollSummaryItem {
  userId: number;
  fullName: string;
  employeeType: string;
  hourlyRate: number;
  mileageRate: number;
  totalHours: number;
  totalMileage: number;
  grossPay: number;
  mileageReimbursement: number;
}

export interface PayrollSummaryResponse {
  dateFrom: string;
  dateTo: string;
  items: PayrollSummaryItem[];
}
