import { HttpClient, HttpContext } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, finalize, firstValueFrom, map, Observable, of, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SKIP_ERROR_TOAST } from '../http/http-context';
import { AuthResponse, AuthUser, LoginRequest } from '../models/auth';
import { Permission } from './permissions';

/**
 * Who is signed in, and their tokens.
 *
 * - The access token lives only in memory (this service). It is never written to localStorage,
 *   where any injected script could read it. A page reload therefore loses it, and
 *   restoreSession() gets a new one from the HttpOnly refresh cookie.
 * - Refreshes are single-flight: the API rotates the refresh token on every refresh and treats a
 *   reused token as theft, so two parallel refreshes would sign the user out everywhere.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly baseUrl = `${environment.apiBaseUrl}/auth`;

  private readonly currentUser = signal<AuthUser | null>(null);
  private accessToken: string | null = null;
  private refreshInFlight: Observable<string> | null = null;

  readonly user = this.currentUser.asReadonly();
  readonly isLoggedIn = computed(() => this.currentUser() !== null);
  readonly hasEmployeeRecord = computed(() => !!this.currentUser()?.employeeId);
  private readonly permissionSet = computed(() => new Set(this.currentUser()?.permissions ?? []));

  get token(): string | null {
    return this.accessToken;
  }

  hasPermission(permission: Permission): boolean {
    return this.permissionSet().has(permission);
  }

  login(request: LoginRequest): Observable<AuthUser> {
    // The login page shows its own error message, so no global toast for this call.
    const context = new HttpContext().set(SKIP_ERROR_TOAST, true);
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, request, { context }).pipe(
      tap((response) => this.startSession(response)),
      map((response) => response.user),
    );
  }

  /** Gets a new access token from the refresh cookie. Concurrent callers share one request. */
  refresh(): Observable<string> {
    this.refreshInFlight ??= this.http
      .post<AuthResponse>(`${this.baseUrl}/refresh`, null, {
        context: new HttpContext().set(SKIP_ERROR_TOAST, true),
      })
      .pipe(
        tap((response) => this.startSession(response)),
        map((response) => response.accessToken),
        finalize(() => (this.refreshInFlight = null)),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    return this.refreshInFlight;
  }

  /** Runs once at startup: a valid refresh cookie means the user is still signed in. */
  restoreSession(): Promise<void> {
    return firstValueFrom(
      this.refresh().pipe(
        map(() => undefined),
        catchError(() => of(undefined)), // no or expired cookie: stay signed out
      ),
    );
  }

  logout(): void {
    // Revoke the refresh token on the server; sign out locally whatever the result.
    // The endpoint needs the access token, set here explicitly because the session is cleared below.
    const headers: Record<string, string> = this.accessToken
      ? { Authorization: `Bearer ${this.accessToken}` }
      : {};
    this.http
      .post(`${this.baseUrl}/logout`, null, {
        headers,
        context: new HttpContext().set(SKIP_ERROR_TOAST, true),
      })
      .pipe(catchError(() => of(null)))
      .subscribe();
    this.endSession();
    void this.router.navigateByUrl('/login');
  }

  /** Forgets the session without calling the API, e.g. after a failed refresh. */
  endSession(): void {
    this.accessToken = null;
    this.currentUser.set(null);
  }

  private startSession(response: AuthResponse): void {
    this.accessToken = response.accessToken;
    this.currentUser.set(response.user);
  }
}
