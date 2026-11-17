import { Component, computed, input, signal } from '@angular/core';
import { hostWidth, niceMax } from './chart-size';

export interface TrendPoint {
  key: string;
  /** Short x label, e.g. "Nov 17". */
  label: string;
  value: number;
  /** Extra lines for the tooltip and the table, e.g. "5 of 10 people in". */
  detail?: string[];
}

const M = { top: 14, right: 48, bottom: 26, left: 40 };

/**
 * One series over time: a 2px line over a 10% area wash, an end dot with its value, and a crosshair
 * that snaps to the nearest point and shows its tooltip (pointer and arrow keys). A table view
 * below keeps every value reachable without hovering.
 */
@Component({
  selector: 'app-trend-chart',
  host: { class: 'block' },
  template: `
    <div class="relative">
      <svg
        width="100%"
        [attr.height]="height()"
        class="block overflow-visible outline-none focus-visible:outline-2 focus-visible:outline-brand-500"
        role="img"
        tabindex="0"
        [attr.aria-label]="ariaLabel()"
        (pointermove)="track($event)"
        (pointerleave)="active.set(null)"
        (keydown)="key($event)"
        (blur)="active.set(null)"
      >
        @for (t of ticks(); track t.value) {
          <line
            [attr.x1]="m.left"
            [attr.x2]="width() - m.right"
            [attr.y1]="t.y"
            [attr.y2]="t.y"
            class="stroke-border"
            stroke-width="1"
          />
          <text
            [attr.x]="m.left - 8"
            [attr.y]="t.y + 4"
            text-anchor="end"
            class="fill-muted text-[11px] tabular-nums"
          >
            {{ t.label }}
          </text>
        }
        @for (x of xLabels(); track x.i) {
          <text
            [attr.x]="x.x"
            [attr.y]="height() - 6"
            [attr.text-anchor]="x.anchor"
            class="fill-muted text-[11px]"
          >
            {{ x.label }}
          </text>
        }

        @if (points().length > 1) {
          <path [attr.d]="area()" class="chart-fade fill-brand-600/10" />
          <path
            [attr.d]="line()"
            pathLength="1"
            class="chart-draw fill-none stroke-brand-600"
            stroke-width="2"
            stroke-linecap="round"
            stroke-linejoin="round"
          />
        }
        @if (last(); as l) {
          <circle [attr.cx]="l.x" [attr.cy]="l.y" r="6" class="fill-surface" />
          <circle [attr.cx]="l.x" [attr.cy]="l.y" r="4" class="fill-brand-600" />
          <text
            [attr.x]="l.x + 10"
            [attr.y]="l.y + 4"
            class="fill-ink text-xs font-semibold tabular-nums"
          >
            {{ format()(l.value) }}
          </text>
        }

        @if (activePoint(); as a) {
          <line
            [attr.x1]="a.x"
            [attr.x2]="a.x"
            [attr.y1]="m.top"
            [attr.y2]="height() - m.bottom"
            class="stroke-muted"
            stroke-width="1"
          />
          <circle [attr.cx]="a.x" [attr.cy]="a.y" r="6" class="fill-surface" />
          <circle [attr.cx]="a.x" [attr.cy]="a.y" r="4" class="fill-brand-600" />
        }
      </svg>

      @if (activePoint(); as a) {
        <div
          class="pointer-events-none absolute top-0 z-10 min-w-36 rounded-md border border-border bg-surface px-3 py-2 text-xs shadow-md"
          [style.left.px]="tooltipLeft()"
          role="status"
        >
          <p class="text-muted">{{ a.label }}</p>
          <p class="flex items-center gap-2 text-sm font-semibold text-ink">
            <span class="inline-block h-0.5 w-3 rounded bg-brand-600" aria-hidden="true"></span>
            {{ format()(a.value) }} <span class="font-normal text-muted">{{ valueLabel() }}</span>
          </p>
          @for (line of a.detail ?? []; track $index) {
            <p class="text-muted">{{ line }}</p>
          }
        </div>
      }
    </div>

    <details class="mt-2 text-xs">
      <summary class="cursor-pointer text-muted select-none hover:text-ink">View as table</summary>
      <div class="mt-2 max-h-56 overflow-auto">
        <table class="w-full text-left">
          <thead>
            <tr class="text-muted">
              <th class="py-1 font-medium">Date</th>
              <th class="py-1 text-right font-medium">{{ valueLabel() }}</th>
              <th class="py-1 pl-4 font-medium">Details</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-border">
            @for (p of points(); track p.key) {
              <tr>
                <td class="py-1">{{ p.label }}</td>
                <td class="py-1 text-right tabular-nums">{{ format()(p.value) }}</td>
                <td class="py-1 pl-4 text-muted">{{ (p.detail ?? []).join(' · ') }}</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </details>
  `,
})
export class TrendChart {
  readonly points = input.required<TrendPoint[]>();
  readonly height = input(200);
  /** Fixed y maximum (e.g. 100 for a percentage); otherwise a round number above the data. */
  readonly max = input<number | null>(null);
  readonly format = input<(v: number) => string>((v) => String(Math.round(v)));
  /** What the value is, for the tooltip and table header, e.g. "attendance". */
  readonly valueLabel = input('');
  readonly ariaLabel = input.required<string>();

