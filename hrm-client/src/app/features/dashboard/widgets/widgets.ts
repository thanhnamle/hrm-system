import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AttendanceBreakdown } from '../../../core/models/dashboard';
import { LeaveRequest } from '../../../core/models/leave';
import { Badge } from '../../../shared/ui/badge/badge';
import { Icon, IconName } from '../../../shared/ui/icon/icon';
import { Sparkline } from '../charts/sparkline';
import { CountUp } from './count-up';

/**
 * A number with a label, an icon and a line of context; optionally a sparkline of its recent
 * history (stat-tile contract: label · value · trend). The whole tile links to the detail page.
 */
@Component({
  selector: 'app-stat-tile',
  imports: [RouterLink, Icon, CountUp, Sparkline],
  template: `
    <a
      [routerLink]="link()"
      class="card lift flex h-full flex-col gap-3 p-5 hover:border-brand-500"
    >
      <span class="flex items-start gap-4">
        <span
          class="grid size-10 shrink-0 place-items-center rounded-lg bg-brand-50 text-brand-700"
        >
          <app-icon [name]="icon()" />
        </span>
        <span class="min-w-0">
          <span class="block text-sm text-muted">{{ label() }}</span>
          <span class="mt-0.5 block text-3xl font-semibold">
            @if (isNumber(value())) {
              <span [appCountUp]="$any(value())"></span>{{ suffix() }}
            } @else {
              {{ value() }}
            }
          </span>
          @if (hint()) {
            <span class="mt-1 block text-xs text-muted">{{ hint() }}</span>
          }
        </span>
      </span>
      @if (trend().length > 1) {
        <app-sparkline class="mt-auto" [values]="trend()" />
      }
    </a>
  `,
})
export class StatTile {
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly icon = input.required<IconName>();
  readonly link = input.required<string>();
  readonly hint = input<string>();
  /** Shown after a numeric value, e.g. "%". */
  readonly suffix = input('');
  /** Recent history, oldest first; the last value should be the one shown. */
  readonly trend = input<number[]>([]);
  protected readonly isNumber = (v: unknown): v is number => typeof v === 'number';
}

/**
 * Today's attendance as one segmented bar (a part-to-whole of the headcount) with a legend.
 * Colours are the app's status colours; every segment also has its name and count in the legend,
 * so the colour is never the only way to tell them apart.
 */
@Component({
  selector: 'app-attendance-bar',
  template: `
    <div
      class="flex h-3 gap-0.5 overflow-hidden rounded-full bg-canvas"
      role="img"
      [attr.aria-label]="summary()"
    >
      @for (s of segments(); track s.key) {
        @if (s.value > 0) {
          <span
            class="h-full first:rounded-l-full last:rounded-r-full"
            [class]="s.color"
            [style.flex-grow]="s.value"
            [title]="s.label + ': ' + s.value"
          ></span>
        }
      }
    </div>
    <!-- Wraps by the card's own width; each item stays on one line. -->
    <ul class="mt-3 flex flex-wrap gap-x-5 gap-y-2 text-sm">
      @for (s of segments(); track s.key) {
        <li class="flex items-center gap-1.5 whitespace-nowrap">
          <span
            class="inline-block size-2.5 shrink-0 rounded-sm"
            [class]="s.color"
            aria-hidden="true"
          ></span>
          <span class="text-muted">{{ s.label }}</span>
          <span class="font-semibold text-ink tabular-nums">{{ s.value }}</span>
        </li>
      }
    </ul>
  `,
})
export class AttendanceBar {
  readonly data = input.required<AttendanceBreakdown>();

  protected readonly segments = computed(() => {
    const d = this.data();
    return [
      { key: 'out', label: 'Checked out', value: d.checkedOut, color: 'bg-brand-600' },
      { key: 'in', label: 'Working', value: d.working, color: 'bg-success' },
      { key: 'leave', label: 'On leave', value: d.onLeave, color: 'bg-warning' },
      { key: 'none', label: 'Not checked in', value: d.notCheckedIn, color: 'bg-muted/40' },
    ];
  });

  protected readonly summary = computed(() =>
    this.segments()
      .map((s) => `${s.label} ${s.value}`)
      .join(', '),
  );
}

