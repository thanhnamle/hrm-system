import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Sidebar } from '../sidebar/sidebar';
import { Topbar } from '../topbar/topbar';

/** Frame for every signed-in page: sidebar, top bar, and the page itself in the outlet. */
@Component({
  imports: [RouterOutlet, Sidebar, Topbar],
  selector: 'app-shell',
  templateUrl: './shell.html',
})
export class Shell {}
