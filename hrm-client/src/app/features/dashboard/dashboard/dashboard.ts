import { DatePipe } from '@angular/common';
import { Component, computed, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { Permission, Permissions } from '../../../core/auth/permissions';
import { Badge } from '../../../shared/ui/badge/badge';
import { EmptyState } from '../../../shared/ui/states/states';
import { DashboardApi } from '../dashboard.api';
import { OrganizationSection } from '../sections/organization-section';
import { PersonalSection } from '../sections/personal-section';
import { HEALTH, ReliabilitySection } from '../sections/reliability-section';
import { TeamSection } from '../sections/team-section';
import { organizationStory, personalStory, teamStory } from '../story';
import { StoryBanner } from '../widgets/story-banner';

/** Reload the numbers while the page is open, so "present today" and the health check stay current. */
export const REFRESH_INTERVAL_MS = 60_000;

interface QuickAction {
  label: string;
  link: string;
  permission: Permission;
  needsEmployeeRecord?: boolean;
}

const QUICK_ACTIONS: QuickAction[] = [
  { label: 'Add employee', link: '/employees/new', permission: Permissions.EmployeesCreate },
  { label: 'Review leave', link: '/leave/approvals', permission: Permissions.LeaveApprove },
  {
    label: 'Request leave',
    link: '/leave',
    permission: Permissions.LeaveRequest,
    needsEmployeeRecord: true,
  },
  {
    label: 'Check in / out',
    link: '/attendance',
    permission: Permissions.AttendanceRecord,
    needsEmployeeRecord: true,
  },
  { label: 'Manage users', link: '/admin/users', permission: Permissions.UsersManage },
  { label: 'Departments', link: '/departments', permission: Permissions.DepartmentsManage },
];

/**
 * Home page. Admin/HR see the organization plus a reliability panel (system status and data
 * quality), a Manager their team, and anyone with an employee record their own profile, leave and
 * attendance. The API decides which sections are filled; this page only lays them out.
 */
@Component({
  imports: [
    DatePipe,
    RouterLink,
    Badge,
    EmptyState,
    StoryBanner,
    OrganizationSection,
    TeamSection,
    PersonalSection,
    ReliabilitySection,
  ],
  selector: 'app-dashboard',
  templateUrl: './dashboard.html',
})
export class Dashboard {
  protected readonly auth = inject(AuthService);
  protected readonly summary = inject(DashboardApi).summary();
  protected readonly health = HEALTH;

  /** When the last successful load finished, and how long it took (browser → API → database → back). */
  protected readonly lastUpdated = signal<Date | null>(null);
  protected readonly roundTripMs = signal<number | null>(null);
  private startedAt = performance.now();

  protected readonly greeting = computed(() => {
    const user = this.auth.user();
    const name = user?.fullName?.split(' ')[0] ?? (user?.role === 'Admin' ? 'Admin' : 'there');
    const hour = new Date().getHours();
    const part = hour < 12 ? 'Good morning' : hour < 18 ? 'Good afternoon' : 'Good evening';
    return `${part}, ${name}`;
  });

  /** The banner tells the widest story the user can see: organization, else team, else their own. */
  protected readonly story = computed(() => {
    if (!this.summary.hasValue()) return null;
    const s = this.summary.value();
    return (
      organizationStory(s) ??
      (s.team
        ? teamStory(s.team, s.today, s.isWorkingDay)
        : s.personal
          ? personalStory(s.personal, s.today, s.isWorkingDay)
          : null)
    );
  });

  protected readonly quickActions = computed(() => {
    const hasRecord = this.auth.hasEmployeeRecord();
    return QUICK_ACTIONS.filter(
      (a) => this.auth.hasPermission(a.permission) && (!a.needsEmployeeRecord || hasRecord),
    );
  });

  protected readonly canManageUsers = computed(() =>
    this.auth.hasPermission(Permissions.UsersManage),
  );

  constructor() {
    effect(() => {
      const status = this.summary.status();
      untracked(() => {
        if (status === 'loading' || status === 'reloading') {
          this.startedAt = performance.now();
        } else if (status === 'resolved') {
          this.roundTripMs.set(Math.round(performance.now() - this.startedAt));
          this.lastUpdated.set(new Date());
        }
      });
    });

    const timer = setInterval(() => this.summary.reload(), REFRESH_INTERVAL_MS);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }

  protected refresh(): void {
    this.summary.reload();
  }
}
