import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { authGuard, employeeRecordGuard, guestGuard, permissionGuard } from './auth.guards';
import { AuthService } from './auth.service';
import { authResponse } from './auth.service.spec';
import { Permissions } from './permissions';

describe('auth guards', () => {
  const route = {} as ActivatedRouteSnapshot;
  const state = (url: string) => ({ url }) as RouterStateSnapshot;
  const run = (guard: typeof authGuard, url = '/employees') =>
    TestBed.runInInjectionContext(() => guard(route, state(url)));
  const urlOf = (result: unknown) => (result as UrlTree).toString();

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  function signIn(employeeId: string | null = 'emp-1') {
    TestBed.inject(AuthService).login({ email: 'a@hrm.local', password: 'pw' }).subscribe();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/auth/login')
      .flush(authResponse('t', employeeId));
  }

  it('authGuard sends signed-out users to login and remembers where they were going', () => {
    expect(urlOf(run(authGuard, '/leave/approvals'))).toBe('/login?returnUrl=%2Fleave%2Fapprovals');
  });

  it('authGuard lets signed-in users through', () => {
    signIn();
    expect(run(authGuard)).toBe(true);
  });

  it('guestGuard keeps signed-in users away from the login page', () => {
    signIn();
    expect(urlOf(run(guestGuard))).toBe('/dashboard');
  });

  it('permissionGuard sends users without the permission to /forbidden', () => {
    signIn(); // Employee: EmployeesView + LeaveRequest only
    expect(run(permissionGuard(Permissions.EmployeesView))).toBe(true);
    expect(urlOf(run(permissionGuard(Permissions.UsersManage)))).toBe('/forbidden');
  });

  it('employeeRecordGuard blocks accounts without an employee record', () => {
    signIn(null); // like the system admin account
    expect(urlOf(run(employeeRecordGuard))).toBe('/forbidden');
  });
});
