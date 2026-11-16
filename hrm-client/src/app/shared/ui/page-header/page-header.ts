import { Component, input } from '@angular/core';

/** Page title with optional description; buttons go inside the element and sit on the right. */
@Component({
  selector: 'app-page-header',
  template: `
    <div class="mb-6 flex flex-wrap items-start justify-between gap-4">
      <div>
        <h1 class="text-2xl font-semibold">{{ title() }}</h1>
        @if (description()) {
          <p class="mt-1 text-sm text-muted">{{ description() }}</p>
        }
      </div>
      <div class="flex flex-wrap items-center gap-2">
        <ng-content />
      </div>
    </div>
  `,
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly description = input<string>();
}
