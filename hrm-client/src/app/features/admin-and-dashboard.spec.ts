import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { polyfillDialog, provideTestApp, signInAs, typeInto } from '../../testing/test-helpers';
import { Permissions } from '../core/auth/permissions';
import { DashboardSummary } from '../core/models/dashboard';
import { generatePassword, passwordPolicy } from '../shared/forms/passwords';
import { ChangePassword } from './account/change-password/change-password';
import { UserList } from './admin/users/user-list/user-list';
import { Dashboard } from './dashboard/dashboard/dashboard';

const page = (items: unknown[]) => ({
  items,
  page: 1,
  pageSize: 100,
  totalCount: items.length,
  totalPages: 1,
});
const text = (el: HTMLElement) => el.textContent?.replace(/\s+/g, ' ') ?? '';
/** "Label value" of each stat tile, read per <p> (adjacent paragraphs have no space in textContent). */
const tiles = (el: HTMLElement) =>
  [...el.querySelectorAll('.card')].map((c) =>
    [...c.querySelectorAll(':scope > p')].map((p) => p.textContent?.trim()).join(' '),
  );

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

describe('Dashboard, Users and Change password', () => {
  let http: HttpTestingController;
  beforeAll(polyfillDialog);

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: provideTestApp([{ path: 'login', children: [] }]),
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  describe('Dashboard', () => {
    const summary: DashboardSummary = {
      role: 'HR',
      today: '2030-01-07',
      organization: {
        headcount: 10,
        onLeaveToday: 1,
        pendingApprovals: 2,
        headcountByDepartment: [
          { departmentId: 'eng', departmentName: 'Engineering', headcount: 5 },
          { departmentId: 'sal', departmentName: 'Sales', headcount: 2 },
        ],
      },
      team: null,
      personal: {
        annualLeaveBalance: 12,
        pendingAnnualDays: 2,
        pendingRequests: 1,
        checkedInAt: null,
        checkedOutAt: null,
        recentRequests: [],
      },
    };

    it('shows the organization with bars scaled to the largest department', async () => {
      signInAs('HR', [Permissions.EmployeesView]);
      const fixture = TestBed.createComponent(Dashboard);
      TestBed.tick();
      http.expectOne('/api/dashboard').flush(summary);
      await fixture.whenStable();
      const el = fixture.nativeElement as HTMLElement;

      expect(text(el)).toContain('Hello, Test');
      expect(tiles(el)).toContain('Active employees 10');
      expect(tiles(el).some((t) => t.startsWith('Annual leave available 10'))).toBe(true); // 12 − 2 pending
      const bars = [
        ...el.querySelectorAll<HTMLElement>('ul span[title] ~ span span, li span.block'),
      ].map((b) => b.style.width);
      expect(bars).toEqual(['100%', '40%']);
      expect(text(el)).not.toContain('My team');
    });

    it('shows only the personal section to an employee', async () => {
      signInAs('Employee', [Permissions.EmployeesView]);
      const fixture = TestBed.createComponent(Dashboard);
      TestBed.tick();
      http.expectOne('/api/dashboard').flush({ ...summary, role: 'Employee', organization: null });
      await fixture.whenStable();

      expect(text(fixture.nativeElement)).not.toContain('Organization');
      expect(text(fixture.nativeElement)).toContain('Not checked in');
    });
  });

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
