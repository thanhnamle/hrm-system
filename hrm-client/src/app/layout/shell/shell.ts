import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { LayoutState } from '../layout-state';
import { Sidebar } from '../sidebar/sidebar';
import { Topbar } from '../topbar/topbar';

/**
 * Frame for every signed-in page: sidebar, top bar, and the page itself in the outlet.
 * From the lg breakpoint the sidebar is a fixed column; below it, a drawer that slides in over
 * the page (opened from the top bar, closed by the backdrop, Escape, its close button, or
 * navigating). A closed drawer is `invisible`, so its links leave the tab order too.
 */
@Component({
  imports: [RouterOutlet, Sidebar, Topbar],
  selector: 'app-shell',
  templateUrl: './shell.html',
  providers: [LayoutState],
  host: { '(document:keydown.escape)': 'layout.navOpen.set(false)' },
})
export class Shell {
  protected readonly layout = inject(LayoutState);
}
