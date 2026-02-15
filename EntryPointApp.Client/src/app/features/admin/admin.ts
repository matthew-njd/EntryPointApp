import { Component, computed, effect, inject, signal } from '@angular/core';
import { Nav } from '../../shared/nav/nav';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  getRoleDisplayName,
  UserDto,
  UserRole,
} from '../../core/models/admin.model';
import { AdminService } from '../../core/services/admin.service';
import { Router } from '@angular/router';
import { ToastService } from '../../core/services/toast.service';
import { Card } from '../../shared/card/card';
import { Footer } from '../../shared/footer/footer';

@Component({
  selector: 'app-admin',
  imports: [CommonModule, FormsModule, Nav, Card, Footer],
  templateUrl: './admin.html',
  styleUrl: './admin.css',
})
export class Admin {
  readonly service = inject(AdminService);
  private router = inject(Router);
  private toastService = inject(ToastService);

  // Filter state
  roleFilter = signal<string>('All');
  searchQuery = signal<string>('');

  // Filtered users
  filteredUsers = computed(() => {
    let users = this.service.users();

    // Filter by role
    if (this.roleFilter() !== 'All') {
      users = users.filter((u) => u.role === this.roleFilter());
    }

    // Filter by search query
    const query = this.searchQuery().toLowerCase();
    if (query) {
      users = users.filter(
        (u) =>
          u.email.toLowerCase().includes(query) ||
          `${u.firstName} ${u.lastName}`.toLowerCase().includes(query),
      );
    }

    return users;
  });

  loadEffect = effect(() => {
    this.service.loadUsers();
  });

  editUser(userId: number) {
    this.router.navigate(['/admin/users', userId, 'edit']);
  }

  removeManager(user: UserDto) {
    if (!confirm(`Remove manager from ${user.email}?`)) return;

    this.service.removeManager(user.id).subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.success(response.message);
          this.service.loadUsers();
        } else {
          this.toastService.error(response.message);
        }
      },
      error: (err) => {
        this.toastService.error(err.message || 'Failed to remove manager');
      },
    });
  }

  toggleUserStatus(user: UserDto) {
    const action = user.isActive ? 'deactivate' : 'activate';
    if (!confirm(`Are you sure you want to ${action} ${user.email}?`)) return;

    const operation = user.isActive
      ? this.service.deactivateUser(user.id)
      : this.service.activateUser(user.id);

    operation.subscribe({
      next: (response) => {
        if (response.success) {
          this.toastService.success(response.message);
          this.service.loadUsers();
        } else {
          this.toastService.error(response.message);
        }
      },
      error: (err) => {
        this.toastService.error(err.message || `Failed to ${action} user`);
      },
    });
  }

  getUserFullName(user: UserDto): string {
    if (user.firstName && user.lastName) {
      return `${user.firstName} ${user.lastName}`;
    }
    return user.email;
  }

  getRoleClass(role: string): string {
    switch (role) {
      case 'Admin':
        return 'badge-error';
      case 'Manager':
        return 'badge-warning';
      case 'User':
        return 'badge-info';
      default:
        return '';
    }
  }
}
