import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { authResponse } from '../../../../testing/test-helpers';
import { Login } from './login';

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let http: HttpTestingController;
  let navigate: ReturnType<typeof vi.spyOn>;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    fixture = TestBed.createComponent(Login);
    http = TestBed.inject(HttpTestingController);
    navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    await fixture.whenStable(); // zoneless: render once so the form directives are bound
  });

  async function submit(returnUrl?: string) {
    if (returnUrl) fixture.componentRef.setInput('returnUrl', returnUrl);
    const page = fixture.nativeElement as HTMLElement;
    for (const [id, value] of [
      ['email', 'huy.le@hrm.local'],
      ['password', 'Secret#1'],
    ]) {
      const input = page.querySelector<HTMLInputElement>(`#${id}`)!;
      input.value = value;
      input.dispatchEvent(new Event('input'));
    }
    page.querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
  }

  it('does not call the API when the form is empty', async () => {
    (fixture.nativeElement as HTMLElement)
      .querySelector('form')!
      .dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    http.expectNone('/api/auth/login');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Enter a valid email address.',
    );
  });

  it('shows the generic message on 401', async () => {
    await submit();
    http.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();

    expect(
      (fixture.nativeElement as HTMLElement).querySelector('[role=alert]')?.textContent,
    ).toContain('Invalid email or password.');
  });

  it('returns to the page the user came from', async () => {
    await submit('/leave/approvals');
    http.expectOne('/api/auth/login').flush(authResponse());

    expect(navigate).toHaveBeenCalledWith('/leave/approvals');
  });

  it.each(['https://evil.example', '//evil.example', '/login'])(
    'ignores an unsafe returnUrl (%s) and goes to the dashboard',
    async (returnUrl) => {
      await submit(returnUrl);
      http.expectOne('/api/auth/login').flush(authResponse());

      expect(navigate).toHaveBeenCalledWith('/dashboard');
    },
  );
});
