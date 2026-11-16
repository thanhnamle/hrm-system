import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

/**
 * Auth endpoints never get the token added here and never trigger a refresh
 * (logout sets its own Authorization header in AuthService.logout).
 */
const isAuthEndpoint = (url: string) => /\/auth\/(login|refresh|logout)$/.test(url);

const isApiCall = (url: string) => url.startsWith(environment.apiBaseUrl);

const withToken = (request: HttpRequest<unknown>, token: string) =>
  request.clone({ setHeaders: { Authorization: `Bearer ${token}` } });

/** Adds the access token to API calls. Only our own API, never third-party URLs. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(AuthService).token;
  return token && isApiCall(request.url) && !isAuthEndpoint(request.url)
    ? next(withToken(request, token))
    : next(request);
};

/**
 * On a 401 (access token expired after 15 minutes), refreshes once and retries the request.
 * If the refresh also fails, the session is over: back to the login page.
 */
export const refreshInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return next(request).pipe(
    catchError((error: unknown) => {
      const expired =
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        isApiCall(request.url) &&
        !isAuthEndpoint(request.url);
      if (!expired) return throwError(() => error);

      return auth.refresh().pipe(
        switchMap((token) => next(withToken(request, token))),
        catchError((refreshError: unknown) => {
          auth.endSession();
          void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
