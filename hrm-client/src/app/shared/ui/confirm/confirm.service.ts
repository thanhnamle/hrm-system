import { Injectable, signal } from '@angular/core';

export interface ConfirmOptions {
  title: string;
  message: string;
  confirmText?: string;
  /** 'danger' for actions that can't be undone (terminate, delete, reject). */
  tone?: 'primary' | 'danger';
}

interface PendingConfirm extends ConfirmOptions {
  resolve: (confirmed: boolean) => void;
}

/**
 * "Are you sure?" as a promise:
 *
 *   if (await this.confirm.ask({ title: 'Terminate employee?', message: '…', tone: 'danger' })) { … }
 *
 * Rendered by <app-confirm-dialog> in the root component.
 */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private readonly current = signal<PendingConfirm | null>(null);
  readonly pending = this.current.asReadonly();

  ask(options: ConfirmOptions): Promise<boolean> {
    this.current()?.resolve(false); // a newer question replaces an unanswered one
    return new Promise((resolve) => this.current.set({ ...options, resolve }));
  }

  answer(confirmed: boolean): void {
    const pending = this.current();
    if (!pending) return;
    this.current.set(null);
    pending.resolve(confirmed);
  }
}
