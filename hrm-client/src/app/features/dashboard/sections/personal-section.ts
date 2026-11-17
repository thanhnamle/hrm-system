import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PersonalSummary } from '../../../core/models/dashboard';
import { Badge, humanize } from '../../../shared/ui/badge/badge';
import { Column, ColumnChart } from '../charts/column-chart';
import { dayLabel, hoursInsight } from '../story';
import { BarList, BarRow, Icon, Meter } from '../widgets/widgets';
import { BRAND_SERIES } from './organization-section';

/** "2 years 3 months", "5 months", "Joined this month". */
export function tenure(hireDate: string, today: string): string {
  const [hy, hm, hd] = hireDate.split('-').map(Number);
  const [ty, tm, td] = today.split('-').map(Number);
  let months = (ty - hy) * 12 + (tm - hm) - (td < hd ? 1 : 0);
  if (months <= 0) return 'Joined this month';
  const years = Math.floor(months / 12);
  months %= 12;
  const parts = [];
  if (years) parts.push(`${years} year${years > 1 ? 's' : ''}`);
  if (months) parts.push(`${months} month${months > 1 ? 's' : ''}`);
  return parts.join(' ');
}

/** Anyone with an employee record: their own profile, leave and attendance. */
@Component({
  selector: 'app-personal-section',
  host: { class: 'block' }, // custom elements are inline; block so the page spacing applies
  imports: [DatePipe, DecimalPipe, RouterLink, Badge, BarList, Icon, Meter, ColumnChart],
  templateUrl: './personal-section.html',
})
export class PersonalSection {
  readonly me = input.required<PersonalSummary>();
  readonly employeeId = input.required<string>();
  readonly today = input.required<string>();

  protected readonly tenure = computed(() => tenure(this.me().profile.hireDate, this.today()));

  protected readonly available = computed(
    () => this.me().annualLeaveBalance - this.me().pendingAnnualDays,
  );

  protected readonly leaveDays = computed<BarRow[]>(() =>
    this.me().leaveDaysThisYear.map((l) => ({ key: l.type, label: l.type, value: l.days })),
  );

  /** Profile items HR fills in; the dashboard shows which are still missing. */
  protected readonly checklist = computed(() => {
    const p = this.me().profile;
    return [
      { label: 'Skills', done: p.skillsCount > 0, detail: `${p.skillsCount} listed` },
      {
        label: 'Education & experience',
        done: p.qualificationsCount > 0,
        detail: `${p.qualificationsCount} entr${p.qualificationsCount === 1 ? 'y' : 'ies'}`,
      },
      { label: 'CV (PDF)', done: p.hasCv, detail: p.hasCv ? 'Uploaded' : 'Not uploaded' },
    ];
  });

  protected readonly completeness = computed(() => {
    const items = this.checklist();
    return Math.round((items.filter((i) => i.done).length / items.length) * 100);
  });

  protected readonly humanize = humanize;

  protected readonly hourSeries = BRAND_SERIES('hours', 'Hours');
  protected readonly hoursFormat = (v: number) => `${Math.round(v * 10) / 10} h`;
  protected readonly hours = computed(() => hoursInsight(this.me().hoursByWeek));

  protected readonly hourColumns = computed<Column[]>(() => {
    const weeks = this.me().hoursByWeek;
    return weeks.map((w, i) => ({
      key: w.weekStart,
      label: dayLabel(w.weekStart),
      values: { hours: w.hours },
      detail: [
        `${w.daysPresent} day(s) present`,
        ...(i === weeks.length - 1 ? ['This week, so far'] : []),
      ],
      highlight: i === weeks.length - 1,
    }));
  });
}
