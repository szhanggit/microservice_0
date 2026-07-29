import { Injectable, signal } from '@angular/core';

export type ToastVariant = 'success' | 'error' | 'info';

export interface Toast {
  readonly id: number;
  readonly message: string;
  readonly variant: ToastVariant;
}

const AUTO_DISMISS_MS = 5000;

/**
 * App-wide notification queue backed by a signal. `ToastContainer`
 * (shared/components/toast-container) renders whatever is in `toasts()`;
 * nothing else needs to know a container exists.
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly _toasts = signal<readonly Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();

  private nextId = 0;

  success(message: string): void {
    this.show(message, 'success');
  }

  error(message: string): void {
    this.show(message, 'error');
  }

  info(message: string): void {
    this.show(message, 'info');
  }

  dismiss(id: number): void {
    this._toasts.update((toasts) => toasts.filter((toast) => toast.id !== id));
  }

  private show(message: string, variant: ToastVariant): void {
    const id = this.nextId++;
    this._toasts.update((toasts) => [...toasts, { id, message, variant }]);
    setTimeout(() => this.dismiss(id), AUTO_DISMISS_MS);
  }
}
