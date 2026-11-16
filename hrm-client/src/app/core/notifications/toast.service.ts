import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  kind: ToastKind;
  message: string;
}

/** Short messages in the corner of the screen. Rendered by <app-toast-container> in the root. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  private static readonly DurationMs = 5000;
  private nextId = 1;
  private readonly items = signal<Toast[]>([]);

  readonly toasts = this.items.asReadonly();

  success(message: string): void {
    this.show('success', message);
  }

  error(message: string): void {
    this.show('error', message);
  }

  info(message: string): void {
    this.show('info', message);
  }

  dismiss(id: number): void {
    this.items.update((list) => list.filter((t) => t.id !== id));
  }

  private show(kind: ToastKind, message: string): void {
    const id = this.nextId++;
    // Keep at most 3 on screen; the same message twice in a row is shown once.
    this.items.update((list) =>
      list.at(-1)?.message === message ? list : [...list.slice(-2), { id, kind, message }],
    );
    setTimeout(() => this.dismiss(id), ToastService.DurationMs);
  }
}