  protected readonly m = M;
  protected readonly width = hostWidth();
  protected readonly active = signal<number | null>(null);

  private readonly yMax = computed(
    () => this.max() ?? niceMax(Math.max(0, ...this.points().map((p) => p.value))),
  );
  private readonly innerW = computed(() => Math.max(1, this.width() - M.left - M.right));
  private readonly innerH = computed(() => Math.max(1, this.height() - M.top - M.bottom));

  private readonly coords = computed(() => {
    const pts = this.points();
    const step = pts.length > 1 ? this.innerW() / (pts.length - 1) : 0;
    return pts.map((p, i) => ({
      ...p,
      x: M.left + (pts.length > 1 ? i * step : this.innerW() / 2),
      y: M.top + this.innerH() - (Math.min(p.value, this.yMax()) / this.yMax()) * this.innerH(),
    }));
  });

  protected readonly line = computed(() =>
    this.coords()
      .map((c, i) => `${i ? 'L' : 'M'}${c.x.toFixed(1)},${c.y.toFixed(1)}`)
      .join(''),
  );

  protected readonly area = computed(() => {
    const c = this.coords();
    const base = M.top + this.innerH();
    return `${this.line()}L${c[c.length - 1].x.toFixed(1)},${base}L${c[0].x.toFixed(1)},${base}Z`;
  });

  protected readonly last = computed(() => this.coords().at(-1) ?? null);

  protected readonly ticks = computed(() =>
    [0, 0.5, 1].map((f) => ({
      value: f,
      y: M.top + this.innerH() - f * this.innerH(),
      label: this.format()(f * this.yMax()),
    })),
  );

  /** First, middle and last date: enough to read the axis without crowding it. */
  protected readonly xLabels = computed(() => {
    const c = this.coords();
    if (!c.length) return [];
    const picks = [...new Set([0, Math.floor((c.length - 1) / 2), c.length - 1])];
    return picks.map((i) => ({
      i,
      x: c[i].x,
      label: c[i].label,
      anchor: i === 0 ? 'start' : i === c.length - 1 ? 'end' : 'middle',
    }));
  });

  protected readonly activePoint = computed(() => {
    const i = this.active();
    return i === null ? null : (this.coords()[i] ?? null);
  });

  protected readonly tooltipLeft = computed(() => {
    const a = this.activePoint();
    if (!a) return 0;
    // Beside the crosshair, flipped to the left in the right half so it never leaves the card.
    return a.x > this.width() / 2 ? Math.max(0, a.x - 160) : a.x + 12;
  });

  protected track(event: PointerEvent): void {
    const n = this.points().length;
    if (!n) return;
    const svg = event.currentTarget as SVGElement;
    const x = event.clientX - svg.getBoundingClientRect().left;
    const step = n > 1 ? this.innerW() / (n - 1) : 1;
    this.active.set(Math.min(n - 1, Math.max(0, Math.round((x - M.left) / step))));
  }

  protected key(event: KeyboardEvent): void {
    const n = this.points().length;
    if (!n) return;
    const current = this.active() ?? n - 1;
    const next =
      event.key === 'ArrowLeft'
        ? current - 1
        : event.key === 'ArrowRight'
          ? current + 1
          : event.key === 'Home'
            ? 0
            : event.key === 'End'
              ? n - 1
              : null;
    if (next === null) return;
    event.preventDefault();
    this.active.set(Math.min(n - 1, Math.max(0, next)));
  }
}