/** "8 of 10 have a CV" with a thin progress bar. */
@Component({
  selector: 'app-meter',
  template: `
    <div class="flex items-baseline justify-between gap-2 text-sm">
      <span class="text-ink">{{ label() }}</span>
      <span class="text-muted tabular-nums">{{ value() }} / {{ total() }}</span>
    </div>
    <div
      class="mt-1.5 h-1.5 rounded-full bg-canvas"
      role="meter"
      [attr.aria-label]="label()"
      [attr.aria-valuenow]="value()"
      aria-valuemin="0"
      [attr.aria-valuemax]="total()"
    >
      <span
        class="block h-full rounded-full"
        [class]="percent() === 100 ? 'bg-success' : 'bg-brand-600'"
        [style.width.%]="percent()"
      ></span>
    </div>
  `,
})
export class Meter {
  readonly label = input.required<string>();
  readonly value = input.required<number>();
  readonly total = input.required<number>();
  protected readonly percent = computed(() =>
    this.total() ? Math.round((this.value() / this.total()) * 100) : 0,
  );
}

export interface BarRow {
  key: string;
  label: string;
  value: number;
  caption?: string | null;
}

/** One series of horizontal bars scaled to the largest value; each bar shows its own number. */
@Component({
  selector: 'app-bar-list',
  imports: [DecimalPipe],
  template: `
    <ul class="space-y-3">
      @for (r of scaled(); track r.key) {
        <li class="grid grid-cols-[minmax(6rem,10rem)_1fr_3rem] items-center gap-3 text-sm">
          <span class="min-w-0">
            <span class="block truncate text-ink" [title]="r.label">{{ r.label }}</span>
            @if (r.caption) {
              <span class="block truncate text-xs text-muted" [title]="r.caption">{{
                r.caption
              }}</span>
            }
          </span>
          <span class="h-2 rounded-full bg-canvas" aria-hidden="true">
            <span
              class="block h-2 rounded-full bg-brand-600"
              [style.width.%]="r.percent"
              [title]="r.label + ': ' + (r.value | number: '1.0-1') + ' ' + unit()"
            ></span>
          </span>
          <span class="text-right font-medium text-ink tabular-nums">{{
            r.value | number: '1.0-1'
          }}</span>
        </li>
      }
    </ul>
  `,
})
export class BarList {
  readonly rows = input.required<BarRow[]>();
  /** Used in the hover text, e.g. "employees" or "days". */
  readonly unit = input('');

  protected readonly scaled = computed(() => {
    const rows = this.rows();
    const max = Math.max(1, ...rows.map((r) => r.value));
    return rows.map((r) => ({ ...r, percent: (r.value / max) * 100 }));
  });
}

/** Leave requests as a compact list; `pending` adds how long each has been waiting. */
@Component({
  selector: 'app-leave-list',
  imports: [DatePipe, DecimalPipe, RouterLink, Badge],
  template: `
    @if (requests().length) {
      <ul class="divide-y divide-border">
        @for (r of requests(); track r.id) {
          <li class="flex items-center justify-between gap-3 py-2.5 text-sm">
            <span class="min-w-0">
              <a
                class="block truncate font-medium text-ink hover:text-brand-700"
                [routerLink]="['/employees', r.employeeId]"
                >{{ r.employeeName }}</a
              >
              <span class="block truncate text-xs text-muted">
                {{ r.type }} · {{ r.startDate | date: 'MMM d' }}
                @if (r.endDate !== r.startDate) {
                  – {{ r.endDate | date: 'MMM d' }}
                }
                · {{ r.days | number: '1.0-1' }} day(s) · {{ r.departmentName }}
              </span>
            </span>
            @if (mode() === 'pending') {
              <span class="shrink-0 text-xs text-muted tabular-nums">{{ waiting(r) }}</span>
            } @else {
              <app-badge [tone]="isOngoing(r) ? 'warning' : 'neutral'">{{
                isOngoing(r) ? 'Away now' : 'Upcoming'
              }}</app-badge>
            }
          </li>
        }
      </ul>
    } @else {
      <p class="py-4 text-sm text-muted">{{ emptyText() }}</p>
    }
  `,
})
export class LeaveList {
  readonly requests = input.required<LeaveRequest[]>();
  readonly mode = input<'pending' | 'upcoming'>('upcoming');
  readonly today = input.required<string>();
  readonly emptyText = input('Nothing here.');

  protected isOngoing(r: LeaveRequest): boolean {
    return r.startDate <= this.today() && r.endDate >= this.today();
  }

  /** "today", "1 day", "5 days": since the request was submitted. */
  protected waiting(r: LeaveRequest): string {
    const days = Math.floor((Date.now() - new Date(r.createdAt).getTime()) / 86_400_000);
    return days <= 0 ? 'today' : days === 1 ? '1 day' : `${days} days`;
  }
}
