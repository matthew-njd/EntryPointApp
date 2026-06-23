import { Component, computed, inject, signal, OnInit, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Nav } from '../../../shared/nav/nav';
import { Footer } from '../../../shared/footer/footer';
import { Modal } from '../../../shared/modal/modal';
import { ToastService } from '../../../core/services/toast.service';
import { ApprovedEmailsService } from '../../../core/services/approved-emails.service';
import { ApprovedEmailDto } from '../../../core/models/approved-emails.model';

@Component({
  selector: 'app-admin-approved-emails',
  imports: [CommonModule, FormsModule, Nav, Footer, Modal],
  templateUrl: './admin-approved-emails.html',
})
export class AdminApprovedEmails implements OnInit {
  private service = inject(ApprovedEmailsService);
  private toastService = inject(ToastService);
  private router = inject(Router);

  emails = signal<ApprovedEmailDto[]>([]);
  isLoading = signal(false);
  isAdding = signal(false);
  newEmail = '';
  removeEmailModal = viewChild<Modal>('removeEmailModal');
  pendingEmailEntry = signal<ApprovedEmailDto | null>(null);
  removeEmailBody = computed(() => `Remove "${this.pendingEmailEntry()?.email}" from the approved list?`);

  ngOnInit(): void {
    this.loadEmails();
  }

  loadEmails(): void {
    this.isLoading.set(true);
    this.service.getAll().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.emails.set(res.data);
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load approved emails');
        this.isLoading.set(false);
      },
    });
  }

  addEmail(): void {
    const trimmed = this.newEmail.trim();
    if (!trimmed) {
      this.toastService.error('Please enter an email address');
      return;
    }

    this.isAdding.set(true);
    this.service.add({ email: trimmed }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.emails.update((list) => [...list, res.data!].sort((a, b) => a.email.localeCompare(b.email)));
          this.newEmail = '';
          this.toastService.success('Email approved successfully');
        } else {
          this.toastService.error(res.message || 'Failed to add email');
        }
        this.isAdding.set(false);
      },
      error: () => {
        this.toastService.error('Failed to add email');
        this.isAdding.set(false);
      },
    });
  }

  removeEmail(entry: ApprovedEmailDto): void {
    this.pendingEmailEntry.set(entry);
    this.removeEmailModal()?.open();
  }

  onRemoveEmailConfirmed(): void {
    const entry = this.pendingEmailEntry();
    if (!entry) return;

    this.service.remove(entry.id).subscribe({
      next: (res) => {
        if (res.success) {
          this.emails.update((list) => list.filter((e) => e.id !== entry.id));
          this.toastService.success('Email removed');
        } else {
          this.toastService.error(res.message || 'Failed to remove email');
        }
        this.pendingEmailEntry.set(null);
      },
      error: () => {
        this.toastService.error('Failed to remove email');
        this.pendingEmailEntry.set(null);
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/admin']);
  }
}
