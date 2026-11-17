import { HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideTestApp, signInAs } from '../../../testing/test-helpers';
import { Permissions } from '../../core/auth/permissions';
import { DashboardSummary, PersonalSummary } from '../../core/models/dashboard';
import { LeaveRequest } from '../../core/models/leave';
import { Dashboard, REFRESH_INTERVAL_MS } from './dashboard/dashboard';
import { tenure } from './sections/personal-section';
import { shortVersion, uptime } from './sections/reliability-section';

/** Visible words, one space between separate elements (textContent would glue "Working" and "4"). */
const text = (el: Element): string => {
  const words: string[] = [];
  const walker = document.createTreeWalker(el, NodeFilter.SHOW_TEXT);
  for (let node = walker.nextNode(); node; node = walker.nextNode()) {
    const value = node.textContent?.trim();
    if (value) words.push(value);
  }
  return words.join(' ').replace(/\s+/g, ' ');
};
const tile = (el: HTMLElement, label: string) =>
  [...el.querySelectorAll('app-stat-tile')].map(text).find((t) => t.startsWith(label));

const request = (over: Partial<LeaveRequest> = {}): LeaveRequest => ({
  id: 'r1',
  employeeId: 'e1',
  employeeName: 'Huy Le',
  departmentName: 'Engineering',
  type: 'Annual',
  startDate: '2030-01-08',
  endDate: '2030-01-09',
  days: 2,
  reason: 'Trip',
  status: 'Pending',
  reviewedByName: null,
  reviewedAt: null,
  reviewNote: null,
  createdAt: new Date(Date.now() - 3 * 86_400_000).toISOString(),
  ...over,
});

const personal: PersonalSummary = {
  annualLeaveBalance: 12,
  pendingAnnualDays: 2,
  pendingRequests: 1,
  checkedInAt: null,
  checkedOutAt: null,
  recentRequests: [],
  profile: {
    employeeCode: 'EMP-0001',
    position: 'HR Manager',
    departmentName: 'Human Resources',
    hireDate: '2026-11-16',
    skillsCount: 3,
    qualificationsCount: 0,
    hasCv: true,
    lastChange: null,
  },
  leaveDaysThisYear: [
    { type: 'Annual', days: 4 },
    { type: 'Sick', days: 1 },
    { type: 'Unpaid', days: 0 },
  ],
  nextLeave: null,
  attendanceThisMonth: { daysPresent: 3, workingDaysSoFar: 5, hoursWorked: 24.5 },
};

const hrSummary: DashboardSummary = {
  role: 'HR',
  today: '2030-01-07',
  isWorkingDay: true,
  organization: {
    headcount: 10,
    onLeaveToday: 1,
    pendingApprovals: 2,
    headcountByDepartment: [
      {
        departmentId: 'eng',
        departmentName: 'Engineering',
        headcount: 5,
        managerName: 'Minh Nguyen',
      },
      { departmentId: 'sal', departmentName: 'Sales', headcount: 2, managerName: null },
    ],
    newHiresThisMonth: 1,
    leaversThisYear: 0,
    attendanceToday: { checkedOut: 2, working: 4, onLeave: 1, notCheckedIn: 3 },
    leaveDaysThisYear: [
      { type: 'Annual', days: 10 },
      { type: 'Sick', days: 5 },
      { type: 'Unpaid', days: 0 },
    ],
    pendingRequests: [request()],
    upcomingLeave: [request({ id: 'r2', status: 'Approved', startDate: '2030-01-07' })],
    recentActivity: [
      {
        employeeId: 'e9',
        employeeName: 'Tuan Phan',
        changeType: 'PositionChanged',
        effectiveDate: '2030-01-02',
        position: 'Senior Developer',
        departmentName: 'Engineering',
        note: 'Annual review',
      },
    ],
  },
  team: null,
  personal,
  dataQuality: {
    activeEmployees: 10,
    withoutLogin: 1,
    withoutCv: 4,
    withoutSkills: 2,
    withoutQualifications: 3,
    lockedAccounts: 0,
    accountsByRole: [
      { role: 'Admin', count: 1 },
      { role: 'HR', count: 2 },
      { role: 'Manager', count: 2 },
      { role: 'Employee', count: 6 },
    ],
    needsAttention: [{ employeeId: 'e5', employeeName: 'Khoa Dang', missing: ['CV', 'skills'] }],
  },
  system: {
    status: 'Healthy',
    components: [{ name: 'postgresql', status: 'Healthy', durationMs: 3.2 }],
    version: '1.0.0+3f2a9c1d2e4b',
    startedAt: '2030-01-07T01:00:00Z',
    checkedAt: '2030-01-07T03:15:00Z',
  },
};

