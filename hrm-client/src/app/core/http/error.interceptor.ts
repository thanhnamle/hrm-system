import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ProblemDetails } from '../models/common';
import { ToastService } from '../notifications/toast.service';
import { SKIP_ERROR_TOAST } from './http-context';

/**
 * Turns API errors into a toast with the server's message (ProblemDetails.detail).
 * The error is still passed on, so the caller can react too.
 *
 * Not toasted: 400 (forms show field errors next to the inputs) and 401 (handled by the
 * refresh interceptor or the login page).
 */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const toast = inject(ToastService);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        !request.context.get(SKIP_ERROR_TOAST) &&
        error.status !== 400 &&
        error.status !== 401
      ) {
        toast.error(messageFor(error));
      }
      return throwError(() => error);
    }),
  );
};

export function messageFor(error: HttpErrorResponse): string {
  const problem = error.error as ProblemDetails | null;
  switch (error.status) {
    case 0:
      return 'Cannot reach the server. Check your connection and try again.';
    case 403:
      return problem?.detail ?? 'You do not have permission to do that.';
    case 429:
      return 'Too many attempts. Please wait a minute and try again.';
    default:
      return error.status >= 500
        ? 'Something went wrong on the server. Please try again.'
        : (problem?.detail ?? problem?.title ?? 'The request failed.');
  }
}
