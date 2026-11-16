import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { polyfillDialog, provideTestApp, signInAs, typeInto } from '../../../testing/test-helpers';
import { Permissions } from '../../core/auth/permissions';
import { LeaveRequest } from '../../core/models/leave';
import { workingDays } from '../../shared/forms/dates';
import { Approvals } from './approvals/approvals';
import { MyRequests } from './my-requests/my-requests';

const page = (items: unknown[]) => ({
  items,
  page: 1,
  pageSize: 20,
  totalCount: items.length,
  totalPages: 1,
});
const request: LeaveRequest = {
  id: 'r1',
  employeeId: 'e1',
  employeeName: 'Huy Le',
  departmentName: 'Engineering',
  type: 'Annual',
  startDate: '2030-01-07',
  endDate: '2030-01-08',
  days: 2,
  reason: 'Family trip',
  status: 'Pending',
  reviewedByName: null,
  reviewedAt: null,
  reviewNote: null,
  createdAt: '2029-12-01T00:00:00Z',
};
const buttons = (el: HTMLElement) => [...el.querySelectorAll('button')];
const button = (el: HTMLElement, text: string) =>
  buttons(el).find((b) => b.textContent?.trim() === text)!;

describe('workingDays', () => {
  it('counts Monday to Friday only', () => {
    expect(workingDays('2030-01-07', '2030-01-11')).toBe(5); // Mon–Fri
    expect(workingDays('2030-01-11', '2030-01-14')).toBe(2); // Fri–Mon
    expect(workingDays('2030-01-12', '2030-01-13')).toBe(0); // weekend
  });
});

describe('Leave feature', () => {
  let http: HttpTestingController;
  beforeAll(polyfillDialog);

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideTestApp() });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  describe('MyRequests', () => {
    async function render() {
      signInAs('Employee', [Permissions.LeaveRequest]);
      const fixture = TestBed.createComponent(MyRequests);
      TestBed.tick();
      http.expectOne((r) => r.url === '/api/leave-requests/mine').flush(page([request]));
      http
        .expectOne('/api/leave-requests/balance')
        .flush({ annualLeaveBalance: 12, pendingAnnualDays: 2, available: 10 });
      await fixture.whenStable();
      return { fixture, el: fixture.nativeElement as HTMLElement };
    }

    it('shows the balance and lets a pending request be cancelled', async () => {
      const { el } = await render();

      expect(el.textContent).toContain('Available annual leave');
      expect(el.textContent).toContain('10');
      expect(el.textContent).toContain('Family trip');
      expect(button(el, 'Cancel')).toBeTruthy();
    });

    it('previews working days and blocks annual leave beyond the available balance', async () => {
      const { fixture, el } = await render();
      button(el, 'Request leave').click();
      await fixture.whenStable();

      typeInto(el, '#startDate', '2030-01-07');
      typeInto(el, '#endDate', '2030-01-18'); // two weeks = 10 working days: fits
      await fixture.whenStable();
      expect(el.textContent).toContain('10 working day(s)');
      expect(button(el, 'Submit request').disabled).toBe(false);

      typeInto(el, '#endDate', '2030-01-21'); // 11 working days > 10 available
      await fixture.whenStable();
      expect(el.textContent).toContain('more than your 10 available');
      expect(button(el, 'Submit request').disabled).toBe(true);
    });

    it('submits the request and reloads the list and balance', async () => {
      const { fixture, el } = await render();
      button(el, 'Request leave').click();
      await fixture.whenStable();
      typeInto(el, '#type', 'Sick');
      typeInto(el, '#startDate', '2030-01-07');
      typeInto(el, '#endDate', '2030-01-07');
      typeInto(el, '#reason', 'Dentist');
      el.querySelector<HTMLFormElement>('#leave-form')!.dispatchEvent(new Event('submit'));

      const post = http.expectOne((r) => r.method === 'POST' && r.url === '/api/leave-requests');
      expect(post.request.body).toEqual({
        type: 'Sick',
        startDate: '2030-01-07',
        endDate: '2030-01-07',
        reason: 'Dentist',
      });
      post.flush({ id: 'r2' }, { status: 201, statusText: 'Created' });
      TestBed.tick();
      http.expectOne((r) => r.url === '/api/leave-requests/mine').flush(page([]));
      http
        .expectOne('/api/leave-requests/balance')
        .flush({ annualLeaveBalance: 12, pendingAnnualDays: 0, available: 12 });
    });
  });

  describe('Approvals', () => {
    async function render(role: 'Manager' | 'HR' | 'Admin', employeeId: string | null = 'm1') {
      const permissions =
        role === 'Manager'
          ? [Permissions.LeaveApprove]
          : [Permissions.LeaveApprove, Permissions.LeaveViewAll];
      signInAs(role, permissions, employeeId);
      const fixture = TestBed.createComponent(Approvals);
      TestBed.tick();
      http.expectOne((r) => r.url === '/api/leave-requests/pending').flush(page([request]));
      http.expectOne('/api/departments').flush([]);
      await fixture.whenStable();
      return { fixture, el: fixture.nativeElement as HTMLElement };
    }

    it('requires a reason before rejecting', async () => {
      const { fixture, el } = await render('Manager');

      button(el, 'Reject').click();
      await fixture.whenStable();
      buttons(el)
        .filter((b) => b.textContent?.trim() === 'Reject')
        .at(-1)!
        .click(); // the dialog's button
      await fixture.whenStable();

      http.expectNone((r) => r.url.endsWith('/reject'));
      expect(el.textContent).toContain('Reason for rejecting is required.');
    });

    it('approves with an optional note and reloads the inbox', async () => {
      const { fixture, el } = await render('Manager');

      button(el, 'Approve').click();
      await fixture.whenStable();
      buttons(el)
        .filter((b) => b.textContent?.trim() === 'Approve')
        .at(-1)!
        .click();

      const post = http.expectOne('/api/leave-requests/r1/approve');
      expect(post.request.body).toEqual({ note: null });
      post.flush(null, { status: 204, statusText: 'No Content' });
      TestBed.tick();
      http.expectOne((r) => r.url === '/api/leave-requests/pending').flush(page([]));
      await fixture.whenStable();
      expect(el.textContent).toContain('Nothing to review');
    });

    it('explains why an account without employee record cannot review', async () => {
      const { el } = await render('Admin', null);

      expect(el.textContent).toContain("isn't linked to an employee record");
      expect(buttons(el).some((b) => b.textContent?.trim() === 'Approve')).toBe(false);
    });

    it('shows the All requests tab only with Leave.ViewAll', async () => {
      const { fixture, el } = await render('HR');

      button(el, 'All requests').click();
      TestBed.tick();
      http
        .expectOne((r) => r.url === '/api/leave-requests')
        .flush(page([{ ...request, status: 'Approved' }]));
      await fixture.whenStable();

      expect(el.textContent).toContain('Approved');
    });
  });
});
