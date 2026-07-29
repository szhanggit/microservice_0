import { Component, inject } from '@angular/core';

import { Toast, ToastService } from '../../../core/services/toast.service';

/**
 * Renders whatever `ToastService.toasts()` currently holds. Mounted once in
 * `App`'s root template so any feature can call `ToastService` without
 * threading a container through routes.
 */
@Component({
  selector: 'app-toast-container',
  templateUrl: './toast-container.html',
  styleUrl: './toast-container.scss',
})
export class ToastContainer {
  private readonly toastService = inject(ToastService);
  protected readonly toasts = this.toastService.toasts;

  protected variantClass(variant: Toast['variant']): string {
    switch (variant) {
      case 'success':
        return 'text-bg-success';
      case 'error':
        return 'text-bg-danger';
      default:
        return 'text-bg-info';
    }
  }

  protected dismiss(id: number): void {
    this.toastService.dismiss(id);
  }
}