describe('Dashboard', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideTestApp([{ path: '**', children: [] }]) });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  async function render(summary: DashboardSummary) {
    const fixture = TestBed.createComponent(Dashboard);
    TestBed.tick();
    http.expectOne('/api/dashboard').flush(summary);
    await fixture.whenStable();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('shows HR the organization, their own section and the reliability panel', async () => {
    signInAs('HR', [
      Permissions.EmployeesView,
      Permissions.EmployeesCreate,
      Permissions.LeaveApprove,
    ]);
    const { el } = await render(hrSummary);

    expect(tile(el, 'Active employees')).toBe(
      'Active employees 10 +1 hired this month · 0 left this year',
    );
    expect(tile(el, 'Present today')).toContain('6'); // 4 working + 2 checked out
    expect(tile(el, 'Present today')).toContain('60% of active employees');
    expect(text(el)).toContain("Who's away");
    expect(text(el)).toContain('3 days'); // the pending request has waited 3 days
    expect(text(el)).toContain('Position changed');
    expect(text(el)).toContain('Profile completeness 67%'); // skills and CV, no education
    expect(text(el)).toContain('All systems operational');
    expect(text(el)).toContain('Database (PostgreSQL)');
    expect(text(el)).toContain('1.0.0 (3f2a9c1)');
    expect(text(el)).toContain('Khoa Dang no CV no skills');
    expect(el.querySelector('app-team-section')).toBeNull();
  });

  it('scales department bars to the largest department and names the manager', async () => {
    signInAs('HR', [Permissions.EmployeesView]);
    const { el } = await render(hrSummary);

    const departments = [...el.querySelectorAll('app-bar-list')].find((b) =>
      text(b).includes('Engineering'),
    )!;
    const widths = [...departments.querySelectorAll<HTMLElement>('span.block.h-2')].map(
      (b) => b.style.width,
    );
    expect(widths).toEqual(['100%', '40%']);
    expect(text(departments)).toContain('Manager: Minh Nguyen');
    expect(text(departments)).toContain('No manager assigned');
  });

  it('labels every attendance segment, so colour is never the only cue', async () => {
    signInAs('HR', [Permissions.EmployeesView]);
    const { el } = await render(hrSummary);

    const legend = [...el.querySelectorAll('app-attendance-bar li')].map(text);
    expect(legend).toEqual(['Checked out 2', 'Working 4', 'On leave 1', 'Not checked in 3']);
  });

  it('offers only the quick actions the user is allowed to use', async () => {
    signInAs('HR', [Permissions.EmployeesCreate, Permissions.LeaveApprove]);
    const { el } = await render(hrSummary);

    const actions = [...el.querySelectorAll('nav[aria-label="Quick actions"] a')].map(text);
    expect(actions).toEqual(['Add employee', 'Review leave']);
  });

  it('shows a manager their team, with each member’s status today', async () => {
    signInAs('Manager', [Permissions.EmployeesView, Permissions.LeaveApprove]);
    const { el } = await render({
      ...hrSummary,
      role: 'Manager',
      organization: null,
      dataQuality: null,
      system: null,
      team: {
        teamSize: 2,
        presentToday: 1,
        onLeaveToday: 1,
        pendingApprovals: 0,
        departmentName: 'Engineering',
        attendanceToday: { checkedOut: 0, working: 1, onLeave: 1, notCheckedIn: 0 },
        members: [
          {
            employeeId: 'e1',
            name: 'Huy Le',
            position: 'Senior Developer',
            status: 'Working',
            checkInAt: '2030-01-07T01:30:00Z',
            checkOutAt: null,
          },
          {
            employeeId: 'e2',
            name: 'Thao Vo',
            position: 'Developer',
            status: 'OnLeave',
            checkInAt: null,
            checkOutAt: null,
          },
        ],
        pendingRequests: [],
        upcomingLeave: [],
      },
    });

    expect(text(el)).toContain('My team · Engineering');
    const members = [...el.querySelectorAll('app-team-section ul.mt-4 > li')].map(text);
    expect(members[0]).toMatch(/^Huy Le Senior Developer .+ Working$/);
    expect(members[1]).toBe('Thao Vo Developer On leave');
    expect(text(el)).toContain('No requests are waiting.');
    expect(el.querySelector('app-organization-section')).toBeNull();
    expect(el.querySelector('app-reliability-section')).toBeNull();
  });

  it('shows an employee only their own section', async () => {
    signInAs('Employee', [Permissions.LeaveRequest, Permissions.AttendanceRecord]);
    const { el } = await render({
      ...hrSummary,
      role: 'Employee',
      organization: null,
      dataQuality: null,
      system: null,
    });

    expect(text(el)).toContain('Not checked in');
    expect(text(el)).toContain('Days present this month 3 / 5');
    expect(text(el)).toContain('24.5 hours worked');
    expect(text(el)).not.toContain('Organization');
    expect(text(el)).not.toContain('Reliability');
    const actions = [...el.querySelectorAll('nav[aria-label="Quick actions"] a')].map(text);
    expect(actions).toEqual(['Request leave', 'Check in / out']);
  });

  it('reloads by itself every minute', async () => {
    // Only the interval: Angular's own scheduling uses setTimeout, which must stay real.
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval'] });
    try {
      signInAs('Employee', []);
      await render({ ...hrSummary, organization: null, dataQuality: null, system: null });

      vi.advanceTimersByTime(REFRESH_INTERVAL_MS);
      TestBed.tick();
      http.expectOne('/api/dashboard').flush({ ...hrSummary, organization: null });
    } finally {
      vi.useRealTimers();
    }
  });
});

describe('dashboard helpers', () => {
  it.each([
    ['2026-11-16', '2026-11-17', 'Joined this month'],
    ['2026-01-20', '2026-11-17', '9 months'],
    ['2023-11-17', '2026-11-17', '3 years'],
    ['2022-11-16', '2026-11-17', '4 years'],
    ['2021-08-01', '2026-11-17', '5 years 3 months'],
  ])('tenure(%s, %s) is %s', (hire, today, expected) => {
    expect(tenure(hire, today)).toBe(expected);
  });

  it('shortens the version to the build number and short commit', () => {
    expect(shortVersion('1.0.0+3f2a9c1d2e4b5a6')).toBe('1.0.0 (3f2a9c1)');
    expect(shortVersion('1.0.0')).toBe('1.0.0');
  });

  it('formats uptime', () => {
    const from = '2030-01-01T00:00:00Z';
    expect(uptime(from, new Date('2030-01-01T00:06:00Z'))).toBe('6 min');
    expect(uptime(from, new Date('2030-01-01T02:15:00Z'))).toBe('2 h 15 min');
    expect(uptime(from, new Date('2030-01-04T04:00:00Z'))).toBe('3 d 4 h');
  });
});
