// services/toast.service.ts
import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info' | 'warning';
  duration: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 0;
  readonly toasts = signal<Toast[]>([]);

  success(message: string, duration = 3000) {
    this.show('success', message, duration);
  }

  error(message: string, duration = 5000) {
    this.show('error', message, duration);
  }

  info(message: string, duration = 5000) {
    this.show('info', message, duration);
  }

  warning(message: string, duration = 5000) {
    this.show('warning', message, duration);
  }

  private show(type: Toast['type'], message: string, duration: number) {
    const toast: Toast = {
      id: this.nextId++,
      message,
      type,
      duration,
    };

    this.toasts.update((t) => [...t, toast]);

    setTimeout(() => {
      this.remove(toast.id);
    }, duration);
  }

  remove(id: number) {
    this.toasts.update((t) => t.filter((toast) => toast.id !== id));
  }
}
