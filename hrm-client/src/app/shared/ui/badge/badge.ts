import { Component, computed, input } from '@angular/core';

export type BadgeTone = 'neutral' | 'brand' | 'success' | 'warning' | 'danger';

// Full class names (not built from strings) so Tailwind can find them in the source.
const TONES: Record<BadgeTone, string> = {
  neutral: 'bg-canvas text-muted ring-border',
  brand: 'bg-brand-50 text-brand-700 ring-brand-100',
  success: 'bg-success/10 text-success ring-success/20',
  warning: 'bg-warning/15 text-ink ring-warning/30',
  danger: 'bg-danger/10 text-danger ring-danger/20',
};

/** Status colour for every enum the API returns, so the same status looks the same everywhere. */
const STATUS_TONES: Record<string, BadgeTone> = {
  Active: 'success',
  Terminated: 'neutral',
  Pending: 'warning',
  Approved: 'success',
  Rejected: 'danger',
  Cancelled: 'neutral',
  Present: 'success',
  CheckedOut: 'brand',
  Absent: 'neutral',
  OnLeave: 'warning',
};

export const statusTone = (status: string): BadgeTone => STATUS_TONES[status] ?? 'neutral';

/** "CheckedOut" → "Checked out", "OnLeave" → "On leave". */
export const humanize = (value: string): string =>
  value
    .replace(/([a-z])([A-Z])/g, '$1 $2')
    .replace(/ (\w)/g, (_, c: string) => ` ${c.toLowerCase()}`);

/**
 * Small coloured label. Pass `status` for API enums (tone and text are derived),
 * or `tone` plus content for anything else.
 */
@Component({
  selector: 'app-badge',
  template: `
    <span
      class="inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset"
      [class]="classes()"
    >
      @if (status(); as s) {
        {{ label(s) }}
      } @else {
        <ng-content />
      }
    </span>
  `,
})
export class Badge {
  readonly status = input<string>();
  readonly tone = input<BadgeTone>('neutral');

  protected readonly classes = computed(
    () => TONES[this.status() ? statusTone(this.status()!) : this.tone()],
  );
  protected readonly label = humanize;
}
