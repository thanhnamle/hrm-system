import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AttendanceBreakdown } from '../../../core/models/dashboard';
import { LeaveRequest } from '../../../core/models/leave';
import { Badge } from '../../../shared/ui/badge/badge';
import { Sparkline } from '../charts/sparkline';
import { CountUp } from './count-up';

/**
 * Simple 24×24 line icons, drawn with the text colour. Path data only (circles and rectangles
 * written as paths): rendered as real <path> elements, because Angular's sanitizer would strip
 * SVG shapes inserted through innerHTML.
 */
const ICONS = {
  users: [
    'M5.5 8a3.5 3.5 0 1 0 7 0a3.5 3.5 0 1 0 -7 0',
    'M2.5 20c0-3.6 2.9-6.5 6.5-6.5s6.5 2.9 6.5 6.5',
    'M14.5 9a2.5 2.5 0 1 0 5 0a2.5 2.5 0 1 0 -5 0',
    'M17 14c2.8 0 4.5 2 4.5 5',
  ],
  clock: ['M3 12a9 9 0 1 0 18 0a9 9 0 1 0 -18 0', 'M12 7v5l3 2'],
  calendar: [
    'M5.5 5h13a2 2 0 0 1 2 2v11a2 2 0 0 1 -2 2h-13a2 2 0 0 1 -2 -2v-11a2 2 0 0 1 2 -2z',
    'M3.5 10h17M8 3v4M16 3v4',
  ],
  inbox: ['M4 13 6 5h12l2 8v6H4z', 'M4 13h5l1 2h4l1-2h5'],
  shield: ['M12 3l8 3v6c0 4.5-3.4 8-8 9-4.6-1-8-4.5-8-9V6z', 'm9 12 2 2 4-4'],
  pulse: ['M3 12h4l2-5 4 10 2-5h6'],
  building: [
    'M6 3h12a1 1 0 0 1 1 1v16a1 1 0 0 1 -1 1h-12a1 1 0 0 1 -1 -1v-16a1 1 0 0 1 1 -1z',
    'M9 7h2M13 7h2M9 11h2M13 11h2M9 15h2M13 15h2',
  ],
  file: ['M7 3h7l5 5v13H7z', 'M14 3v5h5'],
  user: ['M8 8a4 4 0 1 0 8 0a4 4 0 1 0 -8 0', 'M4.5 21c0-4.1 3.4-7.5 7.5-7.5s7.5 3.4 7.5 7.5'],
} as const;

export type IconName = keyof typeof ICONS;

@Component({
  selector: 'app-icon',
  template: `<svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    stroke-width="1.75"
    stroke-linecap="round"
    stroke-linejoin="round"
    class="size-5"
    aria-hidden="true"
  >
    @for (d of paths(); track $index) {
      <path [attr.d]="d" />
    }
  </svg>`,
})
export class Icon {
  readonly name = input.required<IconName>();
  protected readonly paths = computed(() => ICONS[this.name()]);
}

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
