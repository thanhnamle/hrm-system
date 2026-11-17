import { Directive, effect, ElementRef, inject, input, untracked } from '@angular/core';

const DURATION_MS = 700;

/** True when the user asked the OS for less motion, or the environment can't animate (tests). */
export function prefersReducedMotion(): boolean {
  return (
    typeof window === 'undefined' ||
    typeof window.matchMedia !== 'function' ||
    typeof requestAnimationFrame !== 'function' ||
    window.matchMedia('(prefers-reduced-motion: reduce)').matches
  );
}

/**
 * Rolls a number up to its value (ease-out), from 0 the first time and from the previous value on
 * refresh. Writes the final value straight away when reduced motion is preferred.
 *
 *   <span [appCountUp]="org.headcount"></span>
 */
@Directive({ selector: '[appCountUp]' })
export class CountUp {
  readonly appCountUp = input.required<number>();
  readonly decimals = input(0);

  private readonly el = inject(ElementRef<HTMLElement>).nativeElement as HTMLElement;
  private shown = 0;
  private frame = 0;

  constructor() {
    effect((onCleanup) => {
      const target = this.appCountUp();
      const decimals = this.decimals();
      untracked(() => this.animate(target, decimals));
      onCleanup(() => cancelAnimationFrame?.(this.frame));
    });
  }

  private animate(target: number, decimals: number): void {
    const write = (v: number) => (this.el.textContent = v.toFixed(decimals));
    if (prefersReducedMotion() || target === this.shown) {
      this.shown = target;
      write(target);
      return;
    }
    const from = this.shown;
    const start = performance.now();
    const step = (now: number) => {
      const t = Math.min(1, (now - start) / DURATION_MS);
      const eased = 1 - (1 - t) ** 3;
      write(from + (target - from) * eased);
      if (t < 1) this.frame = requestAnimationFrame(step);
      else this.shown = target;
    };
    this.frame = requestAnimationFrame(step);
  }
}
