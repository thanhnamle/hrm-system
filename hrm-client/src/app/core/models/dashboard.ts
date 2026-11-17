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
