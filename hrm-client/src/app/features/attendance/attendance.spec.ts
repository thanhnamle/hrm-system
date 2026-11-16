import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideTestApp, signInAs } from '../../../testing/test-helpers';
import { Permissions } from '../../core/auth/permissions';
import { MyAttendance } from './my-attendance/my-attendance';
import { TeamAttendance } from './team-attendance/team-attendance';

const page = (items: unknown[]) => ({
  items,
  page: 1,
  pageSize: 50,
  totalCount: items.length,
  totalPages: 1,
});

describe('Attendance feature', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideTestApp() });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('checks in, then offers check-out', async () => {
    signInAs('Employee', [Permissions.AttendanceRecord]);
    const fixture = TestBed.createComponent(MyAttendance);
    TestBed.tick();
    http.expectOne('/api/attendance/today').flush({ today: '2030-01-07', record: null });
    http.expectOne((r) => r.url === '/api/attendance/mine').flush(page([]));
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Not checked in yet');

    [...el.querySelectorAll('button')].find((b) => b.textContent?.trim() === 'Check in')!.click();
    const record = {
      id: 'a1',
      workDate: '2030-01-07',
      checkInAt: '2030-01-07T01:58:00Z',
      checkOutAt: null,
      workedHours: null,
    };
    http.expectOne('/api/attendance/check-in').flush(record);
    TestBed.tick();
    http.expectOne('/api/attendance/today').flush({ today: '2030-01-07', record });
    http.expectOne((r) => r.url === '/api/attendance/mine').flush(page([record]));
    await fixture.whenStable();

    expect(el.textContent).toContain('Checked in at');
    expect(
      [...el.querySelectorAll('button')].some((b) => b.textContent?.trim() === 'Check out'),
    ).toBe(true);
  });

  it('summarises the team and shows the department filter only to HR/Admin', async () => {
    signInAs('Manager', [Permissions.AttendanceView]);
    const fixture = TestBed.createComponent(TeamAttendance);
    TestBed.tick();
    const row = (name: string, status: string) => ({
      employeeId: name,
      employeeCode: 'EMP',
      employeeName: name,
      departmentName: 'Engineering',
      checkInAt: null,
      checkOutAt: null,
      onLeave: status === 'OnLeave',
      status,
    });
    http
      .expectOne((r) => r.url === '/api/attendance')
      .flush(
        page([row('A', 'Present'), row('B', 'Present'), row('C', 'OnLeave'), row('D', 'Absent')]),
      );
    http.expectOne('/api/departments').flush([]);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const cards = [...el.querySelectorAll('.card')].map((c) =>
      [...c.querySelectorAll('p')].map((p) => p.textContent?.trim()).join(' '),
    );

    expect(cards).toEqual(['At work 2', 'Checked out 0', 'On leave 1', 'Not checked in 1']);
    expect(el.querySelector('#department')).toBeNull();
  });
});
