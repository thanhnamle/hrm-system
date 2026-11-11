import { Role } from '../auth/permissions';
import { IsoDate, IsoDateTime } from './common';
import { LeaveRequest } from './leave';

/** GET /api/dashboard. Sections the caller's role doesn't get are null. */
export interface DashboardSummary {
  role: Role;
  today: IsoDate;
  organization: OrganizationSummary | null; // Admin, HR
  team: TeamSummary | null; // Manager
  personal: PersonalSummary | null; // anyone with an employee record
}

export interface OrganizationSummary {
  headcount: number;
  onLeaveToday: number;
  pendingApprovals: number;
  headcountByDepartment: { departmentId: string; departmentName: string; headcount: number }[];
}

export interface TeamSummary {
  teamSize: number;
  presentToday: number;
  onLeaveToday: number;
  pendingApprovals: number;
}

export interface PersonalSummary {
  annualLeaveBalance: number;
  pendingAnnualDays: number;
  pendingRequests: number;
  checkedInAt: IsoDateTime | null;
  checkedOutAt: IsoDateTime | null;
  recentRequests: LeaveRequest[];
}
