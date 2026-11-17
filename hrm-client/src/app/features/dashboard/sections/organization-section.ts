import { DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrganizationSummary } from '../../../core/models/dashboard';
import { Badge, BadgeTone, humanize } from '../../../shared/ui/badge/badge';
import { AttendanceBar, BarList, BarRow, LeaveList, StatTile } from '../widgets/widgets';

const CHANGE_TONES: Record<string, BadgeTone> = {
  Hired: 'success',
  PositionChanged: 'brand',
  Transferred: 'brand',
  Terminated: 'neutral',
};

/** Admin and HR: the whole company at a glance. */
@Component({
  selector: 'app-organization-section',
  host: { class: 'block' }, // custom elements are inline; block so the page spacing applies
  imports: [DatePipe, RouterLink, Badge, StatTile, AttendanceBar, BarList, LeaveList],
  templateUrl: './organization-section.html',
})
export class OrganizationSection {
  readonly org = input.required<OrganizationSummary>();
  readonly today = input.required<string>();
  readonly isWorkingDay = input.required<boolean>();

  /** "+2 hired this month · 1 left this year", or "No new hires this month · …" */
  protected readonly headcountHint = computed(() => {
    const o = this.org();
    const hires = o.newHiresThisMonth
      ? `+${o.newHiresThisMonth} hired this month`
      : 'No new hires this month';
    return `${hires} · ${o.leaversThisYear} left this year`;
  });

  protected readonly present = computed(
    () => this.org().attendanceToday.working + this.org().attendanceToday.checkedOut,
  );

  protected readonly presentPercent = computed(() =>
    this.org().headcount ? Math.round((this.present() / this.org().headcount) * 100) : 0,
  );

  protected readonly departments = computed<BarRow[]>(() =>
    this.org().headcountByDepartment.map((d) => ({
      key: d.departmentId,
      label: d.departmentName,
      value: d.headcount,
      caption: d.managerName ? `Manager: ${d.managerName}` : 'No manager assigned',
    })),
  );

  protected readonly leaveDays = computed<BarRow[]>(() =>
    this.org().leaveDaysThisYear.map((l) => ({ key: l.type, label: l.type, value: l.days })),
  );

  protected readonly totalLeaveDays = computed(() =>
    this.org().leaveDaysThisYear.reduce((sum, l) => sum + l.days, 0),
  );

  protected readonly changeTone = (type: string) => CHANGE_TONES[type] ?? 'neutral';
  protected readonly humanize = humanize;
}
