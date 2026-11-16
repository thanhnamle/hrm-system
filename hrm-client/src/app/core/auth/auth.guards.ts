import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { Permission } from './permissions';

/**
 * Route guards are layer 0 in docs/rbac.md: they keep the UI tidy, not the data safe.
 * The API checks every request again, so a user who bypasses a guard still gets 401/403.
 */

/** Signed-in users only; others go to /login and come back afterwards. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  return auth.isLoggedIn()
    ? true
    : inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

/** The login page is for signed-out users; a signed-in user goes straight to the app. */
export const guestGuard: CanActivateFn = () =>
  inject(AuthService).isLoggedIn() ? inject(Router).createUrlTree(['/dashboard']) : true;

/** The user's role must hold the permission, otherwise /forbidden. Use after authGuard. */
export const permissionGuard =
  (permission: Permission): CanActivateFn =>
  () =>
    inject(AuthService).hasPermission(permission) || inject(Router).createUrlTree(['/forbidden']);

/**
 * Self-service pages (my leave, my attendance) need an employee record; the system admin
 * account has none, so the API would answer 403 for everything on those pages.
 */
export const employeeRecordGuard: CanActivateFn = () =>
  inject(AuthService).hasEmployeeRecord() || inject(Router).createUrlTree(['/forbidden']);
