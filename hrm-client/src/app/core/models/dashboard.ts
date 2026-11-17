import { Role } from '../auth/permissions';
import { IsoDate, IsoDateTime } from './common';
import { JobChangeType } from './employee-profile';
import { LeaveRequest, LeaveType } from './leave';

/** GET /api/dashboard. Sections the caller's role doesn't get are null. */
export interface DashboardSummary {
  role: Role;
  today: IsoDate;
  isWorkingDay: boolean;
  organization: OrganizationSummary | null; // Admin, HR
  team: TeamSummary | null; // Manager
  personal: PersonalSummary | null; // anyone with an employee record
  dataQuality: DataQualitySummary | null; // Admin, HR
  system: SystemStatus | null; // Admin, HR
  /** Set by the client when the response lacked data this page expects (an older API). */
  incomplete?: boolean;
}

/** Active employees by today's state; the four numbers add up to the headcount. */
export interface AttendanceBreakdown {
  checkedOut: number;
  working: number;
  onLeave: number;
  notCheckedIn: number;
}

/** Approved leave days starting this year; every type is listed, 0 when unused. */
export interface LeaveTypeDays {
  type: LeaveType;
  days: number;
}

export interface ActivityItem {
  employeeId: string;
  employeeName: string;
  changeType: JobChangeType;
  effectiveDate: IsoDate;
  position: string;
  departmentName: string;
  note: string | null;
}

export interface OrganizationSummary {
  headcount: number;
  onLeaveToday: number;
  pendingApprovals: number;
  headcountByDepartment: {
    departmentId: string;
    departmentName: string;
    headcount: number;
    managerName: string | null;
  }[];
  newHiresThisMonth: number;
  leaversThisYear: number;
  attendanceToday: AttendanceBreakdown;
  leaveDaysThisYear: LeaveTypeDays[];
  pendingRequests: LeaveRequest[]; // oldest first
  upcomingLeave: LeaveRequest[]; // approved, today and the next 14 days
  recentActivity: ActivityItem[];
  attendanceTrend: DailyAttendance[]; // working days of the last 30 days, oldest first
  headcountTrend: MonthHeadcount[]; // the last 12 months, oldest first
  leaveByMonth: MonthLeave[]; // approved leave days, 6 months back to 2 ahead
}

/** `expected` = people employed that day, so the attendance rate is present / expected. */
export interface DailyAttendance {
  date: IsoDate;
  present: number;
  onLeave: number;
  expected: number;
}

/** `month` is the first day of the month; `headcount` is at the month's end (today for this month). */
export interface MonthHeadcount {
  month: IsoDate;
  headcount: number;
  hires: number;
  leavers: number;
}

/** Approved leave days by the month the leave starts in. */
export interface MonthLeave {
  month: IsoDate;
  annual: number;
  sick: number;
  unpaid: number;
}

/** Monday-start week; hours count completed days only. */
export interface WeekHours {
  weekStart: IsoDate;
  hours: number;
  daysPresent: number;
}

export type TodayStatus = 'NotCheckedIn' | 'Working' | 'CheckedOut' | 'OnLeave';

export interface TeamMemberToday {
  employeeId: string;
  name: string;
  position: string;
  status: TodayStatus;
  checkInAt: IsoDateTime | null;
  checkOutAt: IsoDateTime | null;
}

export interface TeamSummary {
  teamSize: number;
  presentToday: number;
  onLeaveToday: number;
  pendingApprovals: number;
  departmentName: string;
  attendanceToday: AttendanceBreakdown;
  members: TeamMemberToday[];
  pendingRequests: LeaveRequest[];
  upcomingLeave: LeaveRequest[];
  attendanceTrend: DailyAttendance[];
}

export interface PersonalSummary {
  annualLeaveBalance: number;
  pendingAnnualDays: number;
  pendingRequests: number;
  checkedInAt: IsoDateTime | null;
  checkedOutAt: IsoDateTime | null;
  recentRequests: LeaveRequest[];
  profile: {
    employeeCode: string;
    position: string;
    departmentName: string;
    hireDate: IsoDate;
    skillsCount: number;
    qualificationsCount: number;
    hasCv: boolean;
    lastChange: ActivityItem | null;
  };
  leaveDaysThisYear: LeaveTypeDays[];
  nextLeave: LeaveRequest | null;
  attendanceThisMonth: { daysPresent: number; workingDaysSoFar: number; hoursWorked: number };
  hoursByWeek: WeekHours[]; // the last 8 weeks, oldest first
}

export interface DataQualitySummary {
  activeEmployees: number;
  withoutLogin: number;
  withoutCv: number;
  withoutSkills: number;
  withoutQualifications: number;
  lockedAccounts: number;
  accountsByRole: { role: Role; count: number }[];
  needsAttention: { employeeId: string; employeeName: string; missing: string[] }[];
}

export type HealthStatus = 'Healthy' | 'Degraded' | 'Unhealthy';

export interface SystemStatus {
  status: HealthStatus;
  components: { name: string; status: HealthStatus; durationMs: number }[];
  version: string;
  startedAt: IsoDateTime;
  checkedAt: IsoDateTime;
}
