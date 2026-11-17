import { Component, computed, input, signal } from '@angular/core';
import { hostWidth, niceMax } from './chart-size';

export interface ColumnSeries {
  key: string;
  label: string;
  /** Full Tailwind class names (so the build finds them): SVG fill and legend swatch. */
  fill: string;
  swatch: string;
}

export interface Column {
  key: string;
  label: string;
  /** Value per series key; stacked bottom to top in series order. */
  values: Record<string, number>;
  /** Extra lines for the tooltip and the table. */
  detail?: string[];
  /** Planned rather than done (e.g. future months): drawn lighter, after the divider. */
  upcoming?: boolean;
  /** Print this column's total on its cap (besides the tallest, which is always labelled). */
  highlight?: boolean;
}

const M = { top: 20, right: 8, bottom: 26, left: 40 };
const GAP = 2; // surface gap between stacked segments
const RADIUS = 4;

/** A rectangle with rounded top corners and a square base. */
function topRounded(x: number, y: number, w: number, h: number): string {
  const r = Math.min(RADIUS, h, w / 2);
  return `M${x},${y + h}V${y + r}Q${x},${y} ${x + r},${y}H${x + w - r}Q${x + w},${y} ${x + w},${y + r}V${y + h}Z`;
}

/**
 * Columns, single or stacked. Columns are at most 24px wide with rounded data ends on a square
 * baseline and 2px surface gaps between segments; a legend appears for two or more series. Each
 * column is its own hover/focus target with a tooltip listing every series; the tallest column and
 * any highlighted one carry their total. A table view below keeps every value reachable.
 */
@Component({
  selector: 'app-column-chart',
  host: { class: 'block' },
  template: `
    @if (series().length > 1) {
      <ul class="mb-3 flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted">
        @for (s of series(); track s.key) {
          <li class="flex items-center gap-1.5">
            <span
              class="inline-block size-2.5 rounded-sm"
              [class]="s.swatch"
              aria-hidden="true"
            ></span>
            {{ s.label }}
          </li>
        }
      </ul>
    }

    <div class="relative">
      <svg
        width="100%"
        [attr.height]="height()"
        class="block overflow-visible outline-none focus-visible:outline-2 focus-visible:outline-brand-500"
        role="img"
        tabindex="0"
        [attr.aria-label]="ariaLabel()"
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

        @if (divider(); as d) {
          <line
            [attr.x1]="d.x"
            [attr.x2]="d.x"
            [attr.y1]="m.top - 8"
            [attr.y2]="height() - m.bottom"
            class="stroke-muted"
            stroke-width="1"
          />
          <text [attr.x]="d.x + 4" [attr.y]="m.top - 10" class="fill-muted text-[11px]">
            {{ dividerLabel() }} →
          </text>
        }

        @for (c of layout(); track c.key; let i = $index) {
          <g
            (pointerenter)="active.set(i)"
            (pointerleave)="active.set(null)"
            [class.opacity-55]="c.upcoming"
          >
            <!-- Hit target: the whole column band, wider than the painted bar. -->
            <rect
              [attr.x]="c.bandX"
              [attr.y]="m.top"
              [attr.width]="c.bandW"
              [attr.height]="innerH()"
              fill="transparent"
            />
            <g class="chart-grow" [style.animation-delay.ms]="i * 40">
              @for (s of c.segments; track s.key) {
                <path [attr.d]="s.d" [class]="s.fill" />
              }
            </g>
            @if (c.showTotal) {
              <text
                [attr.x]="c.cx"
                [attr.y]="c.top - 6"
                text-anchor="middle"
                class="fill-ink text-[11px] font-semibold tabular-nums"
              >
                {{ format()(c.total) }}
              </text>
            }
            <text
              [attr.x]="c.cx"
              [attr.y]="height() - 6"
              text-anchor="middle"
              class="fill-muted text-[11px]"
              [class.font-semibold]="c.highlight"
            >
              {{ c.label }}
            </text>
            @if (active() === i) {
              <rect
                [attr.x]="c.x - 3"
                [attr.y]="c.top - 3"
                [attr.width]="c.w + 6"
                [attr.height]="m.top + innerH() - c.top + 3"
                rx="4"
                class="fill-none stroke-muted"
                stroke-width="1"
              />
            }
          </g>
        }
      </svg>

      @if (activeColumn(); as a) {
        <div
          class="pointer-events-none absolute top-0 z-10 min-w-36 rounded-md border border-border bg-surface px-3 py-2 text-xs shadow-md"
          [style.left.px]="tooltipLeft()"
          role="status"
        >
          <p class="text-muted">{{ a.label }}{{ a.upcoming ? ' (planned)' : '' }}</p>
          @for (s of series(); track s.key) {
            <p class="flex items-center gap-2">
              <span
                class="inline-block h-0.5 w-3 rounded"
                [class]="s.swatch"
                aria-hidden="true"
              ></span>
              <span class="font-semibold text-ink tabular-nums">{{
                format()(a.values[s.key] ?? 0)
              }}</span>
              <span class="text-muted">{{ s.label }}</span>
            </p>
          }
          @if (series().length > 1) {
            <p class="mt-1 border-t border-border pt-1">
              <span class="font-semibold text-ink tabular-nums">{{ format()(a.total) }}</span>
              <span class="text-muted"> total</span>
            </p>
          }
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
              <th class="py-1 font-medium">{{ categoryLabel() }}</th>
              @for (s of series(); track s.key) {
                <th class="py-1 text-right font-medium">{{ s.label }}</th>
              }
              @if (series().length > 1) {
                <th class="py-1 text-right font-medium">Total</th>
              }
            </tr>
          </thead>
          <tbody class="divide-y divide-border">
            @for (c of layout(); track c.key) {
              <tr>
                <td class="py-1">{{ c.label }}{{ c.upcoming ? ' (planned)' : '' }}</td>
                @for (s of series(); track s.key) {
                  <td class="py-1 text-right tabular-nums">{{ format()(c.values[s.key] ?? 0) }}</td>
                }
                @if (series().length > 1) {
                  <td class="py-1 text-right font-medium tabular-nums">{{ format()(c.total) }}</td>
                }
              </tr>
            }
          </tbody>
        </table>
      </div>
    </details>
  `,
})
export class ColumnChart {
  readonly columns = input.required<Column[]>();
  readonly series = input.required<ColumnSeries[]>();
  readonly height = input(200);
  readonly format = input<(v: number) => string>((v) => String(Math.round(v * 10) / 10));
  readonly ariaLabel = input.required<string>();
  /** Header of the first table column, e.g. "Month". */
  readonly categoryLabel = input('Period');
  readonly dividerLabel = input('Upcoming');

