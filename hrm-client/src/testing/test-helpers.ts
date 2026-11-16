import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Routes } from '@angular/router';
import { authInterceptor } from '../app/core/auth/auth.interceptor';
import { AuthService } from '../app/core/auth/auth.service';
import { Permission, Role } from '../app/core/auth/permissions';

/** HttpClient (with the token interceptor) against HttpTestingController, plus a router. */
export function provideTestApp(routes: Routes = []) {
  return [
    provideHttpClient(withInterceptors([authInterceptor])),
    provideHttpClientTesting(),
    provideRouter(routes),
  ];
}

/** Signs in through AuthService with exactly these permissions. */
export function signInAs(
  role: Role,
  permissions: Permission[],
  employeeId: string | null = 'emp-1',
) {
  TestBed.inject(AuthService).login({ email: 'test@hrm.local', password: 'pw' }).subscribe();
  TestBed.inject(HttpTestingController)
    .expectOne('/api/auth/login')
    .flush({
      accessToken: 'token',
      expiresAt: '2030-01-01T00:00:00Z',
      user: {
        id: 'user-1',
        email: 'test@hrm.local',
        role,
        employeeId,
        fullName: 'Test User',
        permissions,
      },
    });
}

/** Sets an input's value the way typing does, so reactive forms see it. */
export function typeInto(root: HTMLElement, selector: string, value: string) {
  const element = root.querySelector<HTMLInputElement | HTMLSelectElement>(selector)!;
  element.value = value;
  element.dispatchEvent(new Event(element instanceof HTMLSelectElement ? 'change' : 'input'));
}

/** jsdom has <dialog> but not showModal()/close(); every real browser does. */
export function polyfillDialog() {
  const proto = HTMLDialogElement.prototype;
  proto.showModal ??= function (this: HTMLDialogElement) {
    this.setAttribute('open', '');
  };
  proto.close ??= function (this: HTMLDialogElement) {
    this.removeAttribute('open');
    this.dispatchEvent(new Event('close'));
  };
}
