import { Component, inject } from '@angular/core';
import { ToastService } from '../../../core/notifications/toast.service';

/** Renders the ToastService queue. Placed once, in the root component. */
@Component({
  selector: 'app-toast-container',
  templateUrl: './toast-container.html',
})
export class ToastContainer {
  protected readonly toast = inject(ToastService);
}