  protected readonly m = M;
  protected readonly width = hostWidth();
  protected readonly active = signal<number | null>(null);

  protected readonly innerH = computed(() => Math.max(1, this.height() - M.top - M.bottom));
  private readonly innerW = computed(() => Math.max(1, this.width() - M.left - M.right));

  private readonly totals = computed(() =>
    this.columns().map((c) => this.series().reduce((sum, s) => sum + (c.values[s.key] ?? 0), 0)),
  );
  private readonly yMax = computed(() => niceMax(Math.max(0, ...this.totals())));

  protected readonly layout = computed(() => {
    const cols = this.columns();
    const band = this.innerW() / Math.max(1, cols.length);
    const w = Math.min(24, band * 0.6);
    const base = M.top + this.innerH();
    const scale = this.innerH() / this.yMax();
    const tallest = this.totals().indexOf(Math.max(...this.totals()));

    return cols.map((c, i) => {
      const x = M.left + band * i + (band - w) / 2;
      let y = base;
      const drawn = this.series().filter((s) => (c.values[s.key] ?? 0) > 0);
      const segments = drawn.map((s, j) => {
        const h = (c.values[s.key] ?? 0) * scale;
        const top = y - h;
        // Square segments up to the last, which gets the rounded data end; a 2px gap between.
        const gap = j > 0 ? GAP : 0;
        const d =
          j === drawn.length - 1
            ? topRounded(x, top, w, Math.max(0, h - gap))
            : `M${x},${top}h${w}v${Math.max(0, h - gap)}h${-w}Z`;
        y = top;
        return { key: s.key, fill: s.fill, d };
      });
      const total = this.totals()[i];
      return {
        ...c,
        x,
        w,
        cx: x + w / 2,
        bandX: M.left + band * i,
        bandW: band,
        top: base - total * scale,
        total,
        segments,
        showTotal: total > 0 && (i === tallest || !!c.highlight),
      };
    });
  });

  protected readonly ticks = computed(() =>
    [0, 0.5, 1].map((f) => ({
      value: f,
      y: M.top + this.innerH() - f * this.innerH(),
      label: this.format()(f * this.yMax()),
    })),
  );

  /** A hairline between the last done column and the first upcoming one. */
  protected readonly divider = computed(() => {
    const cols = this.layout();
    const first = cols.findIndex((c) => c.upcoming);
    return first > 0 ? { x: cols[first].bandX } : null;
  });

  protected readonly activeColumn = computed(() => {
    const i = this.active();
    return i === null ? null : (this.layout()[i] ?? null);
  });

  protected readonly tooltipLeft = computed(() => {
    const a = this.activeColumn();
    if (!a) return 0;
    return a.cx > this.width() / 2 ? Math.max(0, a.x - 164) : a.x + a.w + 10;
  });

  protected key(event: KeyboardEvent): void {
    const n = this.columns().length;
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
