import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  imports: [RouterLink, RouterLinkActive],
  selector: 'app-topbar',
  templateUrl: './topbar.html',
})
export class Topbar {
  protected readonly auth = inject(AuthService);
}
