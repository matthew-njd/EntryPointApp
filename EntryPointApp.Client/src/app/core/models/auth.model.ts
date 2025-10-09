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
