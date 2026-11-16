import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.html',
})
export class Topbar {
  protected readonly auth = inject(AuthService);
}
