import { DatePipe } from '@angular/common';
import { Component, computed, ElementRef, inject, signal, viewChildren } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { Icon } from '../../shared/ui/icon/icon';
import { LayoutState } from '../layout-state';
import { initials } from '../sidebar/sidebar';

/**
 * Top bar: the menu button (small screens), the current page's name, and the account menu.
 * The menu follows the menu-button pattern: Enter/Space/ArrowDown open it on the first item,
 * arrows move, Escape closes and returns focus, a click outside closes it.
 */
@Component({
  imports: [DatePipe, RouterLink, Icon],
  selector: 'app-topbar',
  templateUrl: './topbar.html',
  host: { '(document:click)': 'closeOnOutsideClick($event)' },
})
export class Topbar {
  protected readonly auth = inject(AuthService);
  protected readonly layout = inject(LayoutState);
  private readonly host = inject(ElementRef<HTMLElement>);

  protected readonly today = new Date();
  protected readonly menuOpen = signal(false);
  private readonly items = viewChildren<ElementRef<HTMLElement>>('menuItem');

  protected readonly user = computed(() => {
    const u = this.auth.user();
    if (!u) return null;
    const name = u.fullName ?? u.email;
    return {
      name,
      email: u.email,
      role: u.role,
      initials: initials(name),
      employeeId: u.employeeId,
    };
  });

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
    if (this.menuOpen()) this.focusItem(0);
  }

  protected openMenuAt(index: number, event: Event): void {
    event.preventDefault();
    this.menuOpen.set(true);
    this.focusItem(index);
  }

  protected menuKey(event: KeyboardEvent, button: HTMLElement): void {
    const items = this.items().map((i) => i.nativeElement);
    const current = items.indexOf(document.activeElement as HTMLElement);
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        items[(current + 1) % items.length]?.focus();
        break;
      case 'ArrowUp':
        event.preventDefault();
        items[(current - 1 + items.length) % items.length]?.focus();
        break;
      case 'Home':
        event.preventDefault();
        items[0]?.focus();
        break;
      case 'End':
        event.preventDefault();
        items.at(-1)?.focus();
        break;
      case 'Escape':
        event.preventDefault();
        this.menuOpen.set(false);
        button.focus();
        break;
      case 'Tab':
        this.menuOpen.set(false);
        break;
    }
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  protected signOut(): void {
    this.menuOpen.set(false);
    this.auth.logout();
  }

  protected closeOnOutsideClick(event: MouseEvent): void {
    if (this.menuOpen() && !this.host.nativeElement.contains(event.target as Node)) {
      this.menuOpen.set(false);
    }
  }

  /** After the menu renders. A negative index counts from the end (-1 = last item). */
  private focusItem(index: number): void {
    setTimeout(() => this.items().at(index)?.nativeElement.focus());
  }
}
