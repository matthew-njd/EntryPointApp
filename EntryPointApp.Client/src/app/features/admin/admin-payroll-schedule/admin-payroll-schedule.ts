import { Component, inject, signal, OnInit, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Nav } from '../../../shared/nav/nav';
import { Footer } from '../../../shared/footer/footer';
import { Modal } from '../../../shared/modal/modal';
import { ToastService } from '../../../core/services/toast.service';
import {
  PayrollScheduleService,
  PayrollScheduleEntry,
} from '../../../core/services/payroll-schedule.service';

interface EditState {
  dateFrom: string;
  dateTo: string;
  payrollDate: string;
}

@Component({
  selector: 'app-admin-payroll-schedule',
  imports: [CommonModule, FormsModule, Nav, Footer, Modal],
  templateUrl: './admin-payroll-schedule.html',
})
export class AdminPayrollSchedule implements OnInit {
  private service = inject(PayrollScheduleService);
  private toastService = inject(ToastService);
  private router = inject(Router);

  entries = signal<PayrollScheduleEntry[]>([]);
  isLoading = signal(false);

  editingId = signal<number | null>(null);
  editState: EditState = { dateFrom: '', dateTo: '', payrollDate: '' };

  newEntry: EditState = { dateFrom: '', dateTo: '', payrollDate: '' };
  isAdding = signal(false);

  csvFile = signal<File | null>(null);
  replaceExisting = signal(true);
  isImporting = signal(false);
  deleteEntryModal = viewChild<Modal>('deleteEntryModal');
  pendingDeleteId = signal<number | null>(null);

  ngOnInit(): void {
    this.loadEntries();
  }

  loadEntries(): void {
    this.isLoading.set(true);
    this.service.getAll().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.entries.set(res.data);
        }
        this.isLoading.set(false);
      },
      error: () => {
        this.toastService.error('Failed to load payroll schedule');
        this.isLoading.set(false);
      },
    });
  }

  startEdit(entry: PayrollScheduleEntry): void {
    this.editingId.set(entry.id);
    this.editState = {
      dateFrom: entry.dateFrom,
      dateTo: entry.dateTo,
      payrollDate: entry.payrollDate,
    };
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(id: number): void {
    this.service.update(id, this.editState).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.entries.update((list) =>
            list.map((e) => (e.id === id ? res.data! : e))
          );
          this.editingId.set(null);
          this.toastService.success('Entry updated');
        } else {
          this.toastService.error(res.message || 'Failed to update entry');
        }
      },
      error: () => this.toastService.error('Failed to update entry'),
    });
  }

  deleteEntry(id: number): void {
    this.pendingDeleteId.set(id);
    this.deleteEntryModal()?.open();
  }

  onDeleteEntryConfirmed(): void {
    const id = this.pendingDeleteId();
    if (id === null) return;

    this.service.delete(id).subscribe({
      next: (res) => {
        if (res.success) {
          this.entries.update((list) => list.filter((e) => e.id !== id));
          this.toastService.success('Entry deleted');
        } else {
          this.toastService.error(res.message || 'Failed to delete entry');
        }
        this.pendingDeleteId.set(null);
      },
      error: () => {
        this.toastService.error('Failed to delete entry');
        this.pendingDeleteId.set(null);
      },
    });
  }

  addEntry(): void {
    if (!this.newEntry.dateFrom || !this.newEntry.dateTo || !this.newEntry.payrollDate) {
      this.toastService.error('All three date fields are required');
      return;
    }

    this.isAdding.set(true);
    this.service.create(this.newEntry).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.entries.update((list) => [...list, res.data!]);
          this.newEntry = { dateFrom: '', dateTo: '', payrollDate: '' };
          this.toastService.success('Entry added');
        } else {
          this.toastService.error(res.message || 'Failed to add entry');
        }
        this.isAdding.set(false);
      },
      error: () => {
        this.toastService.error('Failed to add entry');
        this.isAdding.set(false);
      },
    });
  }

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.csvFile.set(input.files?.[0] ?? null);
  }

  importCsv(): void {
    const file = this.csvFile();
    if (!file) {
      this.toastService.error('Please select a CSV file first');
      return;
    }

    this.isImporting.set(true);
    this.service.importCsv(file, this.replaceExisting()).subscribe({
      next: (res) => {
        if (res.success) {
          this.toastService.success(res.message);
          if (res.errors && res.errors.length > 0) {
            res.errors.forEach((e) => this.toastService.error(e));
          }
          this.csvFile.set(null);
          this.loadEntries();
        } else {
          this.toastService.error(res.message || 'Import failed');
        }
        this.isImporting.set(false);
      },
      error: () => {
        this.toastService.error('Import failed');
        this.isImporting.set(false);
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/admin']);
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr + 'T00:00:00');
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
