import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { authInterceptor, refreshInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';
import { authResponse } from '../../../testing/test-helpers';

describe('auth interceptors', () => {
  let client: HttpClient;
  let http: HttpTestingController;
  let auth: AuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor, refreshInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);

    auth.login({ email: 'huy.le@hrm.local', password: 'pw' }).subscribe();
    http.expectOne('/api/auth/login').flush(authResponse('token-1'));
  });

  afterEach(() => http.verify());

  it('adds the Bearer token to API calls only', () => {
    client.get('/api/employees').subscribe();
    client.get('https://example.com/data').subscribe();

    expect(http.expectOne('/api/employees').request.headers.get('Authorization')).toBe(
      'Bearer token-1',
    );
    expect(http.expectOne('https://example.com/data').request.headers.has('Authorization')).toBe(
      false,
    );
  });

  it('on 401 refreshes once and retries with the new token', () => {
    let body: unknown;
    client.get('/api/employees').subscribe((b) => (body = b));

    http.expectOne('/api/employees').flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/auth/refresh').flush(authResponse('token-2'));
    const retry = http.expectOne('/api/employees');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer token-2');
    retry.flush({ items: [] });

    expect(body).toEqual({ items: [] });
  });

  it('ends the session and goes to login when the refresh fails too', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    let failed = false;
    client.get('/api/employees').subscribe({ error: () => (failed = true) });

    http.expectOne('/api/employees').flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(failed).toBe(true);
    expect(auth.isLoggedIn()).toBe(false);
    expect(navigate).toHaveBeenCalledWith(['/login'], expect.anything());
  });

  it('does not try to refresh after a failed login', () => {
    auth.login({ email: 'x@hrm.local', password: 'wrong' }).subscribe({ error: () => undefined });

    http.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectNone('/api/auth/refresh');
  });
});
