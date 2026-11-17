import { DatePipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { OrganizationSummary } from '../../../core/models/dashboard';
import { Badge, BadgeTone, humanize } from '../../../shared/ui/badge/badge';
import { Column, ColumnChart, ColumnSeries } from '../charts/column-chart';
import { TrendChart, TrendPoint } from '../charts/trend-chart';
import {
  attendanceInsight,
  attendanceRate,
  dayLabel,
  headcountInsight,
  leaveInsight,
  monthLabel,
  pct,
} from '../story';
import { AttendanceBar, BarList, BarRow, LeaveList, StatTile } from '../widgets/widgets';

const CHANGE_TONES: Record<string, BadgeTone> = {
  Hired: 'success',
  PositionChanged: 'brand',
  Transferred: 'brand',
  Terminated: 'neutral',
};

/**
 * Leave types, in the validated categorical order (slots 1–3 of the reference palette: they pass
 * the colour-blindness checks as a set). Aqua is below 3:1 on white, so the legend and the table
 * view always carry the labels too.
 */
export const LEAVE_SERIES: ColumnSeries[] = [
  { key: 'annual', label: 'Annual', fill: 'fill-[#2a78d6]', swatch: 'bg-[#2a78d6]' },
  { key: 'sick', label: 'Sick', fill: 'fill-[#eb6834]', swatch: 'bg-[#eb6834]' },
  { key: 'unpaid', label: 'Unpaid', fill: 'fill-[#1baf7a]', swatch: 'bg-[#1baf7a]' },
];

export const BRAND_SERIES = (key: string, label: string): ColumnSeries[] => [
  { key, label, fill: 'fill-brand-600', swatch: 'bg-brand-600' },
];

/** Admin and HR: the whole company, as a story told by charts. */
@Component({
  selector: 'app-organization-section',
  host: { class: 'block' }, // custom elements are inline; block so the page spacing applies
  imports: [
    DatePipe,
    RouterLink,
    Badge,
    StatTile,
    AttendanceBar,
    BarList,
    LeaveList,
    TrendChart,
    ColumnChart,
  ],
  templateUrl: './organization-section.html',
})
export class OrganizationSection {
  readonly org = input.required<OrganizationSummary>();
  readonly today = input.required<string>();
  readonly isWorkingDay = input.required<boolean>();

  protected readonly leaveSeries = LEAVE_SERIES;
  protected readonly headcountSeries = BRAND_SERIES('headcount', 'Employed');
  protected readonly percent = (v: number) => pct(v);

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

  // ---- Charts ----

  protected readonly attendance = computed(() =>
    attendanceInsight(this.org().attendanceTrend, this.today()),
  );

  protected readonly attendancePoints = computed<TrendPoint[]>(() =>
    this.org().attendanceTrend.map((d) => ({
      key: d.date,
      label: d.date === this.today() ? `${dayLabel(d.date)} (today, so far)` : dayLabel(d.date),
      value: attendanceRate(d),
      detail: [
        `${d.present} of ${d.expected} people in`,
        ...(d.onLeave ? [`${d.onLeave} on leave`] : []),
      ],
    })),
  );

  /** The last 12 working days' rates, for the "Present today" tile. */
  protected readonly attendanceSpark = computed(() =>
    this.org().attendanceTrend.slice(-12).map(attendanceRate),
  );

  protected readonly headcount = computed(() => headcountInsight(this.org().headcountTrend));

  protected readonly headcountColumns = computed<Column[]>(() => {
    const trend = this.org().headcountTrend;
    return trend.map((m, i) => ({
      key: m.month,
      label: monthLabel(m.month),
      values: { headcount: m.headcount },
      detail: [`${m.hires} hired, ${m.leavers} left`],
      highlight: i === trend.length - 1,
    }));
  });

  protected readonly headcountSpark = computed(() =>
    this.org().headcountTrend.map((m) => m.headcount),
  );

  protected readonly leave = computed(() => leaveInsight(this.org().leaveByMonth, this.today()));

  protected readonly leaveColumns = computed<Column[]>(() => {
    const current = this.today().slice(0, 7);
    return this.org().leaveByMonth.map((m) => ({
      key: m.month,
      label: monthLabel(m.month),
      values: { annual: m.annual, sick: m.sick, unpaid: m.unpaid },
      upcoming: m.month.slice(0, 7) > current,
      highlight: m.month.slice(0, 7) === current,
    }));
  });

  protected readonly departments = computed<BarRow[]>(() =>
    this.org().headcountByDepartment.map((d) => ({
      key: d.departmentId,
      label: d.departmentName,
      value: d.headcount,
      caption: d.managerName ? `Manager: ${d.managerName}` : 'No manager assigned',
    })),
  );

  protected readonly largestDepartment = computed(() => {
    const rows = this.org().headcountByDepartment;
    if (!rows.length || !this.org().headcount) return null;
    const top = rows.reduce((a, b) => (b.headcount > a.headcount ? b : a));
    return `${top.departmentName} is the largest team: ${Math.round((top.headcount / this.org().headcount) * 100)}% of active employees`;
  });

  protected readonly changeTone = (type: string) => CHANGE_TONES[type] ?? 'neutral';
  protected readonly humanize = humanize;
}
