import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { polyfillDialog, provideTestApp, signInAs, typeInto } from '../../testing/test-helpers';
import { Permissions } from '../core/auth/permissions';
import { generatePassword, passwordPolicy } from '../shared/forms/passwords';
import { ChangePassword } from './account/change-password/change-password';
import { UserList } from './admin/users/user-list/user-list';

const page = (items: unknown[]) => ({
  items,
  page: 1,
  pageSize: 100,
  totalCount: items.length,
  totalPages: 1,
});
const text = (el: HTMLElement) => el.textContent?.replace(/\s+/g, ' ') ?? '';

describe('passwords', () => {
  it('generated passwords always satisfy the policy', () => {
    for (let i = 0; i < 50; i++) {
      expect(passwordPolicy(new FormControl(generatePassword()))).toBeNull();
    }
  });

  it.each(['short1!', 'alllowercase1!', 'ALLUPPER1!', 'NoDigits!!', 'NoSymbol123'])(
    'rejects %s',
    (pw) => {
      expect(passwordPolicy(new FormControl(pw))).not.toBeNull();
    },
  );
});

describe('Users and Change password', () => {
  let http: HttpTestingController;
  beforeAll(polyfillDialog);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideTestApp([{ path: 'login', children: [] }]),
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  describe('UserList', () => {
    async function render() {
      signInAs('Admin', [Permissions.UsersManage], null);
      const fixture = TestBed.createComponent(UserList);
      TestBed.tick();
      http.expectOne('/api/users').flush([
        {
          id: 'user-1',
          email: 'test@hrm.local',
          role: 'Admin',
          employeeId: null,
          fullName: null,
          isLockedOut: false,
        },
        {
          id: 'u2',
          email: 'huy.le@hrm.local',
          role: 'Employee',
          employeeId: 'e1',
          fullName: 'Huy Le',
          isLockedOut: false,
        },
      ]);
      http
        .expectOne((r) => r.url === '/api/employees')
        .flush(
          page([
            { id: 'e1', fullName: 'Huy Le', email: 'huy.le@hrm.local' },
            { id: 'e2', fullName: 'Nam Le', email: 'nam.le@hrm.local' },
          ]),
        );
      await fixture.whenStable();
      return { fixture, el: fixture.nativeElement as HTMLElement };
    }

    it('has no actions on your own row', async () => {
      const { el } = await render();
      const rows = [...el.querySelectorAll('tbody tr')];

      expect(text(rows[0] as HTMLElement)).toContain('You');
      expect(rows[0].querySelectorAll('button').length).toBe(0);
      expect(rows[1].querySelectorAll('button').length).toBe(3);
    });

    it('offers only employees without a login and sends a valid generated password', async () => {
      const { fixture, el } = await render();
      [...el.querySelectorAll('button')]
        .find((b) => b.textContent?.trim() === 'Create login')!
        .click();
      await fixture.whenStable();

      const options = [...el.querySelectorAll<HTMLOptionElement>('#employeeId option')].map((o) =>
        o.textContent?.trim(),
      );
      expect(options).toEqual(['Choose an employee', 'Nam Le · nam.le@hrm.local']);

      typeInto(el, '#employeeId', 'e2');
      el.querySelector<HTMLFormElement>('#create-form')!.dispatchEvent(new Event('submit'));
      const post = http.expectOne((r) => r.method === 'POST' && r.url === '/api/users');
      expect(post.request.body.employeeId).toBe('e2');
      expect(passwordPolicy(new FormControl(post.request.body.password))).toBeNull();
      post.flush({ id: 'u3' }, { status: 201, statusText: 'Created' });
      TestBed.tick();
      http.expectOne('/api/users').flush([]);
    });
  });

  describe('ChangePassword', () => {
    async function render() {
      signInAs('Employee', []);
      const fixture = TestBed.createComponent(ChangePassword);
      await fixture.whenStable();
      const el = fixture.nativeElement as HTMLElement;
      typeInto(el, '#currentPassword', 'Old#Pass1');
      typeInto(el, '#newPassword', 'New#Pass2x');
      return { fixture, el };
    }

    it('requires the repeat to match', async () => {
      const { fixture, el } = await render();
      typeInto(el, '#repeatPassword', 'Different#1');
      el.querySelector('form')!.dispatchEvent(new Event('submit'));
      await fixture.whenStable();

      http.expectNone('/api/auth/change-password');
      expect(text(el)).toContain('The passwords do not match.');
    });

    it('shows a wrong current password next to that field', async () => {
      const { fixture, el } = await render();
      typeInto(el, '#repeatPassword', 'New#Pass2x');
      el.querySelector('form')!.dispatchEvent(new Event('submit'));

      http
        .expectOne('/api/auth/change-password')
        .flush(
          { errors: { CurrentPassword: ['Current password is incorrect.'] } },
          { status: 400, statusText: 'Bad Request' },
        );
      await fixture.whenStable();

      expect(el.querySelector('#currentPassword-error')?.textContent).toContain(
        'Current password is incorrect.',
      );
    });

    it('signs out after a successful change', async () => {
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
      const { el } = await render();
      typeInto(el, '#repeatPassword', 'New#Pass2x');
      el.querySelector('form')!.dispatchEvent(new Event('submit'));

      http
        .expectOne('/api/auth/change-password')
        .flush(null, { status: 204, statusText: 'No Content' });
      http.expectOne('/api/auth/logout').flush(null, { status: 204, statusText: 'No Content' });

      expect(navigate).toHaveBeenCalledWith('/login');
    });
  });
});
