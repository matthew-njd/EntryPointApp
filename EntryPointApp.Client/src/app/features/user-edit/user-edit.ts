import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  getRoleDisplayName,
  UserDto,
  UserRole,
} from '../../core/models/admin.model';
import { ActivatedRoute, Router } from '@angular/router';
import { AdminService } from '../../core/services/admin.service';
import { ToastService } from '../../core/services/toast.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { Footer } from '../../shared/footer/footer';

@Component({
  selector: 'app-user-edit',
  imports: [CommonModule, ReactiveFormsModule, Footer],
  templateUrl: './user-edit.html',
  styleUrl: './user-edit.css',
})
export class UserEdit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminService = inject(AdminService);
  private toastService = inject(ToastService);

  userId = toSignal(this.route.paramMap);
  userForm: FormGroup;
  isLoading = signal(false);
  isLoadingData = signal(true);
  user = signal<UserDto | null>(null);

  UserRole = UserRole;
  getRoleDisplayName = getRoleDisplayName;

  availableManagers = computed(() =>
    this.adminService
      .users()
      .filter(
        (u) => u.role === 'Manager' && u.isActive && u.id !== this.user()?.id,
      ),
  );

  roleDescriptions = {
    [UserRole.User]:
      'Can create and submit timesheets. Must be assigned to a manager.',
    [UserRole.Manager]:
      'Can approve/deny team timesheets. Cannot submit their own timesheets.',
    [UserRole.Admin]:
      'Full system access. Can manage users, roles, and view all data.',
  };

  constructor() {
    this.userForm = this.fb.group({
      role: [UserRole.User, [Validators.required]],
      managerId: [null as number | null],
    });

    this.adminService.loadUsers();
  }

  loadEffect = effect(() => {
    const id = this.userId()?.get('id');
    if (id) {
      this.loadUserData(+id);
    }
  });

  loadUserData(userId: number): void {
    this.isLoadingData.set(true);

    this.adminService.getUserById(userId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          const user = response.data;
          this.user.set(user);

          const roleValue = this.getRoleEnumValue(user.role);
          this.userForm.patchValue({
            role: roleValue,
            managerId: user.managerId,
          });

          this.userForm.get('role')?.valueChanges.subscribe((role) => {
            this.handleRoleChange(role);
          });

          this.handleRoleChange(roleValue);

          this.isLoadingData.set(false);
        } else {
          this.toastService.error('Failed to load user');
          this.router.navigate(['/admin']);
        }
      },
      error: (err) => {
        this.toastService.error(err.message || 'Failed to load user');
        this.router.navigate(['/admin']);
      },
    });
  }

  handleRoleChange(role: UserRole): void {
    const managerIdControl = this.userForm.get('managerId');

    if (role === UserRole.User) {
      managerIdControl?.enable();
    } else {
      managerIdControl?.setValue(null);
      managerIdControl?.disable();
    }
  }

  onSubmit(): void {
    if (this.userForm.invalid || !this.user()) {
      this.userForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    const currentUser = this.user()!;
    const formValue = this.userForm.getRawValue();

    const currentRoleEnum = this.getRoleEnumValue(currentUser.role);
    const newRole = formValue.role;
    const newManagerId = formValue.managerId;

    const roleChanged = currentRoleEnum !== newRole;
    const managerChanged = currentUser.managerId !== newManagerId;

    if (!roleChanged && !managerChanged) {
      this.toastService.info('No changes to save');
      this.isLoading.set(false);
      return;
    }

    if (roleChanged) {
      this.adminService.updateUserRole(currentUser.id, newRole).subscribe({
        next: (response) => {
          if (response.success) {
            this.toastService.success('Role updated successfully!');

            if (managerChanged && newRole === UserRole.User) {
              this.updateManager(currentUser.id, newManagerId);
            } else {
              this.completeUpdate();
            }
          } else {
            this.toastService.error(response.message);
            this.isLoading.set(false);
          }
        },
        error: (err) => {
          this.toastService.error(err.message || 'Failed to update role');
          this.isLoading.set(false);
        },
      });
    } else if (managerChanged) {
      this.updateManager(currentUser.id, newManagerId);
    }
  }

  updateManager(userId: number, managerId: number | null): void {
    if (managerId === null) {
      this.adminService.removeManager(userId).subscribe({
        next: (response) => {
          if (response.success) {
            this.toastService.success('Manager removed successfully!');
            this.completeUpdate();
          } else {
            this.toastService.error(response.message);
            this.isLoading.set(false);
          }
        },
        error: (err) => {
          this.toastService.error(err.message || 'Failed to remove manager');
          this.isLoading.set(false);
        },
      });
    } else {
      this.adminService.assignManager(userId, managerId).subscribe({
        next: (response) => {
          if (response.success) {
            this.toastService.success('Manager assigned successfully!');
            this.completeUpdate();
          } else {
            this.toastService.error(response.message);
            this.isLoading.set(false);
          }
        },
        error: (err) => {
          this.toastService.error(err.message || 'Failed to assign manager');
          this.isLoading.set(false);
        },
      });
    }
  }

  completeUpdate(): void {
    this.isLoading.set(false);
    this.adminService.loadUsers();
    this.router.navigate(['/admin']);
  }

  goBack(): void {
    this.router.navigate(['/admin']);
  }

  getUserFullName(user: UserDto): string {
    if (user.firstName && user.lastName) {
      return `${user.firstName} ${user.lastName}`;
    }
    return user.email;
  }

  private getRoleEnumValue(role: string): UserRole {
    switch (role) {
      case 'Admin':
        return UserRole.Admin;
      case 'Manager':
        return UserRole.Manager;
      case 'User':
      default:
        return UserRole.User;
    }
  }

  get roleControl() {
    return this.userForm.get('role');
  }

  get managerIdControl() {
    return this.userForm.get('managerId');
  }

  getRoleDescription(role: UserRole): string {
    return this.roleDescriptions[role] || '';
  }
}
