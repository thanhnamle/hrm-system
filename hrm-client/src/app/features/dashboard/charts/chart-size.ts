import { afterNextRender, DestroyRef, ElementRef, inject, signal } from '@angular/core';

/**
 * The host element's width, kept current with a ResizeObserver, so charts draw in real pixels
 * (text and 2px lines stay crisp instead of being stretched by a viewBox).
 * Falls back to `fallback` where ResizeObserver doesn't exist (unit tests in jsdom).
 *
 * The chart's <svg> must use width="100%", not this value: a fixed pixel width would let the
 * drawing widen its own grid column, and the observer would then measure that wider size back.
 */
export function hostWidth(fallback = 600) {
  const host = inject(ElementRef<HTMLElement>);
  const destroyRef = inject(DestroyRef);
  const width = signal(fallback);

  afterNextRender(() => {
    if (typeof ResizeObserver === 'undefined') return;
    const observer = new ResizeObserver(([entry]) => {
      const w = Math.floor(entry.contentRect.width);
      if (w > 0) width.set(w);
    });
    observer.observe(host.nativeElement);
    destroyRef.onDestroy(() => observer.disconnect());
  });

  return width.asReadonly();
}

/** A round axis maximum at or above `value`: 1, 2, 2.5, 5 × 10^n. */
export function niceMax(value: number): number {
  if (value <= 0) return 1;
  const power = 10 ** Math.floor(Math.log10(value));
  const step = [1, 2, 2.5, 5, 10].find((m) => m * power >= value)!;
  return step * power;
}
