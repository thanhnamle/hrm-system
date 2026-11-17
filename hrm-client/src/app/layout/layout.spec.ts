import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Routes } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTestApp, signInAs } from '../../testing/test-helpers';
import { Permissions } from '../core/auth/permissions';
import { Shell } from './shell/shell';
import { initials } from './sidebar/sidebar';

const page = (title: string) => ({ title: `${title} · HRM`, children: [] });

const routes: Routes = [
  { path: 'login', children: [] },
  {
    path: '',
    component: Shell,
    children: [
      { path: 'dashboard', ...page('Dashboard') },
      { path: 'employees', ...page('Employees') },
      { path: 'employees/:id', ...page('Employee') },
      { path: 'leave', ...page('My leave') },
      { path: 'leave/approvals', ...page('Approvals') },
      { path: 'account/password', ...page('Change password') },
    ],
  },
];

const words = (el: Element | null) => el?.textContent?.replace(/\s+/g, ' ').trim() ?? '';

describe('Layout', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideTestApp(routes) });
    http = TestBed.inject(HttpTestingController);
  });

  /** Answers every pending-approvals call (one per navigation) with `count`. */
  function flushPending(count: number) {
    for (const req of http.match((r) => r.url === '/api/leave-requests/pending')) {
      req.flush({ items: [], page: 1, pageSize: 1, totalCount: count, totalPages: 1 });
    }
  }

  async function open(url: string, pending = 0) {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl(url);
    TestBed.tick();
    flushPending(pending);
    await harness.fixture.whenStable();
    return { harness, el: harness.fixture.nativeElement as HTMLElement };
  }

  const link = (el: HTMLElement, label: string) =>
    [...el.querySelectorAll<HTMLAnchorElement>('app-sidebar nav a')].find((a) =>
      words(a).startsWith(label),
    )!;

  it('groups the menu, and shows an approver how many requests wait', async () => {
    signInAs('Manager', [
      Permissions.EmployeesView,
      Permissions.LeaveRequest,
      Permissions.LeaveApprove,
    ]);
    const { el } = await open('/dashboard', 3);

    expect([...el.querySelectorAll('app-sidebar nav p')].map(words)).toEqual([
      'Overview',
      'People',
      'Time off',
    ]);
    expect(words(link(el, 'Approvals').querySelector('[aria-label]'))).toBe('3');
    expect(link(el, 'Approvals').querySelector('[aria-label]')?.getAttribute('aria-label')).toBe(
      '3 waiting',
    );
  });

  it('never asks for the count without the approve permission', async () => {
    signInAs('Employee', [Permissions.LeaveRequest]);
    await open('/dashboard');

    http.expectNone('/api/leave-requests/pending');
  });

  it('marks the current page: exact for /leave, by prefix for employee pages', async () => {
    signInAs('Manager', [
      Permissions.EmployeesView,
      Permissions.LeaveRequest,
      Permissions.LeaveApprove,
    ]);
    const { harness, el } = await open('/leave/approvals');

    expect(link(el, 'Approvals').getAttribute('aria-current')).toBe('page');
    expect(link(el, 'My leave').getAttribute('aria-current')).toBeNull();

    await harness.navigateByUrl('/employees/42');
    flushPending(0);
    await harness.fixture.whenStable();
    expect(link(el, 'Employees').getAttribute('aria-current')).toBe('page');
  });

  it('names the current page in the top bar', async () => {
    signInAs('Employee', [Permissions.EmployeesView]);
    const { harness, el } = await open('/employees');

    expect(words(el.querySelector('app-topbar header p'))).toBe('Employees');
    await harness.navigateByUrl('/account/password');
    await harness.fixture.whenStable();
    expect(words(el.querySelector('app-topbar header p'))).toBe('Change password');
  });

  it('account menu: opens from the keyboard, closes on Escape, signs out', async () => {
    signInAs('Employee', [Permissions.EmployeesView]);
    const { harness, el } = await open('/dashboard');
    const button = el.querySelector<HTMLButtonElement>('button[aria-haspopup=menu]')!;

    button.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown' }));
    await harness.fixture.whenStable();
    expect(button.getAttribute('aria-expanded')).toBe('true');
    const items = [...el.querySelectorAll('[role=menuitem]')].map(words);
    expect(items).toEqual(['My profile', 'Change password', 'Sign out']);

    el.querySelector('[role=menu]')!.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }),
    );
    await harness.fixture.whenStable();
    expect(el.querySelector('[role=menu]')).toBeNull();

    button.click();
    await harness.fixture.whenStable();
    [...el.querySelectorAll<HTMLButtonElement>('[role=menuitem]')].at(-1)!.click();
    http.expectOne('/api/auth/logout').flush(null, { status: 204, statusText: 'No Content' });
  });

  it('drawer on small screens: opens from the menu button, closes on navigation and Escape', async () => {
    signInAs('Employee', [Permissions.EmployeesView]);
    const { harness, el } = await open('/dashboard');
    const aside = el.querySelector('aside')!;
    expect(aside.classList).toContain('invisible'); // closed: hidden from tab order too

    el.querySelector<HTMLButtonElement>('button[aria-label="Open menu"]')!.click();
    await harness.fixture.whenStable();
    expect(aside.classList).not.toContain('invisible');

    await harness.navigateByUrl('/employees');
    await harness.fixture.whenStable();
    expect(aside.classList).toContain('invisible');

    el.querySelector<HTMLButtonElement>('button[aria-label="Open menu"]')!.click();
    await harness.fixture.whenStable();
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    await harness.fixture.whenStable();
    expect(aside.classList).toContain('invisible');
  });

  it.each([
    ['Linh Tran', 'LT'],
    ['Minh Van Nguyen', 'MN'],
    ['admin@hrm.local', 'A'],
    ['Huy', 'H'],
  ])('initials(%s) is %s', (name, expected) => {
    expect(initials(name)).toBe(expected);
  });
});
