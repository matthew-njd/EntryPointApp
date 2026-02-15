export interface UserDto {
  id: number;
  email: string;
  firstName: string | null;
  lastName: string | null;
  role: string;
  managerId: number | null;
  managerName: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
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
