import { CommonModule } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import {
  EmployeeType,
  getRoleDisplayName,
  UserDto,
  UserRateDto,
  UserRole,
} from '../../../core/models/admin.model';
import { ActivatedRoute, Router } from '@angular/router';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { Footer } from '../../../shared/footer/footer';
import { Nav } from '../../../shared/nav/nav';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-user-edit',
  imports: [CommonModule, ReactiveFormsModule, Footer, Nav, TranslatePipe],
  templateUrl: './user-edit.html',
  styleUrl: './user-edit.css',
})
export class UserEdit {
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private adminService = inject(AdminService);
  private toastService = inject(ToastService);
  private translateService = inject(TranslateService);

  userId = toSignal(this.route.paramMap);
  userForm: FormGroup;
  rateForm: FormGroup;
  isLoading = signal(false);
  isLoadingData = signal(true);
  isLoadingRates = signal(false);
  isSavingRate = signal(false);
  user = signal<UserDto | null>(null);
  rates = signal<UserRateDto[]>([]);
  selectedRole = signal<UserRole>(UserRole.User);
  salesRepHasValue = signal(false);

  UserRole = UserRole;
  EmployeeType = EmployeeType;
  getRoleDisplayName = getRoleDisplayName;

  availableManagers = computed(() =>
    this.adminService
      .users()
      .filter(
        (u) => u.role === 'Manager' && u.isActive && u.id !== this.user()?.id,
      ),
  );

  availableSalesReps = computed(() =>
    this.adminService
      .users()
      .filter(
        (u) => u.role === 'SalesRep' && u.isActive && u.id !== this.user()?.id,
      ),
  );

  constructor() {
    this.userForm = this.fb.group({
      role: [UserRole.User, [Validators.required]],
      employeeType: [null as EmployeeType | null],
      managerId: [null as number | null],
      salesRepId: [null as number | null],
    });

    this.rateForm = this.fb.group({
      hourlyRate: [null as number | null, [Validators.required, Validators.min(0)]],
      mileageRate: [null as number | null, [Validators.required, Validators.min(0)]],
      effectiveDate: ['', [Validators.required]],
    });

    this.adminService.loadUsers();
  }

