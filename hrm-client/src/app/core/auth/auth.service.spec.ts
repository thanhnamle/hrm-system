import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { authResponse } from '../../../testing/test-helpers';
import { AuthService } from './auth.service';
import { Permissions } from './permissions';

describe('AuthService', () => {
  let auth: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'login', children: [] }]),
      ],
    });
    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('login keeps the token in memory and exposes the user and permissions', () => {
    auth.login({ email: 'huy.le@hrm.local', password: 'pw' }).subscribe();
    http.expectOne('/api/auth/login').flush(authResponse());

    expect(auth.isLoggedIn()).toBe(true);
    expect(auth.token).toBe('token-1');
    expect(auth.hasPermission(Permissions.LeaveRequest)).toBe(true);
    expect(auth.hasPermission(Permissions.UsersManage)).toBe(false);
    expect(localStorage.length).toBe(0); // never persisted
  });

  it('shares one refresh request between concurrent callers', () => {
    const tokens: string[] = [];
    auth.refresh().subscribe((t) => tokens.push(t));
    auth.refresh().subscribe((t) => tokens.push(t));

    // Two requests would rotate the cookie twice and trip the API's reuse detection.
    http.expectOne('/api/auth/refresh').flush(authResponse('token-2'));

    expect(tokens).toEqual(['token-2', 'token-2']);
  });

  it('restoreSession stays signed out when there is no valid refresh cookie', async () => {
    const restored = auth.restoreSession();
    http.expectOne('/api/auth/refresh').flush(null, { status: 401, statusText: 'Unauthorized' });
    await restored;

    expect(auth.isLoggedIn()).toBe(false);
  });

  it('logout sends the access token, then forgets the session', () => {
    auth.login({ email: 'huy.le@hrm.local', password: 'pw' }).subscribe();
    http.expectOne('/api/auth/login').flush(authResponse());

    auth.logout();

    const request = http.expectOne('/api/auth/logout');
    expect(request.request.headers.get('Authorization')).toBe('Bearer token-1');
    request.flush(null, { status: 204, statusText: 'No Content' });
    expect(auth.isLoggedIn()).toBe(false);
    expect(auth.token).toBeNull();
  });
});
