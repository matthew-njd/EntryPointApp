export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserResponse;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  role: number;
  firstName: string;
  lastName: string;
}

export interface RegisterResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: UserResponse;
}

export interface UserResponse {
  id: number;
  email: string;
  firstName: string | null;
  lastName: string | null;
  role: string;
  managerId: number | null;
}

export interface ApiResponse<T = any> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

export interface ErrorResponse {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance: string;
  traceId: string;
}

export enum UserRole {
  User = 0,
  Manager = 1,
  Admin = 2,
}
