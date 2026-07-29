import { Injectable, computed, signal } from '@angular/core';

/**
 * Tracks the number of in-flight HTTP requests so a single global spinner
 * (shared/components/spinner) can reflect overall network activity.
 * `loadingInterceptor` is the only writer; components only ever read
 * `isLoading()`.
 */
@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly activeRequestCount = signal(0);
  readonly isLoading = computed(() => this.activeRequestCount() > 0);

  increment(): void {
    this.activeRequestCount.update((count) => count + 1);
  }

  decrement(): void {
    this.activeRequestCount.update((count) => Math.max(0, count - 1));
  }
}
