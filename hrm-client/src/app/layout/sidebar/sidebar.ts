import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NAV_ITEMS } from '../nav-items';

@Component({
  imports: [RouterLink, RouterLinkActive],
  selector: 'app-sidebar',
  templateUrl: './sidebar.html',
})
export class Sidebar {
  // Shows every item for now; filtering by permission comes with the AuthService.
  protected readonly items = NAV_ITEMS;
}
