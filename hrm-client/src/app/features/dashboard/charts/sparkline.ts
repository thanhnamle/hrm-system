import { Component, computed, input } from '@angular/core';
import { hostWidth } from './chart-size';

/**
 * A small trend under a stat tile's number: the history in a quiet grey line, the current
 * value as a brand dot. Decorative support for the number above it, so hidden from screen
 * readers; the tile's text carries the value.
 */
@Component({
  selector: 'app-sparkline',
  host: { class: 'block' },
  template: `
    <svg width="100%" [attr.height]="height" class="block overflow-visible" aria-hidden="true">
      @if (path(); as d) {
        <path
          [attr.d]="d"
          pathLength="1"
          class="chart-draw fill-none stroke-muted/60"
          stroke-width="1.5"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      }
      @if (end(); as e) {
        <circle [attr.cx]="e.x" [attr.cy]="e.y" r="4.5" class="fill-surface" />
        <circle [attr.cx]="e.x" [attr.cy]="e.y" r="3" class="fill-brand-600" />
      }
    </svg>
  `,
})
export class Sparkline {
  readonly values = input.required<number[]>();
  protected readonly height = 28;
  protected readonly width = hostWidth(160);

  private readonly coords = computed(() => {
    const v = this.values();
    if (v.length < 2) return [];
    const min = Math.min(...v);
    const range = Math.max(...v) - min || 1;
    const pad = 4;
    const w = this.width() - pad * 2;
    return v.map((value, i) => ({
      x: pad + (i / (v.length - 1)) * w,
      y: pad + (1 - (value - min) / range) * (this.height - pad * 2),
    }));
  });

  protected readonly path = computed(
    () =>
      this.coords()
        .map((c, i) => `${i ? 'L' : 'M'}${c.x.toFixed(1)},${c.y.toFixed(1)}`)
        .join('') || null,
  );

  protected readonly end = computed(() => this.coords().at(-1) ?? null);
}
