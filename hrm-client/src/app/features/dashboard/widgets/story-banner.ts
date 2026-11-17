import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Story, Tone } from '../story';
import { CountUp } from './count-up';
import { Icon } from './widgets';

const TONE_DOT: Record<Tone, string> = {
  good: 'bg-success',
  warning: 'bg-warning',
  neutral: 'bg-white/50',
};

/**
 * The top of the dashboard: who it's for, what matters today in one or two sentences, the one
 * number that sums it up, and a few supporting points that link to where to act. Each point also
 * says its state in words; the coloured dot only repeats it.
 */
@Component({
  selector: 'app-story-banner',
  host: { class: 'block' },
  imports: [DatePipe, NgTemplateOutlet, RouterLink, CountUp, Icon],
  template: `
    @let s = story();
    <section
      class="reveal overflow-hidden rounded-xl bg-brand-600 p-6 text-white shadow-sm sm:p-8"
      aria-labelledby="story-heading"
    >
      <div class="flex flex-wrap items-start justify-between gap-6">
        <div class="max-w-2xl min-w-0">
          <p class="text-sm text-brand-100">{{ today() | date: 'EEEE, MMMM d' }}</p>
          <h1 id="story-heading" class="mt-1 text-2xl font-semibold sm:text-3xl">
            {{ greeting() }}
          </h1>
          <p class="mt-3 text-base leading-relaxed text-brand-50 sm:text-lg">{{ s.headline }}</p>
        </div>
        <div class="shrink-0 text-right">
          <p class="text-5xl leading-none font-semibold sm:text-6xl">
            <span [appCountUp]="s.hero.value"></span>{{ s.hero.suffix }}
          </p>
          <p class="mt-2 text-sm text-brand-100">{{ s.hero.label }}</p>
        </div>
      </div>

      @if (s.points.length) {
        <ul class="mt-6 grid gap-2 sm:grid-cols-2 xl:grid-cols-3">
          @for (p of s.points; track p.text; let i = $index) {
            <li class="reveal" [style.--i]="i + 1">
              @if (p.link) {
                <a
                  [routerLink]="p.link"
                  class="flex h-full items-center gap-3 rounded-lg bg-white/10 px-3 py-2.5 text-sm transition-colors hover:bg-white/20"
                >
                  <ng-container *ngTemplateOutlet="point; context: { $implicit: p }" />
                </a>
              } @else {
                <div
                  class="flex h-full items-center gap-3 rounded-lg bg-white/10 px-3 py-2.5 text-sm"
                >
                  <ng-container *ngTemplateOutlet="point; context: { $implicit: p }" />
                </div>
              }
            </li>
          }
        </ul>
      }
    </section>

    <ng-template #point let-p>
      <span class="relative shrink-0 text-white/90">
        <app-icon [name]="p.icon" />
        <span
          class="absolute -top-0.5 -right-0.5 size-2 rounded-full ring-2 ring-brand-600"
          [class]="dotClass(p.tone)"
          aria-hidden="true"
        ></span>
      </span>
      <span class="min-w-0">{{ p.text }}</span>
      @if (p.tone !== 'neutral') {
        <span class="sr-only">({{ p.tone === 'good' ? 'good' : 'needs attention' }})</span>
      }
    </ng-template>
  `,
})
export class StoryBanner {
  readonly story = input.required<Story>();
  readonly greeting = input.required<string>();
  readonly today = input.required<string>();
  /** Typed lookup: the ng-template's `let-p` is untyped, so the map can't be indexed in the template. */
  protected dotClass(tone: Tone): string {
    return TONE_DOT[tone];
  }
}
