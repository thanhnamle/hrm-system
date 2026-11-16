import { Component, computed, input, output } from '@angular/core';

/** "Showing 21–40 of 57" with Previous/Next. Matches the API's PagedResult (pages start at 1). */
@Component({
  selector: 'app-pagination',
  template: `
    @if (totalCount() > 0) {
      <nav
        class="flex flex-wrap items-center justify-between gap-3 border-t border-border px-4 py-3 text-sm"
        aria-label="Pagination"
      >
        <p class="text-muted">
          Showing <span class="font-medium text-ink">{{ from() }}–{{ to() }}</span> of
          <span class="font-medium text-ink">{{ totalCount() }}</span>
        </p>
        <div class="flex items-center gap-2">
          <button
            type="button"
            class="btn-secondary px-3 py-1.5"
            [disabled]="page() <= 1"
            (click)="pageChange.emit(page() - 1)"
          >
            Previous
          </button>
          <span class="text-muted">Page {{ page() }} of {{ totalPages() }}</span>
          <button
            type="button"
            class="btn-secondary px-3 py-1.5"
            [disabled]="page() >= totalPages()"
            (click)="pageChange.emit(page() + 1)"
          >
            Next
          </button>
        </div>
      </nav>
    }
  `,
})
export class Pagination {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize())),
  );
  protected readonly from = computed(() => (this.page() - 1) * this.pageSize() + 1);
  protected readonly to = computed(() =>
    Math.min(this.page() * this.pageSize(), this.totalCount()),
  );
}
