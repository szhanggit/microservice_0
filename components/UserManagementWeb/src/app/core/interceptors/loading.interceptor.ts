import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';

import { LoadingService } from '../services/loading.service';

/**
 * Increments/decrements `LoadingService`'s counter around every outgoing
 * request so the global spinner tracks any concurrent mix of in-flight calls.
 */
export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);

  loadingService.increment();
  return next(req).pipe(finalize(() => loadingService.decrement()));
};
