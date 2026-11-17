import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { App } from './app';
import { routes } from './app.routes';
import { AuthService } from './core/auth/auth.service';
import { authResponse } from './core/auth/auth.service.spec';

describe('App', () => {
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    router = TestBed.inject(Router);
  });

  async function open(url: string) {
    const fixture = TestBed.createComponent(App);
    await router.navigateByUrl(url);
    // The dashboard loads its data; answer that request, or whenStable() would wait for it.
    TestBed.tick();
    TestBed.inject(HttpTestingController)
      .match('/api/dashboard')
      .forEach((r) =>
        r.flush({
          role: 'Employee',
          today: '2030-01-07',
          organization: null,
          team: null,
          personal: null,
        }),
      );
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  function signIn() {
    TestBed.inject(AuthService).login({ email: 'huy.le@hrm.local', password: 'pw' }).subscribe();
    TestBed.inject(HttpTestingController).expectOne('/api/auth/login').flush(authResponse());
  }

  it('sends signed-out visitors to the login page, remembering the page they wanted', async () => {
    const page = await open('/');

    expect(router.url).toBe('/login?returnUrl=%2Fdashboard');
    expect(page.querySelector('h1')?.textContent).toContain('Sign in');
  });

  it('shows the dashboard inside the shell once signed in', async () => {
    signIn();
    const page = await open('/');

    expect(router.url).toBe('/dashboard');
    expect(page.querySelector('app-sidebar')).toBeTruthy();
    expect(page.querySelector('h1')?.textContent).toMatch(/Good (morning|afternoon|evening), Huy/);
  });

  it('shows only the menu items the role may use', async () => {
    signIn(); // Employee with EmployeesView + LeaveRequest
    const page = await open('/dashboard');

    const labels = [...page.querySelectorAll('app-sidebar nav a')].map((a) =>
      a.textContent?.trim(),
    );
    expect(labels).toEqual(['HRM', 'Dashboard', 'Employees', 'My leave']);
  });

  it('sends a role without the permission to /forbidden', async () => {
    signIn();
    await open('/admin/users');

    expect(router.url).toBe('/forbidden');
  });

  it('shows the not-found page for unknown URLs', async () => {
    const page = await open('/does-not-exist');

    expect(page.querySelector('h1')?.textContent).toContain('Page not found');
  });
});
