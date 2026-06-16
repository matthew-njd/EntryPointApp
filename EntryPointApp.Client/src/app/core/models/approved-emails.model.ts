export interface ApprovedEmailDto {
  id: number;
  email: string;
  addedByAdminId: number | null;
  addedByAdminName: string | null;
  createdAt: string;
}

export interface AddApprovedEmailRequest {
  email: string;
}