  loadEffect = effect(() => {
    const id = this.userId()?.get('id');
    if (id) {
      this.loadUserData(+id);
      this.loadRates(+id);
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
            employeeType: this.getEmployeeTypeEnumValue(user.employeeType),
            managerId: user.managerId,
            salesRepId: user.salesRepId,
          });

          this.userForm.get('role')?.valueChanges.subscribe((role) => {
            this.handleRoleChange(role);
          });

          this.userForm.get('salesRepId')?.valueChanges.subscribe((val) => {
            this.handleSalesRepChange(val);
          });

          this.handleRoleChange(roleValue);

          this.isLoadingData.set(false);
        } else {
          this.toastService.error(this.translateService.instant('toast.failedLoadUser'));
          this.router.navigate(['/admin']);
        }
      },
      error: (err) => {
        this.toastService.error(err.message || this.translateService.instant('toast.failedLoadUser'));
        this.router.navigate(['/admin']);
      },
    });
  }

  handleRoleChange(role: UserRole): void {
    this.selectedRole.set(role);
    const managerIdControl = this.userForm.get('managerId');
    const employeeTypeControl = this.userForm.get('employeeType');
    const salesRepIdControl = this.userForm.get('salesRepId');

    if (role === UserRole.User) {
      managerIdControl?.enable();
      employeeTypeControl?.enable();
      salesRepIdControl?.enable();
      this.handleSalesRepChange(salesRepIdControl?.value);
    } else if (role === UserRole.SalesRep) {
      managerIdControl?.enable();
      employeeTypeControl?.setValue(null);
      employeeTypeControl?.disable();
      salesRepIdControl?.setValue(null);
      salesRepIdControl?.disable();
    } else {
      managerIdControl?.setValue(null);
      managerIdControl?.disable();
      employeeTypeControl?.setValue(null);
      employeeTypeControl?.disable();
      salesRepIdControl?.setValue(null);
      salesRepIdControl?.disable();
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
    const newSalesRepId = formValue.salesRepId;
    const newEmployeeType = formValue.employeeType as EmployeeType | null;

    const roleChanged = currentRoleEnum !== newRole;
    const managerChanged = currentUser.managerId !== newManagerId;
    const salesRepChanged = currentUser.salesRepId !== newSalesRepId;
    const currentEmployeeTypeEnum = this.getEmployeeTypeEnumValue(currentUser.employeeType);
    const employeeTypeChanged = currentEmployeeTypeEnum !== newEmployeeType;

    if (!roleChanged && !managerChanged && !salesRepChanged && !employeeTypeChanged) {
      this.toastService.info(this.translateService.instant('toast.noChanges'));
      this.isLoading.set(false);
      return;
    }

    const doEmployeeType = () => {
      if (employeeTypeChanged) {
        this.updateEmployeeType(currentUser.id, newEmployeeType);
      } else {
        this.completeUpdate();
      }
    };

    const doSalesRep = () => {
      if (salesRepChanged) {
        this.updateSalesRep(currentUser.id, newSalesRepId, doEmployeeType);
      } else {
        doEmployeeType();
      }
    };

    const doManager = () => {
      if (managerChanged) {
        this.updateManager(currentUser.id, newManagerId, doSalesRep);
      } else {
        doSalesRep();
      }
    };

    if (roleChanged) {
      this.adminService.updateUserRole(currentUser.id, newRole).subscribe({
        next: (response) => {
          if (response.success) {
            this.toastService.success(this.translateService.instant('toast.roleUpdated'));
            if (newRole === UserRole.User) {
              doManager();
            } else {
              doEmployeeType();
            }
          } else {
            this.toastService.error(response.message);
            this.isLoading.set(false);
          }
        },
        error: (err) => {
          this.toastService.error(err.message || this.translateService.instant('toast.failedUpdateRole'));
          this.isLoading.set(false);
        },
      });
    } else if (managerChanged) {
      this.updateManager(currentUser.id, newManagerId, doSalesRep);
    } else {
      doSalesRep();
    }
  }

  updateManager(userId: number, managerId: number | null, onComplete: () => void): void {
    if (managerId === null) {
      this.adminService.removeManager(userId).subscribe({
        next: (response) => {
          if (response.success) {
            this.toastService.success(this.translateService.instant('toast.managerRemoved'));
            onComplete();
          } else {
            this.toastService.error(response.message);
            this.isLoading.set(false);
          }
        },
        error: (err) => {
          this.toastService.error(err.message || this.translateService.instant('toast.failedRemoveManager'));
          this.isLoading.set(false);
        },
      });
    } else {
      this.adminService.assignManager(userId, managerId).subscribe({
        next: (response) => {
          if (response.success) {
            this.toastService.success(this.translateService.instant('toast.managerAssigned'));
            onComplete();
          } else {
            this.toastService.error(response.message);
            this.isLoading.set(false);
          }
        },
        error: (err) => {
          this.toastService.error(err.message || this.translateService.instant('toast.failedAssignManager'));
          this.isLoading.set(false);
        },
      });
    }
  }

  updateSalesRep(userId: number, salesRepId: number | null, onComplete: () => void): void {
    if (salesRepId === null) {
      this.adminService.removeSalesRep(userId).subscribe({
        next: (response) => {
          if (response.success) {
            this.toastService.success(this.translateService.instant('toast.salesRepRemoved'));
            onComplete();
          } else {
            this.toastService.error(response.message);
            this.isLoading.set(false);
          }
        },
        error: (err) => {
          this.toastService.error(err.message || this.translateService.instant('toast.failedRemoveSalesRep'));
          this.isLoading.set(false);
        },
      });
    } else {
      this.adminService.assignSalesRep(userId, salesRepId).subscribe({
        next: (response) => {
          if (response.success) {
            this.toastService.success(this.translateService.instant('toast.salesRepAssigned'));
            onComplete();
          } else {
            this.toastService.error(response.message);
            this.isLoading.set(false);
          }
        },
        error: (err) => {
          this.toastService.error(err.message || this.translateService.instant('toast.failedAssignSalesRep'));
          this.isLoading.set(false);
        },
      });
    }
  }

  updateEmployeeType(userId: number, employeeType: EmployeeType | null): void {
    this.adminService.updateEmployeeType(userId, employeeType).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.success(this.translateService.instant('toast.employeeTypeUpdated'));
          this.completeUpdate();
        } else {
          this.toastService.error(response.message);
          this.isLoading.set(false);
        }
      },
      error: (err) => {
        this.toastService.error(err.message || this.translateService.instant('toast.failedUpdateEmployeeType'));
        this.isLoading.set(false);
      },
    });
  }

  completeUpdate(): void {
    this.isLoading.set(false);
    this.adminService.loadUsers();
    this.router.navigate(['/admin']);
  }

  loadRates(userId: number): void {
    this.isLoadingRates.set(true);
    this.adminService.getUserRates(userId).subscribe({
      next: (response) => {
        if (response.success && response.data) {
          this.rates.set(response.data);
        }
        this.isLoadingRates.set(false);
      },
      error: () => {
        this.isLoadingRates.set(false);
      },
    });
  }

  onSubmitRate(): void {
    if (this.rateForm.invalid || !this.user()) {
      this.rateForm.markAllAsTouched();
      return;
    }

    this.isSavingRate.set(true);
    const { hourlyRate, mileageRate, effectiveDate } = this.rateForm.value;

    this.adminService
      .setUserRate(this.user()!.id, { hourlyRate, mileageRate, effectiveDate })
      .subscribe({
        next: (response) => {
          if (response.success) {
            this.toastService.success(this.translateService.instant('toast.rateSaved'));
            this.rateForm.reset();
            this.loadRates(this.user()!.id);
          } else {
            this.toastService.error(response.message);
          }
          this.isSavingRate.set(false);
        },
        error: (err) => {
          this.toastService.error(err.message || this.translateService.instant('toast.failedSaveRate'));
          this.isSavingRate.set(false);
        },
      });
  }

  goBack(): void {
    this.router.navigate(['/admin']);
  }

  viewTimesheets(): void {
    const id = this.userId()?.get('id');
    if (id) {
      this.router.navigate(['/admin/users', id, 'timesheets']);
    }
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
      case 'SalesRep':
        return UserRole.SalesRep;
      case 'User':
      default:
        return UserRole.User;
    }
  }

  private getEmployeeTypeEnumValue(employeeType: string | null | undefined): EmployeeType | null {
    switch (employeeType) {
      case 'Employee':
        return EmployeeType.Employee;
      case 'Contractor':
        return EmployeeType.Contractor;
      default:
        return null;
    }
  }

  get roleControl() {
    return this.userForm.get('role')!;
  }

  get employeeTypeControl() {
    return this.userForm.get('employeeType')!;
  }

  get managerIdControl() {
    return this.userForm.get('managerId')!;
  }

  get hourlyRateControl() {
    return this.rateForm.get('hourlyRate')!;
  }

  get mileageRateControl() {
    return this.rateForm.get('mileageRate')!;
  }

  get effectiveDateControl() {
    return this.rateForm.get('effectiveDate')!;
  }

  getRoleDescription(role: UserRole): string {
    switch (role) {
      case UserRole.User:
        return 'userEdit.roleDescUser';
      case UserRole.Manager:
        return 'userEdit.roleDescManager';
      case UserRole.Admin:
        return 'userEdit.roleDescAdmin';
      case UserRole.SalesRep:
        return 'userEdit.roleDescSalesRep';
      default:
        return '';
    }
  }

  handleSalesRepChange(salesRepId: number | null): void {
    this.salesRepHasValue.set(salesRepId !== null);
    if (this.selectedRole() !== UserRole.User) return;
    const managerIdControl = this.userForm.get('managerId');
    if (salesRepId !== null) {
      managerIdControl?.setValue(null);
      managerIdControl?.disable();
    } else {
      managerIdControl?.enable();
    }
  }

  get salesRepIdControl() {
    return this.userForm.get('salesRepId')!;
  }
}
