import { Component, computed, input } from '@angular/core';

/**
 * Simple 24×24 line icons, drawn with the text colour. Path data only (circles and rectangles
 * written as paths): rendered as real <path> elements, because Angular's sanitizer would strip
 * SVG shapes inserted through innerHTML.
 */
const ICONS = {
  // Navigation
  grid: [
    'M4 5a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v4a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1z',
    'M14 5a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v4a1 1 0 0 1-1 1h-4a1 1 0 0 1-1-1z',
    'M4 15a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v4a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1z',
    'M14 15a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v4a1 1 0 0 1-1 1h-4a1 1 0 0 1-1-1z',
  ],
  users: [
    'M5.5 8a3.5 3.5 0 1 0 7 0a3.5 3.5 0 1 0 -7 0',
    'M2.5 20c0-3.6 2.9-6.5 6.5-6.5s6.5 2.9 6.5 6.5',
    'M14.5 9a2.5 2.5 0 1 0 5 0a2.5 2.5 0 1 0 -5 0',
    'M17 14c2.8 0 4.5 2 4.5 5',
  ],
  building: [
    'M6 3h12a1 1 0 0 1 1 1v16a1 1 0 0 1 -1 1h-12a1 1 0 0 1 -1 -1v-16a1 1 0 0 1 1 -1z',
    'M9 7h2M13 7h2M9 11h2M13 11h2M9 15h2M13 15h2',
  ],
  calendar: [
    'M5.5 5h13a2 2 0 0 1 2 2v11a2 2 0 0 1 -2 2h-13a2 2 0 0 1 -2 -2v-11a2 2 0 0 1 2 -2z',
    'M3.5 10h17M8 3v4M16 3v4',
  ],
  approvals: [
    'M9 3.5h6v3H9z',
    'M8 5H6.5A1.5 1.5 0 0 0 5 6.5v13A1.5 1.5 0 0 0 6.5 21h11a1.5 1.5 0 0 0 1.5-1.5v-13A1.5 1.5 0 0 0 17.5 5H16',
    'm9 14 2 2 4-4',
  ],
  clock: ['M3 12a9 9 0 1 0 18 0a9 9 0 1 0 -18 0', 'M12 7v5l3 2'],
  userCheck: [
    'M5 8a4 4 0 1 0 8 0a4 4 0 1 0 -8 0',
    'M2.5 21c0-3.6 2.9-6.5 6.5-6.5 1.9 0 3.6.8 4.8 2.1',
    'm15 19 2 2 4-4',
  ],
  key: [
    'M3.5 15a4.5 4.5 0 1 0 9 0a4.5 4.5 0 1 0 -9 0',
    'M11 11.5 20 3',
    'M17 6l3 3',
    'M14.5 8.5l2 2',
  ],
  // Dashboard
  inbox: ['M4 13 6 5h12l2 8v6H4z', 'M4 13h5l1 2h4l1-2h5'],
  shield: ['M12 3l8 3v6c0 4.5-3.4 8-8 9-4.6-1-8-4.5-8-9V6z', 'm9 12 2 2 4-4'],
  pulse: ['M3 12h4l2-5 4 10 2-5h6'],
  file: ['M7 3h7l5 5v13H7z', 'M14 3v5h5'],
  user: ['M8 8a4 4 0 1 0 8 0a4 4 0 1 0 -8 0', 'M4.5 21c0-4.1 3.4-7.5 7.5-7.5s7.5 3.4 7.5 7.5'],
  // Chrome
  menu: ['M4 7h16', 'M4 12h16', 'M4 17h16'],
  close: ['M6 6l12 12', 'M18 6 6 18'],
  chevronDown: ['m6 9 6 6 6-6'],
  lock: [
    'M6.5 11h11a1.5 1.5 0 0 1 1.5 1.5v7a1.5 1.5 0 0 1 -1.5 1.5h-11a1.5 1.5 0 0 1 -1.5 -1.5v-7a1.5 1.5 0 0 1 1.5 -1.5z',
    'M8 11V8a4 4 0 0 1 8 0v3',
  ],
  logout: ['M10 4H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h4', 'm15 16 4-4-4-4', 'M19 12H9'],
} as const;

export type IconName = keyof typeof ICONS;

@Component({
  selector: 'app-icon',
  host: { class: 'inline-flex shrink-0' },
  template: `<svg
    viewBox="0 0 24 24"
    fill="none"
    stroke="currentColor"
    stroke-width="1.75"
    stroke-linecap="round"
    stroke-linejoin="round"
    [attr.class]="size()"
    aria-hidden="true"
  >
    @for (d of paths(); track $index) {
      <path [attr.d]="d" />
    }
  </svg>`,
})
export class Icon {
  readonly name = input.required<IconName>();
  /** Tailwind size class for the svg. */
  readonly size = input('size-5');
  protected readonly paths = computed(() => ICONS[this.name()]);
}
