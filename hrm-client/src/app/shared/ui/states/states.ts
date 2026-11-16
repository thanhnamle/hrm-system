import { Component, input } from '@angular/core';

/** Shown while data loads. The text is for screen readers; sighted users see the spinner. */
@Component({
  selector: 'app-loading',
  template: `
    <div class="flex items-center justify-center gap-3 py-12 text-muted" role="status">
      <span
        class="size-5 animate-spin rounded-full border-2 border-border border-t-brand-600"
        aria-hidden="true"
      ></span>
      <span class="text-sm">{{ label() }}</span>
    </div>
  `,
})
export class Loading {
  readonly label = input('Loading…');
}

/** Shown when a list has no rows. Put an action button inside the element if useful. */
@Component({
  selector: 'app-empty-state',
  template: `
    <div class="py-12 text-center">
      <p class="font-medium">{{ title() }}</p>
      @if (message()) {
        <p class="mt-1 text-sm text-muted">{{ message() }}</p>
      }
      <div class="mt-4 flex justify-center gap-2"><ng-content /></div>
    </div>
  `,
})
export class EmptyState {
  readonly title = input.required<string>();
  readonly message = input<string>();
}
