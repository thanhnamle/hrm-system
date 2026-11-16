import { Component, inject } from '@angular/core';
import { Modal } from '../modal/modal';
import { ConfirmService } from './confirm.service';

/** Renders ConfirmService questions. Placed once, in the root component. */
@Component({
  imports: [Modal],
  selector: 'app-confirm-dialog',
  template: `
    @let question = confirm.pending();
    <app-modal [title]="question?.title ?? ''" [open]="!!question" (closed)="confirm.answer(false)">
      <p class="text-sm text-muted">{{ question?.message }}</p>
      <div modal-actions>
        <button type="button" class="btn-secondary" (click)="confirm.answer(false)">Cancel</button>
        <button
          type="button"
          [class]="question?.tone === 'danger' ? 'btn-danger' : 'btn-primary'"
          (click)="confirm.answer(true)"
        >
          {{ question?.confirmText ?? 'Confirm' }}
        </button>
      </div>
    </app-modal>
  `,
})
export class ConfirmDialog {
  protected readonly confirm = inject(ConfirmService);
}
