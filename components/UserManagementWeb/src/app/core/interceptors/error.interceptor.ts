import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { ProblemDetails } from '../models/problem-details.model';
import { ToastService } from '../services/toast.service';

/**
 * Surfaces every failed HTTP call as a toast, then rethrows so callers can
 * still run request-specific recovery logic (e.g. a form staying open on a
 * 409 duplicate-email conflict). Understands the `ProblemDetails` shape
 * written by `GlobalExceptionHandler` on the gateway; falls back to a generic
 * message for network failures (status 0) or unexpected response bodies.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toastService = inject(ToastService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      toastService.error(extractMessage(error));
      return throwError(() => error);
    }),
  );
};

function extractMessage(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Could not reach the server. Check your connection and try again.';
  }

  const problem = error.error as ProblemDetails | undefined;
  if (problem?.errors?.length) {
    return problem.errors.join(' ');
  }
  if (problem?.detail) {
    return problem.detail;
  }
  if (problem?.title) {
    return problem.title;
  }

  return `Request failed (${error.status}).`;
}
