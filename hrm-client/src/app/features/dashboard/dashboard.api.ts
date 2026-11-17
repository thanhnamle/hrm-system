import { httpResource } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { environment } from '../../../environments/environment';
import { DashboardSummary } from '../../core/models/dashboard';

type Loose<T> = { [K in keyof T]?: T[K] | null };

/**
 * Makes a partial response safe to render instead of crashing the page. That happens when the
 * client is newer than the API it talks to, e.g. during development before the API is restarted.
 * - A missing list becomes [] (an empty chart is honest: there is no data to show).
 * - A section missing an object it can't do without (today's attendance, the profile) is dropped,
 *   rather than filled with made-up zeros, and `incomplete` is set so the page can say why.
 */
export function normalizeDashboard(raw: unknown): DashboardSummary {
  const s = (raw ?? {}) as Loose<DashboardSummary>;
  const list = <T>(value: T[] | null | undefined): T[] => value ?? [];

  const dropped = (section: object | null | undefined, required: unknown[]) =>
    !!section && required.some((r) => r == null);
  const orgOk = !dropped(s.organization, [s.organization?.attendanceToday]);
  const teamOk = !dropped(s.team, [s.team?.attendanceToday]);
  const meOk = !dropped(s.personal, [s.personal?.profile, s.personal?.attendanceThisMonth]);

  const org = orgOk ? s.organization : null;
  const team = teamOk ? s.team : null;
  const me = meOk ? s.personal : null;
  const quality = s.dataQuality;

  return {
    role: s.role ?? 'Employee',
    today: s.today ?? new Date().toISOString().slice(0, 10),
    isWorkingDay: s.isWorkingDay ?? true,
    organization: org
      ? {
          ...org,
          headcountByDepartment: list(org.headcountByDepartment),
          leaveDaysThisYear: list(org.leaveDaysThisYear),
          pendingRequests: list(org.pendingRequests),
          upcomingLeave: list(org.upcomingLeave),
          recentActivity: list(org.recentActivity),
          attendanceTrend: list(org.attendanceTrend),
          headcountTrend: list(org.headcountTrend),
          leaveByMonth: list(org.leaveByMonth),
        }
      : null,
    team: team
      ? {
          ...team,
          members: list(team.members),
          pendingRequests: list(team.pendingRequests),
          upcomingLeave: list(team.upcomingLeave),
          attendanceTrend: list(team.attendanceTrend),
        }
      : null,
    personal: me
      ? {
          ...me,
          recentRequests: list(me.recentRequests),
          leaveDaysThisYear: list(me.leaveDaysThisYear),
          hoursByWeek: list(me.hoursByWeek),
        }
      : null,
    dataQuality: quality
      ? {
          ...quality,
          accountsByRole: list(quality.accountsByRole),
          needsAttention: list(quality.needsAttention),
        }
      : null,
    system: s.system ? { ...s.system, components: list(s.system.components) } : null,
    incomplete:
      !orgOk ||
      !teamOk ||
      !meOk ||
      (!!org && !org.attendanceTrend) ||
      (!!team && !team.attendanceTrend) ||
      (!!me && !me.hoursByWeek),
  } as DashboardSummary;
}

/** One call for the home page; the API fills only the sections the caller's role gets. */
@Injectable({ providedIn: 'root' })
export class DashboardApi {
  summary() {
    return httpResource(() => `${environment.apiBaseUrl}/dashboard`, { parse: normalizeDashboard });
  }
}
