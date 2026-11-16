import { Component, effect, ElementRef, input, output, viewChild } from '@angular/core';

let nextId = 0;

/**
 * Dialog built on the native <dialog> element: showModal() gives focus trapping, Esc to close
 * and an inert background for free, so no extra library is needed.
 *
 *   <app-modal title="Add department" [open]="formOpen()" (closed)="formOpen.set(false)">
 *     …content…
 *     <div modal-actions><button class="btn-primary">Save</button></div>
 *   </app-modal>
 */
@Component({
  selector: 'app-modal',
  template: `
    <dialog
      #dialog
      class="m-auto w-full max-w-lg rounded-lg border border-border bg-surface p-0 text-ink shadow-xl backdrop:bg-ink/40"
      [attr.aria-labelledby]="titleId"
      (close)="closed.emit()"
    >
      <div class="p-6">
        <div class="flex items-start justify-between gap-4">
          <h2 class="text-lg font-semibold" [id]="titleId">{{ title() }}</h2>
          <button
            type="button"
            class="btn-ghost -m-2 p-2"
            aria-label="Close"
            (click)="dialog.close()"
          >
            ✕
          </button>
        </div>
        <div class="mt-4">
          <ng-content />
        </div>
        <div class="mt-6 flex justify-end gap-2 empty:hidden">
          <ng-content select="[modal-actions]" />
        </div>
      </div>
    </dialog>
  `,
})
export class Modal {
  readonly title = input.required<string>();
  readonly open = input(false);
  /** Fired however the dialog closes (✕, Esc, or open set to false). */
  readonly closed = output<void>();

  protected readonly titleId = `modal-title-${nextId++}`;
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    effect(() => {
      const dialog = this.dialog().nativeElement;
      if (this.open() && !dialog.open) dialog.showModal();
      if (!this.open() && dialog.open) dialog.close();
    });
  }
}
